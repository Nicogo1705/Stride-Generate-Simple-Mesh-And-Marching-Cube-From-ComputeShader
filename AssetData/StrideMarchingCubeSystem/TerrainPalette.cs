using Stride.Core.Mathematics;
using Stride.Graphics;

namespace StrideMarchingCubeSystem;

/// <summary>
/// The colour of each <see cref="TerrainType"/>. The GPU blends a dominant and a
/// secondary colour per surface triangle. Colours are uploaded as a tiny Nx1 texture
/// (<see cref="BuildTexture"/>) so the shader needs no external image assets — swap in
/// your own palette (or point the material at real textures) to reskin the terrain.
/// </summary>
public static class TerrainPalette
{
    /// <summary>Palette width. Indices beyond the defined materials fall back to grey.</summary>
    public const int Size = 16;

    /// <summary>A natural default palette matching <see cref="TerrainType"/>.</summary>
    public static Color4[] Default()
    {
        var c = new Color4[Size];
        for (int i = 0; i < Size; i++) c[i] = new Color4(0.5f, 0.5f, 0.5f, 1f);
        c[(int)TerrainType.Rock]  = new Color4(0.42f, 0.40f, 0.38f, 1f);
        c[(int)TerrainType.Dirt]  = new Color4(0.35f, 0.25f, 0.16f, 1f);
        c[(int)TerrainType.Grass] = new Color4(0.28f, 0.52f, 0.18f, 1f);
        c[(int)TerrainType.Sand]  = new Color4(0.76f, 0.70f, 0.50f, 1f);
        c[(int)TerrainType.Snow]  = new Color4(0.92f, 0.94f, 0.96f, 1f);
        return c;
    }

    /// <summary>Builds the Nx1 palette texture consumed by TerrainDiffuse.sdsl.</summary>
    public static Texture BuildTexture(GraphicsDevice device, Color4[] colors)
    {
        var texels = new Color[Size];
        for (int i = 0; i < Size; i++)
        {
            var src = i < colors.Length ? colors[i] : new Color4(0.5f, 0.5f, 0.5f, 1f);
            texels[i] = new Color(src.R, src.G, src.B, src.A);
        }
        return Texture.New2D(device, Size, 1, PixelFormat.R8G8B8A8_UNorm_SRgb, texels);
    }
}
