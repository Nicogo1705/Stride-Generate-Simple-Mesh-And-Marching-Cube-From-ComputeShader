using System;

namespace StrideMarchingCubeSystem;

/// <summary>Built-in terrain generators selectable from the editor.</summary>
public enum NoiseKind
{
    /// <summary>Rolling hills from fractal Perlin (FBM) — the friendly default.</summary>
    Perlin,
    /// <summary>Sharp mountain ridges from ridged multifractal noise.</summary>
    Ridged,
    /// <summary>Perlin hills carved by 3D noise tunnels — overhangs and caves.</summary>
    Caves3D,
}

/// <summary>
/// Fills a <see cref="VoxelGrid"/> chunk with density + material from absolute world
/// coordinates. Because every sample (including the +1 border row) is derived purely
/// from world position, adjacent chunks line up seamlessly with no stitching pass.
/// </summary>
public interface ITerrainGenerator
{
    void Fill(VoxelGrid grid, int worldX0, int worldY0, int worldZ0);
}

/// <summary>Shared parameters + material banding for the built-in generators.</summary>
public abstract class TerrainGeneratorBase : ITerrainGenerator
{
    protected readonly Noise Noise;

    /// <summary>Average terrain height, in world units.</summary>
    public float BaseHeight { get; set; } = 16f;
    /// <summary>Vertical scale of the terrain features.</summary>
    public float Amplitude { get; set; } = 20f;
    /// <summary>Horizontal noise frequency — smaller = broader features.</summary>
    public float Frequency { get; set; } = 0.015f;
    /// <summary>Number of fractal octaves.</summary>
    public int Octaves { get; set; } = 4;

    /// <summary>Surfaces at or above this height get Snow.</summary>
    public float SnowHeight { get; set; } = 42f;
    /// <summary>Surfaces at or below this height get Sand.</summary>
    public float SandHeight { get; set; } = 3f;

    protected TerrainGeneratorBase(int seed) => Noise = new Noise(seed);

    /// <summary>World-space surface height at (x, z).</summary>
    protected abstract float SurfaceHeight(float worldX, float worldZ);

    /// <summary>Extra per-voxel carving (0 = keep, 1 = fully air). Default: none.</summary>
    protected virtual float Carve(float worldX, float worldY, float worldZ, float surface) => 0f;

    public void Fill(VoxelGrid grid, int worldX0, int worldY0, int worldZ0)
    {
        int n = grid.AxisLength;
        for (int x = 0; x < n; x++)
        {
            float wx = worldX0 + x;
            for (int z = 0; z < n; z++)
            {
                float wz = worldZ0 + z;
                float surface = SurfaceHeight(wx, wz);
                for (int y = 0; y < n; y++)
                {
                    float wy = worldY0 + y;

                    // Air amount in [0,1]: 0 = solid, 1 = air, 0.5 at the surface.
                    float air = Math.Clamp((wy - surface) + 0.5f, 0f, 1f);
                    if (wy < surface)
                        air = MathF.Max(air, Carve(wx, wy, wz, surface));

                    byte density = (byte)(air * 255f);
                    TerrainType mat = Material(wy, surface);
                    grid.Set(density, mat, x, y, z);
                }
            }
        }
    }

    protected TerrainType Material(float worldY, float surface)
    {
        float depth = surface - worldY;
        if (depth < 1.0f)
        {
            if (surface >= SnowHeight) return TerrainType.Snow;
            if (surface <= SandHeight) return TerrainType.Sand;
            return TerrainType.Grass;
        }
        if (depth < 4f) return TerrainType.Dirt;
        return TerrainType.Rock;
    }

    protected static float SmoothStep(float edge0, float edge1, float x)
    {
        float t = Math.Clamp((x - edge0) / (edge1 - edge0), 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    /// <summary>Builds the generator selected by <paramref name="kind"/> with common parameters.</summary>
    public static TerrainGeneratorBase Create(NoiseKind kind, int seed,
        float baseHeight, float amplitude, float frequency, int octaves)
    {
        TerrainGeneratorBase g = kind switch
        {
            NoiseKind.Ridged  => new RidgedTerrainGenerator(seed),
            NoiseKind.Caves3D => new Caves3DTerrainGenerator(seed),
            _                 => new PerlinTerrainGenerator(seed),
        };
        g.BaseHeight = baseHeight;
        g.Amplitude = amplitude;
        g.Frequency = frequency;
        g.Octaves = octaves;
        return g;
    }
}

/// <summary>Rolling hills from fractal Perlin noise.</summary>
public sealed class PerlinTerrainGenerator : TerrainGeneratorBase
{
    public PerlinTerrainGenerator(int seed) : base(seed) { }

    protected override float SurfaceHeight(float worldX, float worldZ)
        => BaseHeight + Noise.Fbm(worldX * Frequency, worldZ * Frequency, Octaves) * Amplitude;
}

/// <summary>Sharp mountain ridges from ridged multifractal noise.</summary>
public sealed class RidgedTerrainGenerator : TerrainGeneratorBase
{
    public RidgedTerrainGenerator(int seed) : base(seed) { }

    protected override float SurfaceHeight(float worldX, float worldZ)
        => BaseHeight + Noise.Ridged(worldX * Frequency, worldZ * Frequency, Octaves) * Amplitude;
}

/// <summary>Perlin hills carved by 3D noise tunnels, producing caves and overhangs.</summary>
public sealed class Caves3DTerrainGenerator : TerrainGeneratorBase
{
    /// <summary>Frequency of the 3D cave noise.</summary>
    public float CaveFrequency { get; set; } = 0.05f;
    /// <summary>Noise value above which rock is carved away (higher = fewer caves).</summary>
    public float CaveThreshold { get; set; } = 0.55f;

    public Caves3DTerrainGenerator(int seed) : base(seed) { }

    protected override float SurfaceHeight(float worldX, float worldZ)
        => BaseHeight + Noise.Fbm(worldX * Frequency, worldZ * Frequency, Octaves) * Amplitude;

    protected override float Carve(float worldX, float worldY, float worldZ, float surface)
    {
        // Don't carve the top ~2 m so the surface stays intact; caves live below.
        if (surface - worldY < 2f) return 0f;
        // Stretch Y so caves form horizontal-ish tunnels rather than vertical shafts.
        float cave = Noise.Fbm(worldX * CaveFrequency, worldY * CaveFrequency * 2f, worldZ * CaveFrequency, 3);
        return SmoothStep(CaveThreshold, CaveThreshold + 0.12f, cave);
    }
}
