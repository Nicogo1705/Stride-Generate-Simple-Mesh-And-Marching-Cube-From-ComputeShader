using System;

namespace StrideMarchingCubeSystem;

/// <summary>
/// A small, dependency-free gradient-noise generator (classic Perlin) with
/// fractal (FBM) and ridged helpers. Seeded and deterministic. Values are
/// (roughly) in [-1, 1] for the base noise; the fractal helpers normalise back
/// into [-1, 1]. This is deliberately compact — enough to drive interesting
/// terrain without pulling in a full noise library.
/// </summary>
public sealed class Noise
{
    private readonly int[] _perm = new int[512];

    public Noise(int seed)
    {
        // Fisher–Yates shuffle of 0..255 with a small deterministic PRNG.
        var p = new int[256];
        for (int i = 0; i < 256; i++) p[i] = i;

        uint state = (uint)seed * 2654435761u + 1013904223u;
        for (int i = 255; i > 0; i--)
        {
            state = state * 1664525u + 1013904223u;
            int j = (int)(state % (uint)(i + 1));
            (p[i], p[j]) = (p[j], p[i]);
        }
        for (int i = 0; i < 512; i++) _perm[i] = p[i & 255];
    }

    private static float Fade(float t) => t * t * t * (t * (t * 6 - 15) + 10);
    private static float Lerp(float a, float b, float t) => a + t * (b - a);

    private static float Grad(int hash, float x, float y)
    {
        // 8 gradient directions in 2D.
        switch (hash & 7)
        {
            case 0: return  x + y;
            case 1: return  x - y;
            case 2: return -x + y;
            case 3: return -x - y;
            case 4: return  x;
            case 5: return -x;
            case 6: return  y;
            default: return -y;
        }
    }

    private static float Grad(int hash, float x, float y, float z)
    {
        int h = hash & 15;
        float u = h < 8 ? x : y;
        float v = h < 4 ? y : (h == 12 || h == 14 ? x : z);
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }

    /// <summary>2D Perlin noise, ~[-1, 1].</summary>
    public float Perlin(float x, float y)
    {
        int xi = (int)MathF.Floor(x) & 255;
        int yi = (int)MathF.Floor(y) & 255;
        float xf = x - MathF.Floor(x);
        float yf = y - MathF.Floor(y);
        float u = Fade(xf), v = Fade(yf);

        int aa = _perm[_perm[xi] + yi];
        int ab = _perm[_perm[xi] + yi + 1];
        int ba = _perm[_perm[xi + 1] + yi];
        int bb = _perm[_perm[xi + 1] + yi + 1];

        float x1 = Lerp(Grad(aa, xf, yf), Grad(ba, xf - 1, yf), u);
        float x2 = Lerp(Grad(ab, xf, yf - 1), Grad(bb, xf - 1, yf - 1), u);
        return Lerp(x1, x2, v);
    }

    /// <summary>3D Perlin noise, ~[-1, 1].</summary>
    public float Perlin(float x, float y, float z)
    {
        int xi = (int)MathF.Floor(x) & 255;
        int yi = (int)MathF.Floor(y) & 255;
        int zi = (int)MathF.Floor(z) & 255;
        float xf = x - MathF.Floor(x);
        float yf = y - MathF.Floor(y);
        float zf = z - MathF.Floor(z);
        float u = Fade(xf), v = Fade(yf), w = Fade(zf);

        int a  = _perm[xi] + yi;
        int aa = _perm[a] + zi;
        int ab = _perm[a + 1] + zi;
        int b  = _perm[xi + 1] + yi;
        int ba = _perm[b] + zi;
        int bb = _perm[b + 1] + zi;

        float x1 = Lerp(Grad(_perm[aa], xf, yf, zf), Grad(_perm[ba], xf - 1, yf, zf), u);
        float x2 = Lerp(Grad(_perm[ab], xf, yf - 1, zf), Grad(_perm[bb], xf - 1, yf - 1, zf), u);
        float y1 = Lerp(x1, x2, v);

        x1 = Lerp(Grad(_perm[aa + 1], xf, yf, zf - 1), Grad(_perm[ba + 1], xf - 1, yf, zf - 1), u);
        x2 = Lerp(Grad(_perm[ab + 1], xf, yf - 1, zf - 1), Grad(_perm[bb + 1], xf - 1, yf - 1, zf - 1), u);
        float y2 = Lerp(x1, x2, v);

        return Lerp(y1, y2, w);
    }

    /// <summary>Fractal Brownian motion (summed octaves), normalised to ~[-1, 1].</summary>
    public float Fbm(float x, float y, int octaves, float lacunarity = 2f, float gain = 0.5f)
    {
        float sum = 0f, amp = 1f, freq = 1f, norm = 0f;
        for (int o = 0; o < octaves; o++)
        {
            sum += Perlin(x * freq, y * freq) * amp;
            norm += amp;
            amp *= gain;
            freq *= lacunarity;
        }
        return norm > 0 ? sum / norm : 0f;
    }

    /// <summary>3D FBM, normalised to ~[-1, 1].</summary>
    public float Fbm(float x, float y, float z, int octaves, float lacunarity = 2f, float gain = 0.5f)
    {
        float sum = 0f, amp = 1f, freq = 1f, norm = 0f;
        for (int o = 0; o < octaves; o++)
        {
            sum += Perlin(x * freq, y * freq, z * freq) * amp;
            norm += amp;
            amp *= gain;
            freq *= lacunarity;
        }
        return norm > 0 ? sum / norm : 0f;
    }

    /// <summary>Ridged multifractal — sharp mountain ridges. Output ~[0, 1].</summary>
    public float Ridged(float x, float y, int octaves, float lacunarity = 2f, float gain = 0.5f)
    {
        float sum = 0f, amp = 1f, freq = 1f, norm = 0f;
        for (int o = 0; o < octaves; o++)
        {
            float n = 1f - MathF.Abs(Perlin(x * freq, y * freq));
            n *= n;
            sum += n * amp;
            norm += amp;
            amp *= gain;
            freq *= lacunarity;
        }
        return norm > 0 ? sum / norm : 0f;
    }
}
