using UnityEngine;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

namespace Noise
{
    public class NoiseVolume : ScriptableObject
    {
        #region Asset creation

        //public List<NoiseSettings> noisePasses_R;
        //public List<NoiseSettings> noisePasses_G;
        //public List<NoiseSettings> noisePasses_B;
        //public List<NoiseSettings> noisePasses_A;

        public NoiseChannel noisePasses_R = new NoiseChannel(true);
        public NoiseChannel noisePasses_G = new NoiseChannel(false);
        public NoiseChannel noisePasses_B = new NoiseChannel(false);
        public NoiseChannel noisePasses_A = new NoiseChannel(false);

        [SerializeField]
        public int resolution = 64;

        [SerializeField]
        public bool useMipmaps = false;

        [SerializeField]
        public TextureFormat format = TextureFormat.RGBAFloat;

        [SerializeField]
        public FilterMode filterMode = FilterMode.Bilinear;

        [Range(-5f, 5f)]
        public float mipMapBias = 0f;

        [SerializeField]
        public System.Threading.ThreadPriority threadPriority = System.Threading.ThreadPriority.AboveNormal;

        [SerializeField, HideInInspector]
        Texture3D _texture;


        static Object threadLock = new Object();
        public static int completedThreads = 0;
        public static int completedPixels = 0;
        static int size = 32;
        public static List<Thread> threads = new List<Thread>(8);
        public static int threadCount = 4;
        static bool multiOctave = false;

        static Color[] pixelBuffer;
        //static NoiseTools.NoiseGeneratorBase noise;
        public static float progress;
        public static string progressText;

        public static System.DateTime start;
        public static float timeElapsed { get { return (float)System.DateTime.Now.Subtract(start).TotalSeconds; } }

        public Texture3D texture {
            get { return _texture; }
        }

        void OnEnable ()
        {
            if (_texture == null || _texture.width != resolution || format != _texture.format)
            {
                _texture = new Texture3D(
                    resolution,
                    resolution,
                    resolution,
                    format, useMipmaps
                );
                _texture.name = "Texture3D";
                RebuildTexture();
            }
        }

        public void ChangeResolution (int newResolution)
        {
            //DestroyImmediate(_texture);

            _texture = new Texture3D(
                newResolution,
                newResolution,
                newResolution,
                format, useMipmaps
            );
            _texture.name = "Texture3D";
        }

        public void RebuildTexture ()
        {
            if (_texture == null)
            {
                Debug.LogError("Texture3D asset is missing.");
                return;
            }

            _texture.filterMode = filterMode;
            _texture.mipMapBias = mipMapBias;

            size = _texture.width;

            pixelBuffer = _texture.GetPixels();
            //pixelBuffer = new Color[size * size * size];

            threadCount = SystemInfo.processorCount;

            completedThreads = 0;
            completedPixels = 0;

            noisePasses_R.Init();
            noisePasses_G.Init();
            noisePasses_B.Init();
            noisePasses_A.Init();

            multiOctave = (noisePasses_R.multiOctave || noisePasses_G.multiOctave || noisePasses_B.multiOctave || noisePasses_A.multiOctave);

            int workloadPerThread = Mathf.CeilToInt(size / threadCount);

            ClearThreads();

            for (int i = 0; i < threadCount; i++)
            {
                var start = (i * workloadPerThread).ToString();
                var end = ((i < threadCount - 1) ? workloadPerThread * (i + 1) : size).ToString();
                Thread newThread = new Thread(() => CalcThread(start, end));
                newThread.Priority = threadPriority;
                newThread.Start();
            }

            start = System.DateTime.Now;
        }

        public IEnumerator CompletionWait ()
        {
            while (completedThreads < threadCount)
            {
                //Debug.LogFormat("Waiting ({0} / {1})", completedThreads, threadCount);
                //progress = completedThreads / ((float)threads);
                progress = completedPixels / (float)(size * size * size);
                progressText = string.Format("Generating Texture || {0} Complete ({1} / {2} Pixels) || {3} Seconds Elapsed", progress.ToString("P2"), completedPixels, size * size * size, timeElapsed.ToString("N3"));
                yield return null;
            }

            _texture.SetPixels(pixelBuffer);
            _texture.Apply(useMipmaps);

            Debug.LogFormat("<b>Completed Texture Generation After {0} Seconds</b>", timeElapsed.ToString("N3"));
            //Debug.LogFormat("<b>Completed Texture Generation After {0} Seconds ({1})</b>", timeElapsed.ToString("N3"), timeElapsed.ToString("E3"));
        }

        public void ClearThreads ()
        {
            for (int i = 0; i < threads.Count; i++)
            {
                threads[i].Abort();
            }

            threads.Clear();
        }

        public void CalcThread (string startString, string endString)
        {
            int startX = int.Parse(startString);
            int endX = int.Parse(endString);
            var scale = 1.0f / size;

            var index = 0;
            Color pixelColor;

            float r, g, b, a;
            float x, y, z;

            var channel_R = noisePasses_R.Clone();
            var channel_G = noisePasses_G.Clone();
            var channel_B = noisePasses_B.Clone();
            var channel_A = noisePasses_A.Clone();

            channel_R.Init();
            channel_G.Init();
            channel_B.Init();
            channel_A.Init();

            Vector3 pos;

            //Debug.Log("Starting Thread " + startZ + " || " + endZ);

            for (int ix = startX; ix < endX; ix++)
            {
                x = scale * ix;

                for (int iy = 0; iy < size; iy++)
                {
                    y = scale * iy;

                    for (int iz = 0; iz < size; iz++)
                    {
                        z = scale * iz;
                        pos = new Vector3(x, y, z);

                        index = ix * size * size + iy * size + iz;

                        r = channel_R.GetValue(pos, pixelBuffer[index].r);
                        g = channel_G.GetValue(pos, pixelBuffer[index].g);
                        b = channel_B.GetValue(pos, pixelBuffer[index].b);
                        a = channel_A.GetValue(pos, pixelBuffer[index].a);

                        //lock (threadLock)
                        //{
                        //    r = noisePasses_R.GetValue(pos);
                        //    g = noisePasses_G.GetValue(pos);
                        //    b = noisePasses_B.GetValue(pos);
                        //    a = noisePasses_A.GetValue(pos);
                        //}

                        pixelColor = new Color(r, g, b, a);
                        pixelBuffer[index] = pixelColor;
                    }

                    //if (multiOctave)
                    //{
                    //    Debug.LogWarning("Finished Y " + iy);
                    //}

                    lock (threadLock)
                        completedPixels += size;

                    Thread.Sleep(1);
                }

                //if(multiOctave)
                //    Debug.LogWarning("Finished X " + ix);
                Thread.Sleep(1);

                //lock (threadLock)
                //    completedPixels++;
            }

            lock (threadLock)
                completedThreads++;
        }

        public List<NoisePass> InitNoiseChannel (List<NoisePass> settings)
        {
            var newNoise = new List<NoisePass>(settings.Count);
            newNoise.AddRange(settings);

            for (int i = 0; i < newNoise.Count; i++)
            {
                newNoise[i].Init();
            }

            return newNoise;
        }

        public float ApplyChannel (List<NoisePass> settings, Vector3 pos)
        {
            float value = 0f;

            for (int i = 0; i < settings.Count; i++)
            {
                if(!settings[i].skip)
                    value = settings[i].ApplyOperation(pos, value);
            }

            return value;
        }

        #endregion
    }
}
