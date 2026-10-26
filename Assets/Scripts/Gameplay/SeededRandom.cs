namespace WordDrop.Gameplay
{
    /// <summary>
    /// A small, fully specified random generator (xorshift32). Same seed, same
    /// numbers on every device and runtime, which System.Random does not promise.
    /// Used by the Daily Challenge so everyone gets the same pieces (GDD 4.3).
    /// </summary>
    public class SeededRandom : System.Random
    {
        private uint state;

        public SeededRandom(int seed)
        {
            state = (uint)seed ^ 0x9E3779B9u;
            if (state == 0)
            {
                state = 0x6D2B79F5u;
            }
        }

        private uint NextUInt()
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return state;
        }

        protected override double Sample() => NextUInt() / 4294967296.0;

        public override double NextDouble() => Sample();

        public override int Next() => (int)(NextUInt() >> 1);

        public override int Next(int maxValue) => maxValue <= 0 ? 0 : (int)(Sample() * maxValue);

        public override int Next(int minValue, int maxValue) => minValue + Next(maxValue - minValue);
    }
}
