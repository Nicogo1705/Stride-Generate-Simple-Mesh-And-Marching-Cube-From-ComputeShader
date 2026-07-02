using System.Runtime.CompilerServices;

namespace StrideMarchingCubeSystem;

/// <summary>
/// Packed voxel storage for one chunk: each voxel is a <see cref="ushort"/> with
/// density in bits 0-7 and material (<see cref="TerrainType"/>) in bits 8-15. This
/// layout is exactly what the GPU expects, so uploading is a plain ushort → uint widen.
///
/// Density convention: <b>0 = fully solid, 255 = air</b>; a voxel is solid when
/// density &lt; 128 (isolevel). <see cref="AxisLength"/> = <see cref="Size"/> + 1 so a
/// cube at index Size-1 can read the extra sample at index Size. Filling those extra
/// samples from absolute world coordinates makes neighbouring chunks seamless without
/// any explicit border stitching.
///
/// Memory layout: X-major (x varies slowest, z fastest).
/// </summary>
public struct VoxelGrid
{
    /// <summary>Logical chunk size (e.g. 32). Cubes span [0..Size-1] per axis.</summary>
    public readonly int Size;

    /// <summary>Samples per axis = Size + 1. Array indices span [0..AxisLength-1].</summary>
    public readonly int AxisLength;

    /// <summary>AxisLength², precomputed for Index().</summary>
    public readonly int AxisLengthSq;

    /// <summary>Total voxels = AxisLength³.</summary>
    public readonly int TotalLength;

    /// <summary>Packed voxel data: bits[0:7] = density, bits[8:15] = material.</summary>
    public ushort[] Data;

    public VoxelGrid(int size)
    {
        Size = size;
        AxisLength = size + 1;
        AxisLengthSq = AxisLength * AxisLength;
        TotalLength = AxisLengthSq * AxisLength;
        Data = new ushort[TotalLength];
    }

    /// <summary>True if every voxel is solid or every voxel is air (no surface → skip meshing).</summary>
    public readonly bool IsUniform(byte isolevelByte)
    {
        bool firstSolid = (byte)(Data[0] & 0xFF) < isolevelByte;
        for (int i = 1; i < TotalLength; i++)
        {
            bool solid = (byte)(Data[i] & 0xFF) < isolevelByte;
            if (solid != firstSolid) return false;
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int Index(int x, int y, int z) => x * AxisLengthSq + y * AxisLength + z;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsInBounds(int x, int y, int z)
        => (uint)x < (uint)AxisLength && (uint)y < (uint)AxisLength && (uint)z < (uint)AxisLength;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly byte GetDensity(int x, int y, int z) => (byte)(Data[Index(x, y, z)] & 0xFF);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly TerrainType GetMaterial(int x, int y, int z) => (TerrainType)(byte)(Data[Index(x, y, z)] >> 8);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetDensity(byte value, int x, int y, int z)
    {
        int idx = Index(x, y, z);
        Data[idx] = (ushort)((Data[idx] & 0xFF00) | value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetMaterial(TerrainType type, int x, int y, int z)
    {
        int idx = Index(x, y, z);
        Data[idx] = (ushort)((Data[idx] & 0x00FF) | ((byte)type << 8));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(byte density, TerrainType type, int x, int y, int z)
        => Data[Index(x, y, z)] = (ushort)(density | ((byte)type << 8));

    private static readonly float[] ByteToFloatLUT = InitByteLUT();
    private static float[] InitByteLUT()
    {
        var lut = new float[256];
        for (int i = 0; i < 256; i++) lut[i] = i / 255f;
        return lut;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ByteToFloat(byte density) => ByteToFloatLUT[density];
}
