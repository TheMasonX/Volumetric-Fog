using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace TMX.Utils
{
    public static class MathUtils
    {
        public static readonly float TAU = Mathf.PI * 2f;

        public static int GetGridIndex (Vector2Int pos, int resolution)
        {
            return GetGridIndex(pos.x, pos.y, resolution);
        }

        public static int GetGridIndex (int x, int y, int resolution)
        {
            return x + y * resolution;
        }

        public static Vector2 GetUVFromPosition (Vector3 position, float size)
        {
            return new Vector2(position.x / size + .5f, position.z / size + .5f);
        }

        public static Vector3 GetPositionFromUV (Vector2 uv, float size)
        {
            uv.x -= .5f;
            uv.y -= .5f;
            return new Vector3(uv.x * size, 0f, uv.y * size);
        }

        public static float Interpolate (this float[] grid, Vector2 uv, int resolution)
        {
            uv *= resolution;
            Vector2Int floored = Vector2Int.Floor(uv);
            Vector2Int ceiled = Vector2Int.Ceil(uv);


            floored.x = Mathf.Clamp(floored.x, 0, resolution - 1);
            floored.y = Mathf.Clamp(floored.y, 0, resolution - 1);
            ceiled.x = Mathf.Clamp(ceiled.x, 0, resolution - 1);
            ceiled.y = Mathf.Clamp(ceiled.y, 0, resolution - 1);

            Vector2 lerpValues = uv - (Vector2)floored;

            var topLeft = grid[GetGridIndex(floored.x, floored.y, resolution)];
            var topRight = grid[GetGridIndex(ceiled.x, floored.y, resolution)];

            var bottomLeft = grid[GetGridIndex(floored.x, ceiled.y, resolution)];
            var bottomRight = grid[GetGridIndex(ceiled.x, ceiled.y, resolution)];

            return Mathf.Lerp(Mathf.Lerp(topLeft, topRight, lerpValues.x), Mathf.Lerp(bottomLeft, bottomRight, lerpValues.x), lerpValues.y);
        }

        public static float PercentBetween (float value, Vector2 range)
        {
            return PercentBetween(value, range.x, range.y);
        }

        public static float PercentBetween (float value, float min, float max)
        {
            return (value - min) / (max - min);
        }

        public static float Remap (float value, Vector2 range)
        {
            return Remap(value, range.x, range.y);
        }

        public static float Remap (float value, float min, float max)
        {
            return value * (max - min) + min;
        }

        public static int MeshesAtDepth (int depth)
        {
            return (Pow(4, depth + 1) - 1) / 3;
        }

        public static Vector3 GetCircularPositionV3 (float angle, float radius)
        {
            return GetCircularPositionV3(angle) * radius;
        }

        public static Vector3 GetCircularPositionV3 (float angle)
        {
            return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        }

        public static Vector2 GetCircularPosition (float angle, float radius)
        {
            return GetCircularPosition(angle) * radius;
        }

        public static Vector2 GetCircularPosition (float angle)
        {
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        public static int Pow (int b, int pow)
        {
            if (pow < 0)
                throw new System.Exception("Int Pow() Requires A Non-Negative Power");

            int result = 1;
            for (int i = 0; i < pow; i++)
            {
                result *= b;
            }
            return result;
        }

        public static int PowOfTwo (int pow)
        {
            return 1 << pow;
        }

        #region Operations

        public static void Add (this Vector2 a, float b)
        {
            a.x += b;
            a.y += b;
        }

        #endregion

        #region List Extensions

        public static void TryAdd (this IList collection, object value)
        {
            if(!collection.Contains(value))
            {
                collection.Add(value);
            }
        }

        public static void Shuffle (this IList collection)
        {
            int n, i;
            var t = collection[0];
            n = collection.Count;
            while (n > 0)
            {
                n--;
                i = Random.Range(0, n); // 0 ≤ i < n
                t = collection[n];
                collection[n] = collection[i];
                collection[i] = t;
            }
        }

        public static int GetRandom (this IList collection, int startIndex = 0)
        {
            return collection.GetRandom(startIndex, collection.Count);
        }

        public static int GetRandom (this IList collection, int startIndex, int endIndex)
        {
            return Random.Range(startIndex, endIndex);
        }

        public static T Pop<T> (this IList<T> collection, int index)
        {
            var value = collection[index];
            collection.RemoveAt(index);
            return value;
        }
        #endregion
    }
}