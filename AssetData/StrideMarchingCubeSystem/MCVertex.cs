using Stride.Core.Mathematics;
using Stride.Graphics;
using System.Runtime.InteropServices;

namespace StrideMarchingCubeSystem;

/// <summary>
/// Vertex format for GPU marching-cubes output: Position + Normal + PackedMaterial = 28 bytes.
/// PackedMaterial: bits[0:7] = MaterialId, bits[8:15] = MaterialId2, bits[16:31] = BlendWeight (uint16).
/// The uint attribute is flat (not interpolated by the rasterizer).
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct MCVertex
{
    public Vector3 Position;
    public Vector3 Normal;
    public uint PackedMaterial;

    public const int SizeInBytes = 28;

    public static readonly VertexDeclaration Layout = new VertexDeclaration(
        VertexElement.Position<Vector3>(),
        VertexElement.Normal<Vector3>(),
        new VertexElement("PACKEDMATERIAL", PixelFormat.R32_UInt));
}
