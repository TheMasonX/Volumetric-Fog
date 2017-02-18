using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Noise
{
    [System.Serializable]
    public class NoiseChannel
    {
        public bool skip = false;

        public float initialValue = 0f;

        public List<NoisePass> noisePasses = new List<NoisePass>(5);

        [HideInInspector]
        public bool multiOctave = false;

        public NoiseChannel (bool initialPass)
        {
            if (initialPass)
            {
                noisePasses.Add(new NoisePass(NoisePass.NoiseType.Perlin));
            }
        }

        public void Init ()
        {
            multiOctave = false;

            for (int i = 0; i < noisePasses.Count; i++)
            {
                noisePasses[i].Init();
                if (noisePasses[i].octaves > 1)
                    multiOctave = true;
            }
        }

        public float GetValue (Vector3 pos)
        {
            float value = initialValue;

            for (int i = 0; i < noisePasses.Count; i++)
            {
                if (!noisePasses[i].skip)
                {
                    if (noisePasses[i].operationType == NoisePass.OperationType.FrequencyMod)
                    {
                        pos *= noisePasses[i].ApplyOperation(pos, value);
                    }
                    else
                    {
                        value = noisePasses[i].ApplyOperation(pos, value);
                    }
                }
            }

            return value;
        }

        public NoiseChannel Clone ()
        {
            var clone = new NoiseChannel(false);

            clone.skip = skip;
            clone.initialValue = initialValue;

            for (int i = 0; i < noisePasses.Count; i++)
            {
                clone.noisePasses.Add(noisePasses[i].Clone());
            }

            //var clone = MemberwiseClone() as NoiseChannel;

            //clone.noisePasses.Clear();

            //for (int i = 0; i < noisePasses.Count; i++)
            //{
            //    var clonePass = noisePasses[i].Clone();
            //    Debug.Log(clonePass.noiseType + " || " + clonePass.multiplier);
            //    clone.noisePasses.Add(clonePass);
            //}

            return clone;
        }
    }
}
