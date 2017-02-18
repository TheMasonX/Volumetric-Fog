using System;
using UnityEngine;

namespace TMX.Utils
{
    [System.Serializable]
    public struct Vector2Int
    {
        public int x;
        public int y;

        public Vector2Int (int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public static explicit operator Vector2 (Vector2Int a)
        {
            return new Vector2(a.x, a.y);
        }

        public static explicit operator Vector2Int (Vector2 a)
        {
            return new Vector2Int(Mathf.RoundToInt(a.x), Mathf.RoundToInt(a.y));
        }

        public static Vector2Int Floor (Vector2 a)
        {
            return new Vector2Int(Mathf.FloorToInt(a.x), Mathf.FloorToInt(a.y));
        }

        public static Vector2Int Ceil (Vector2 a)
        {
            return new Vector2Int(Mathf.CeilToInt(a.x), Mathf.CeilToInt(a.y));
        }

        internal void Clamp (int min, int max)
        {
            this = Clamp(this, min, max);
        }

        public static Vector2Int Clamp (Vector2Int a, int min, int max)
        {
            a.x = Mathf.Clamp(a.x, min, max);
            a.y = Mathf.Clamp(a.y, min, max);
            return a;
        }

        #region Operators

        #region Add/Subtract

        public static Vector2Int operator + (Vector2Int a, Vector2Int b)
        {
            a.x += b.x;
            a.y += b.y;
            return a;
        }

        public static Vector2Int operator + (Vector2Int a, int b)
        {
            a.x += b;
            a.y += b;
            return a;
        }

        public static Vector2Int operator - (Vector2Int a, Vector2Int b)
        {
            a.x -= b.x;
            a.y -= b.y;
            return a;
        }

        public static Vector2Int operator - (Vector2Int a, int b)
        {
            a.x -= b;
            a.y -= b;
            return a;
        }

        #endregion


        #region Multiply/Divide

        public static Vector2Int operator * (Vector2Int a, Vector2Int b)
        {
            a.x *= b.x;
            a.y *= b.y;
            return a;
        }

        public static Vector2Int operator * (Vector2Int a, int scalar)
        {
            a.x *= scalar;
            a.y *= scalar;
            return a;
        }

        public static Vector2Int operator / (Vector2Int a, Vector2Int b)
        {
            a.x /= b.x;
            a.y /= b.y;
            return a;
        }

        public static Vector2Int operator / (Vector2Int a, int scalar)
        {
            a.x /= scalar;
            a.y /= scalar;
            return a;
        }

        #endregion

        #endregion

        public override string ToString ()
        {
            return string.Format("({0}, {1})", x, y);
        }

        public override bool Equals (object obj)
        {
            var otherV2 = (Vector2Int)obj;
            return (x.Equals(otherV2.x) && y.Equals(otherV2.y));
        }

        public override int GetHashCode ()
        {
            return x * 1123581321 + y;
        }
    }
}