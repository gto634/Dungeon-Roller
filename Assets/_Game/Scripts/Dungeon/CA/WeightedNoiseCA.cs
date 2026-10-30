using System;
using System.Collections.Generic;

public static class WeightedNoiseCA
{
    public static NoiseMapCA Generate(int width, int height, IReadOnlyList<float> weights, int seed)
    {
        if (width < 1) throw new ArgumentOutOfRangeException(nameof(width));
        if (height < 1) throw new ArgumentOutOfRangeException(nameof(height));
        double[] cumulative = BuildDistribution(weights, out double total);
        var random = new Random(seed);
        var noise = new float[width, height];
        var states = new int[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float value = (float)random.NextDouble();
                noise[x, y] = value;
                states[x, y] = Select(value, cumulative, total);
            }
        }
        return new NoiseMapCA(noise, states, weights.Count, seed);
    }

    public static int Quantize(float noise, IReadOnlyList<float> weights)
    {
        if (float.IsNaN(noise) || noise < 0f || noise > 1f) throw new ArgumentOutOfRangeException(nameof(noise));
        return Select(noise, BuildDistribution(weights, out double total), total);
    }

    static double[] BuildDistribution(IReadOnlyList<float> weights, out double total)
    {
        if (weights == null || weights.Count == 0) throw new ArgumentException("Add at least one state.", nameof(weights));
        var cumulative = new double[weights.Count];
        total = 0d;
        for (int i = 0; i < weights.Count; i++)
        {
            float weight = weights[i];
            if (float.IsNaN(weight) || float.IsInfinity(weight) || weight < 0f)
                throw new ArgumentException("State weights must be finite and non-negative.", nameof(weights));
            total += weight;
            cumulative[i] = total;
        }
        if (total <= 0d) throw new ArgumentException("At least one state must have a positive weight.", nameof(weights));
        return cumulative;
    }

    static int Select(float noise, double[] cumulative, double total)
    {
        double threshold = noise * total;
        int lastPositive = 0;
        double previous = 0d;
        for (int i = 0; i < cumulative.Length; i++)
        {
            if (cumulative[i] <= previous) continue;
            lastPositive = i;
            if (threshold < cumulative[i]) return i;
            previous = cumulative[i];
        }
        return lastPositive;
    }
}
