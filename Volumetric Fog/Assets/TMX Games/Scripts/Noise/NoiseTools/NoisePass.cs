using System.Collections;
using System.Collections.Generic;
using TMX.Utils;
using UnityEngine;

namespace Noise
{
    [System.Serializable]
    public class NoisePass
    {
        public string passName;

        public bool skip = false;

        public enum NoiseType { Perlin, Worley }

        public NoiseType noiseType = NoiseType.Perlin;

        public enum OperationType { Add, Multiply, Screen, FrequencyMod }

        public OperationType operationType = OperationType.Add;

        public bool saturate;
        public bool oneMinus;

        [Range(1, 100)]
        public int frequency = 1;

        [Range(1, 10)]
        public int lacunarity = 2;

        [Range(-10f, 10f)]
        public float amplitude = 1f;

        [Range(-2f, 2f)]
        public float persistence = .5f;

        [Range(1, 4)]
        public int octaves = 1;

        [Range(-1000, 1000)]
        public int seed;

        [Range(-10f, 10f)]
        public float multiplier = 1f;

        public bool useScale;
        public ScaledCurve scale;

        public NoiseTools.NoiseGeneratorBase noise;

        public NoisePass (NoiseType type)
        {
            noiseType = type;
        }

        public void Init ()
        {
            if (noiseType == NoiseType.Perlin)
                noise = new NoiseTools.PerlinNoise(frequency, 1, seed, lacunarity, amplitude, persistence);
            else
                noise = new NoiseTools.WorleyNoise(frequency, 1, seed, lacunarity, amplitude, persistence);
        }

        public float GetValue (Vector3 pos)
        {
            return (octaves > 1) ? noise.GetFractal(pos, octaves) : noise.GetAt(pos);
        }

        public float ApplyOperation (Vector3 pos, float input)
        {
            float value = GetValue(pos);

            if (saturate)
                value = Mathf.Clamp01(value);

            if (oneMinus)
                value = 1f - value;

            value *= multiplier;

            if (useScale/* && scale.curve != null*/)
                value = scale.GetValue(value);

            switch (operationType)
            {
                case OperationType.Add:
                    value = input + value;
                    break;

                case OperationType.Multiply:
                    value = input * value;
                    break;

                case OperationType.Screen:
                    value = 1f - (1f - input) * (1f - value);
                    break;

                case OperationType.FrequencyMod:
                    break;
            }

            return value;
        }

        public NoisePass Clone ()
        {
            var clone = new NoisePass(noiseType);
            clone.skip = skip;
            clone.operationType = operationType;
            clone.saturate = saturate;
            clone.oneMinus = oneMinus;

            clone.frequency = frequency;
            clone.lacunarity = lacunarity;
            clone.amplitude = amplitude;
            clone.persistence = persistence;
            clone.octaves = octaves;
            clone.seed = seed;
            clone.multiplier = multiplier;

            clone.useScale = useScale;
            clone.scale = scale.Clone();

            //var clone = MemberwiseClone() as NoisePass;

            return clone;
        }
    }
}
