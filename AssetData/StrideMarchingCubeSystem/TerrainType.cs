namespace StrideMarchingCubeSystem;

/// <summary>
/// Per-voxel terrain material. Stored in the high byte of each <see cref="VoxelGrid"/>
/// voxel and resolved to a per-surface dominant/secondary blend on the GPU. Each value
/// maps to a colour slot in <see cref="TerrainPalette"/>.
///
/// Keep the count &lt;= 14 (the value the marching-cubes shader scans) and in sync with
/// the palette. Add your own biomes/ores by extending this enum and the palette together.
/// </summary>
public enum TerrainType : byte
{
    Rock  = 0,
    Dirt  = 1,
    Grass = 2,
    Sand  = 3,
    Snow  = 4,
}
