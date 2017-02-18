namespace NoiseTools
{
    public struct XXHash
    {
        #region Private Members

        const uint PRIME32_1 = 2654435761U;
        const uint PRIME32_2 = 2246822519U;
        const uint PRIME32_3 = 3266489917U;
        const uint PRIME32_4 = 668265263U;
        const uint PRIME32_5 = 374761393U;

        static uint rotl32(uint x, int r)
        {
            return (x << r) | (x >> 32 - r);
        }

        #endregion

        #region Static Functions

        public static uint GetHash(int data, int seed)
        {
            uint h32 = (uint)seed + PRIME32_5;
            h32 += 4U;
            h32 += (uint)data * PRIME32_3;
            h32 = rotl32(h32, 17) * PRIME32_4;
            h32 ^= h32 >> 15;
            h32 *= PRIME32_2;
            h32 ^= h32 >> 13;
            h32 *= PRIME32_3;
            h32 ^= h32 >> 16;
            return h32;
        }

        #endregion
    }
}
