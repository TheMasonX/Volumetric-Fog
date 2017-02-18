using UnityEngine;

namespace NoiseTools
{
    public class RidgedMultiFractalNoise : NoiseGeneratorBase
    {
        public float weight = 1.0f;
        public float[] weights;

        #region Constructor

        public RidgedMultiFractalNoise (int frequency, int repeat, int seed = 0, int lacunarity = 2, float amplitude = 1f, float persistence = .5f)
        : base(frequency, repeat, seed, lacunarity, amplitude, persistence)
        {
            SetWeights();
        }

        void SetWeights ()
        {
            weights = new float[4];
            float f = 1f;
            for (int i = 0; i < 4; i++)
            {
                weights[i] = Mathf.Pow(f, -1f);
                f *= _lacunarity;
            }
        }

        #endregion

        #region Private members

        static float Fade(float t)
        {
            return t * t * t * (t * (t * 6 - 15) + 10);
        }

        static float Lerp(float t, float a, float b)
        {
            return a + t * (b - a);
        }

        static float Grad(int hash, float x, float y)
        {
            return ((hash & 1) == 0 ? x : -x) + ((hash & 2) == 0 ? y : -y);
        }

        static float Grad(int hash, float x, float y, float z)
        {
            var h = hash & 15;
            var u = h < 8 ? x : y;
            var v = h < 4 ? y : (h == 12 || h == 14 ? x : z);
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }

        #endregion

        #region 2D noise

        protected override float Calculate2D(Vector2 point)
        {
            var x = point.x * Frequency;
            var y = point.y * Frequency;

            var cx = Mathf.FloorToInt(x);
            var cy = Mathf.FloorToInt(y);

            x -= cx;
            y -= cy;

            var u = Fade(x);
            var v = Fade(y);

            var h00 = Hash(CellID(cx    , cy    ));
            var h01 = Hash(CellID(cx + 1, cy    ));
            var h10 = Hash(CellID(cx    , cy + 1));
            var h11 = Hash(CellID(cx + 1, cy + 1));

            var n = Lerp(v, Lerp(u, Grad(h00, x, y  ), Grad(h01, x-1, y  )),
                            Lerp(u, Grad(h10, x, y-1), Grad(h11, x-1, y-1)));

            return n * 0.5f + 0.5f;
        }

        #endregion

        #region 3D noise

        protected override float Calculate3DFractal (Vector3 point, int level)
        {
            var originalFreq = _freq;
            var originalRepeat = _repeat;

            var sum = 0.0f;
            float value;
            weight = 1.0f;

            if(weights == null)
                SetWeights();

            for (var i = 0; i < level; i++)
            {
                value = Calculate3D(point);
                sum += value * weights[i];
                _freq *= _lacunarity;
                _repeat *= _lacunarity;
                weight = value * _persistence;
                weight = Mathf.Clamp01(weight);
            }

            _freq = originalFreq;
            _repeat = originalRepeat;

            //return sum;
            return (sum * 1.25f) - 1.0f;
        }

        protected override float Calculate3D(Vector3 point)
        {
            var x = point.x * _freq;
            var y = point.y * _freq;
            var z = point.z * _freq;
            
            var cx = Mathf.FloorToInt(x);
            var cy = Mathf.FloorToInt(y);
            var cz = Mathf.FloorToInt(z);

            x -= cx;
            y -= cy;
            z -= cz;

            var u = Fade(x);
            var v = Fade(y);
            var w = Fade(z);

            var h000 = Hash(CellID(cx    , cy    , cz    ));
            var h001 = Hash(CellID(cx + 1, cy    , cz    ));
            var h010 = Hash(CellID(cx    , cy + 1, cz    ));
            var h011 = Hash(CellID(cx + 1, cy + 1, cz    ));
            var h100 = Hash(CellID(cx    , cy    , cz + 1));
            var h101 = Hash(CellID(cx + 1, cy    , cz + 1));
            var h110 = Hash(CellID(cx    , cy + 1, cz + 1));
            var h111 = Hash(CellID(cx + 1, cy + 1, cz + 1));

            var n = Lerp(w, Lerp(v, Lerp(u, Grad(h000, x, y  , z  ), Grad(h001, x-1, y  , z  )),
                                    Lerp(u, Grad(h010, x, y-1, z  ), Grad(h011, x-1, y-1, z  ))),
                            Lerp(v, Lerp(u, Grad(h100, x, y  , z-1), Grad(h101, x-1, y  , z-1)),
                                    Lerp(u, Grad(h110, x, y-1, z-1), Grad(h111, x-1, y-1, z-1))));

            n = Mathf.Abs(n);
            n = _amplitude - n;
            n *= n;
            n *= weight;

            return n;
        }

        #endregion
    }
}
