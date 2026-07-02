using Stride.Core.Mathematics;
using Stride.Graphics;
using System;

namespace StrideMarchingCubeSystem;

/// <summary>
/// Builds a <see cref="Texture"/> array (one slice per <see cref="TerrainType"/>) of
/// procedural material textures — rock, dirt, grass, sand, snow — entirely on the CPU,
/// so the terrain looks textured with zero external image assets. Triplanar-sampled by
/// TerrainDiffuse.sdsl. Swap this out (or point the material at real textures) to reskin.
/// </summary>
public static class TerrainTextures
{
    public const int TexSize = 128;
    public const int SliceCount = 8; // >= TerrainType count; extra slices are neutral grey

    /// <summary>Builds the terrain Texture2DArray. Call once and bind to the material.</summary>
    public static Texture BuildArray(GraphicsDevice device, int seed = 1234)
    {
        using var image = Image.New2D(TexSize, TexSize, 1, PixelFormat.R8G8B8A8_UNorm_SRgb, SliceCount);
        var noise = new Noise(seed);
        for (int slice = 0; slice < SliceCount; slice++)
            image.PixelBuffer[slice, 0].SetPixels(GenerateSlice(slice, noise));
        return Texture.New(device, image);
    }

    private static Color[] GenerateSlice(int index, Noise noise)
    {
        // (base colour, noise variation, world-ish detail scale)
        (Color4 baseCol, float variation, float scale) = index switch
        {
            (int)TerrainType.Rock  => (new Color4(0.48f, 0.43f, 0.36f, 1f), 0.38f, 0.09f),
            (int)TerrainType.Dirt  => (new Color4(0.36f, 0.26f, 0.16f, 1f), 0.32f, 0.07f),
            (int)TerrainType.Grass => (new Color4(0.24f, 0.44f, 0.15f, 1f), 0.36f, 0.11f),
            (int)TerrainType.Sand  => (new Color4(0.78f, 0.70f, 0.50f, 1f), 0.16f, 0.16f),
            (int)TerrainType.Snow  => (new Color4(0.90f, 0.93f, 0.97f, 1f), 0.07f, 0.13f),
            _                      => (new Color4(0.5f, 0.5f, 0.5f, 1f), 0.20f, 0.10f),
        };

        var pixels = new Color[TexSize * TexSize];
        for (int y = 0; y < TexSize; y++)
        {
            for (int x = 0; x < TexSize; x++)
            {
                // Two octaves of noise for some structure; offset per slice so materials differ.
                float nx = (x + index * 91) * scale;
                float ny = (y + index * 57) * scale;
                float v = noise.Fbm(nx, ny, 4) * variation
                        + noise.Perlin(nx * 3.1f, ny * 3.1f) * variation * 0.4f;
                float m = 1f + v;
                pixels[y * TexSize + x] = new Color(
                    Math.Clamp(baseCol.R * m, 0f, 1f),
                    Math.Clamp(baseCol.G * m, 0f, 1f),
                    Math.Clamp(baseCol.B * m, 0f, 1f),
                    1f);
            }
        }
        return pixels;
    }
}
