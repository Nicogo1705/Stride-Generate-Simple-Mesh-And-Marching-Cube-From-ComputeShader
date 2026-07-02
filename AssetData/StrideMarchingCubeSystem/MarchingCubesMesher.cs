using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.ComputeEffect;
using System;
using GpuBuffer = Stride.Graphics.Buffer;

namespace StrideMarchingCubeSystem;

/// <summary>
/// GPU Marching Cubes: uploads a <see cref="VoxelGrid"/>, dispatches
/// <c>GenerateMarchingCube.sdsl</c>, reads back the vertex count, and returns a
/// right-sized vertex buffer of <see cref="MCVertex"/> ready to drop into a Stride mesh.
///
/// The shader derives smooth normals from the density gradient and packs a
/// dominant/secondary surface material per triangle (see <see cref="TerrainType"/>).
/// One instance can be reused for many chunks — each call overwrites the shared output
/// buffer and copies the result into a fresh, exact-size vertex buffer you own.
/// Call from the main thread. Dispose when done.
/// </summary>
public sealed class MarchingCubesMesher : IDisposable
{
    private static readonly ObjectParameterKey<GpuBuffer?> KeyVoxelData    = ParameterKeys.NewObject<GpuBuffer?>(null, "GenerateMarchingCube.VoxelData");
    private static readonly ObjectParameterKey<GpuBuffer?> KeyEdgeTable    = ParameterKeys.NewObject<GpuBuffer?>(null, "GenerateMarchingCube.EdgeTable");
    private static readonly ObjectParameterKey<GpuBuffer?> KeyTriTable     = ParameterKeys.NewObject<GpuBuffer?>(null, "GenerateMarchingCube.TriTable");
    private static readonly ObjectParameterKey<GpuBuffer?> KeyOutVertices  = ParameterKeys.NewObject<GpuBuffer?>(null, "GenerateMarchingCube.OutVertices");
    private static readonly ObjectParameterKey<GpuBuffer?> KeyCounter      = ParameterKeys.NewObject<GpuBuffer?>(null, "GenerateMarchingCube.Counter");
    private static readonly ValueParameterKey<int>     KeyGridSize     = ParameterKeys.NewValue<int>(0, "GenerateMarchingCube.GridSize");
    private static readonly ValueParameterKey<int>     KeyAxisLength   = ParameterKeys.NewValue<int>(0, "GenerateMarchingCube.AxisLength");
    private static readonly ValueParameterKey<int>     KeyAxisLengthSq = ParameterKeys.NewValue<int>(0, "GenerateMarchingCube.AxisLengthSq");
    private static readonly ValueParameterKey<float>   KeyIsolevel     = ParameterKeys.NewValue<float>(0.5f, "GenerateMarchingCube.Isolevel");
    private static readonly ValueParameterKey<int>     KeyLodStep      = ParameterKeys.NewValue<int>(1, "GenerateMarchingCube.LodStep");
    private static readonly ValueParameterKey<int>     KeyMaxVertices  = ParameterKeys.NewValue<int>(0, "GenerateMarchingCube.MaxVertices");
    private static readonly ValueParameterKey<Vector3> KeyChunkOffset  = ParameterKeys.NewValue<Vector3>(default, "GenerateMarchingCube.ChunkOffset");

    private static readonly uint[] ZeroCounter = { 0u };

    private readonly GraphicsDevice _device;
    private readonly ComputeEffectShader _shader;
    private readonly GpuBuffer _edgeTableBuf;
    private readonly GpuBuffer _triTableBuf;
    private readonly GpuBuffer _outVerticesBuf;
    private readonly GpuBuffer _counterBuf;
    private readonly GpuBuffer _stagingCounterBuf;
    private readonly int _maxVertices;
    private readonly uint[] _counterReadback = new uint[1];

    private GpuBuffer? _voxelDataBuf;
    private uint[] _packed = Array.Empty<uint>();
    private RenderDrawContext? _drawContext;

    public MarchingCubesMesher(IServiceRegistry services, GraphicsDevice device, int maxVertices = 300_000)
    {
        _device = device;
        _maxVertices = Math.Max(1000, maxVertices);

        var renderContext = RenderContext.GetShared(services);
        _shader = new ComputeEffectShader(renderContext)
        {
            ShaderSourceName = "GenerateMarchingCube",
            // 4x4x4 = 64 threads/group. The shader uses many indexable temp
            // registers (8 densities + 12 edge verts + 12 normals + 14 material
            // weights); 8x8x8 trips the HLSL X4714 temp-register limit.
            ThreadNumbers = new Int3(4, 4, 4),
        };

        _edgeTableBuf   = GpuBuffer.Structured.New(_device, LookupTables.EdgeTable, unorderedAccess: false);
        _triTableBuf    = GpuBuffer.Structured.New(_device, FlattenTriangleTable(), unorderedAccess: false);
        _outVerticesBuf = GpuBuffer.Structured.New<MCVertex>(_device, _maxVertices, unorderedAccess: true);
        _counterBuf     = GpuBuffer.Structured.New<uint>(_device, 1, unorderedAccess: true);
        _stagingCounterBuf = _counterBuf.ToStaging();
    }

