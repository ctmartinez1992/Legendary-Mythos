using System;
using System.Runtime.CompilerServices;

namespace LM.MyRNG.Generators {
    //SplitMix64 is a concrete generator class derived from Seedable which implements the SPLITMIX64 algorithm. It has a RandMax value of 2^64-1.
    //The SPLITMIX64 algorithm is a fixed increment version of Java 8's SplittableRandom generator.
    //It is a very fast generator which passes "Big Crush" (See TestU01) and is useful where only 64 bits of state is required.
    //Otherwise, xoroshiro128+ or xorshift1024* are recommended.
    //The initial state (without randomization) is set to 0x0123456789ABCDEF.
    //From this state, the first three calls to Next() generate: 0x157A3807A48FAA9D, 0xD573529B34A1D093, 0x2F90B72E996DCCBE.
    //This implementation is derived from code put in the public domain by Sebastiano Vigna.
    //Refer to: http://xoroshiro.di.unimi.it/splitmix64.c
    public class SplitMix64 : Seedable<ulong>, IClonable, IConcordant {
        private const ulong DefaultSeed = 0x0123456789ABCDEFUL;
        private ulong state = DefaultSeed;

        //The generator is initialized in its default, non-randomized starting state.
        public SplitMix64() : base(UInt64.MaxValue) {
        }

        //Constructor with an option to randomize the generator.
        public SplitMix64(bool randomize) : base(UInt64.MaxValue) {
            if(randomize) {
                RandomizeInternalState();
            }
        }

        public override ulong Next() {
            return NextImpl();
        }

        public override int SeedLength { get { return 1; } }
        public override string AlgorithmName { get { return "Split Mix 64"; } }

        public override void SetSeed(ulong[] seed) {
            if(seed == null) {
                state = DefaultSeed;

                // Important to reset base cache
                ResetCache();
            }
            else if(seed.Length > 0) {
                state = seed[0];
                ResetCache();
            }
        }

        public RNG CloneInstance() {
            var clone = (SplitMix64)CloneBaseRng();
            clone.state = state;

            return clone;
        }

        public bool IsConcordant(RNG other) {
            if(IsBaseConcordant(other)) {
                return (state == ((SplitMix64)other).state);
            }

            return false;
        }

        //Override for performance. Disable to test base methods.
        public override RNG NewInstance() {
            return new SplitMix64();
        }
        public override uint Next32() {
            return (uint)NextImpl();
        }
        public override ulong Next64() {
            return NextImpl();
        }
        public override void Discard(long count) {
            while(--count > -1) {
                state += 0x9E3779B97F4A7C15UL;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ulong NextImpl() {
            ulong z = (state += 0x9E3779B97F4A7C15UL);
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;

            return z ^ (z >> 31);
        }
    }
}