    /// <summary>
    /// Meshes one voxel chunk. Returns a right-sized vertex buffer + vertex count, or
    /// null if the chunk has no surface. You own the returned buffer — dispose it with
    /// the mesh/model that uses it.
    /// </summary>
    public (GpuBuffer vertexBuffer, int vertexCount)? GenerateMesh(
        VoxelGrid grid, Game game, float isolevel = 0.5f, int lodStep = 1, Vector3 chunkOffset = default)
    {
        int gridSize = grid.Size / lodStep;
        if (gridSize <= 0) return null;

        _drawContext ??= new RenderDrawContext(game.Services, RenderContext.GetShared(game.Services), game.GraphicsContext);
        var cl = _drawContext.CommandList;

        // Pack VoxelGrid (ushort) → uint[] (the GPU layout is identical, just wider).
        if (_packed.Length < grid.TotalLength) _packed = new uint[grid.TotalLength];
        var data = grid.Data;
        for (int i = 0; i < grid.TotalLength; i++) _packed[i] = data[i];

        if (_voxelDataBuf == null || _voxelDataBuf.ElementCount < grid.TotalLength)
        {
            _voxelDataBuf?.Dispose();
            _voxelDataBuf = GpuBuffer.Structured.New<uint>(_device, grid.TotalLength, unorderedAccess: false);
        }
        _voxelDataBuf.SetData(cl, new ReadOnlySpan<uint>(_packed, 0, grid.TotalLength));
        _counterBuf.SetData(cl, ZeroCounter);

        _shader.Parameters.Set(KeyVoxelData,    _voxelDataBuf);
        _shader.Parameters.Set(KeyEdgeTable,    _edgeTableBuf);
        _shader.Parameters.Set(KeyTriTable,     _triTableBuf);
        _shader.Parameters.Set(KeyOutVertices,  _outVerticesBuf);
        _shader.Parameters.Set(KeyCounter,      _counterBuf);
        _shader.Parameters.Set(KeyGridSize,     gridSize);
        _shader.Parameters.Set(KeyAxisLength,   grid.AxisLength);
        _shader.Parameters.Set(KeyAxisLengthSq, grid.AxisLengthSq);
        _shader.Parameters.Set(KeyIsolevel,     isolevel);
        _shader.Parameters.Set(KeyLodStep,      lodStep);
        _shader.Parameters.Set(KeyMaxVertices,  _maxVertices);
        _shader.Parameters.Set(KeyChunkOffset,  chunkOffset);

        int groups = (gridSize + 3) / 4;
        _shader.ThreadGroupCounts = new Int3(groups, groups, groups);
        ((RendererBase)_shader).Draw(_drawContext);

        _counterBuf.GetData(cl, _stagingCounterBuf, _counterReadback);
        int vertexCount = Math.Min((int)_counterReadback[0], _maxVertices);
        if (vertexCount == 0) return null;

        var vbo = GpuBuffer.New(_device, vertexCount * MCVertex.SizeInBytes, BufferFlags.VertexBuffer, GraphicsResourceUsage.Default);
        int byteCount = vertexCount * MCVertex.SizeInBytes;
        cl.CopyRegion(_outVerticesBuf, 0, new ResourceRegion(0, 0, 0, byteCount, 1, 1), vbo, 0);
        return (vbo, vertexCount);
    }

    private static int[] FlattenTriangleTable()
    {
        var flat = new int[256 * 16];
        Array.Fill(flat, -1);
        for (int i = 0; i < 256; i++)
        {
            var row = LookupTables.TriangleTable[i];
            for (int j = 0; j < row.Length; j++)
                flat[i * 16 + j] = row[j];
        }
        return flat;
    }

    public void Dispose()
    {
        _drawContext?.Dispose();
        _voxelDataBuf?.Dispose();
        _edgeTableBuf?.Dispose();
        _triTableBuf?.Dispose();
        _outVerticesBuf?.Dispose();
        _counterBuf?.Dispose();
        _stagingCounterBuf?.Dispose();
    }
}
