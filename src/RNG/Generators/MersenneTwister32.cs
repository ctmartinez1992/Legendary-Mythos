using System;
using System.Runtime.CompilerServices;

namespace LM.MyRNG.Generators {
    //MersenneTwister32 is a concrete generator class derived from Seedable which implements the MT19937 algorithm. It has a RandMax value of 2^32-1.
    public class MersenneTwister32 : Seedable<uint>, IClonable, IConcordant {
        private const uint DefaultSeed = 5489U;
        private const int BitWidth = 32;
        private const int BitWidthM2 = 30;

        private const uint InitSeedA = 19650218U;
        private const uint InitSeedB = 1664525U;
        private const uint InitSeedC = 1566083941U;

        private const int StateSize = 624;
        private const uint UMask = 0x80000000U;
        private const uint LMask = 0x7FFFFFFFU;
        private const uint Matrix = 0x9908B0DFU;
        private const int ShiftU = 11;
        private const int ShiftS = 7;
        private const uint MaskB = 0x9D2C5680U;
        private const int ShiftT = 15;
        private const uint MaskC = 0xEFC60000U;
        private const int ShiftL = 18;
        private const uint InitF = 0x6C078965U;
        private const int Offset = 397;

        private int stateIdx;
        private uint[] state = new uint[StateSize];

        //The generator is initialized in its default, non-randomized starting state.
        public MersenneTwister32() : base(UInt32.MaxValue) {
            SetSeed(DefaultSeed);
        }

        //Constructor with an option to randomize the generator.
        public MersenneTwister32(bool randomize) : base(UInt32.MaxValue) {
            if(randomize) {
                RandomizeInternalState();
            }
            else {
                SetSeed(DefaultSeed);
            }
        }

        public override int SeedLength { get { return StateSize; } }
        public override string AlgorithmName { get { return "MT19937-32"; } }

        public override ulong Next() {
            return NextImpl();
        }
        
        public override void SetSeed(uint[] seed) {
            if(seed == null) {
                SetSeed(DefaultSeed);
            }
            else if(seed.Length > 0) {
                //Pre-initialize state. This will call CopyCacheFrom()
                SetSeed(InitSeedA);

                int i = 1;
                uint j = 0;
                int jlen = seed.Length;

                int klen = jlen;
                if(klen < StateSize) {
                    klen = StateSize;
                }
                
                var sloc = state;           //Use as local.

                for(int k = 0; k < klen; ++k) {
                    uint a = sloc[i - 1];
                    sloc[i] = (sloc[i] ^ ((a ^ (a >> BitWidthM2)) * InitSeedB)) + seed[j] + j;

                    if(++i == StateSize) {
                        sloc[0] = sloc[StateSize - 1];
                        i = 1;
                    }

                    if(++j == jlen) {
                        j = 0;
                    }
                }

                for(int k = 1; k < StateSize; ++k) {
                    uint a = sloc[i - 1];
                    sloc[i] = (sloc[i] ^ ((a ^ (a >> BitWidthM2)) * InitSeedC)) - (uint)i;

                    if(++i == StateSize) {
                        sloc[0] = sloc[StateSize - 1];
                        i = 1;
                    }
                }

                sloc[0] = 0x01U << (BitWidth - 1);
            }
        }

        //Overrides Seedable.SetSeed(ulong) to implement the MT19937 integer seeding routine.
        public override void SetSeed(ulong seed) {
            uint a;
            var sloc = state;

            sloc[0] = (uint)seed;
            for(int n = 1; n < StateSize; ++n) {
                a = sloc[n - 1];
                sloc[n] = InitF * (a ^ (a >> BitWidthM2)) + (uint)n;
            }

            stateIdx = StateSize;
            
            ResetCache();           //Important to reset base cache.
        }

        public virtual RNG CloneInstance() {
            //MemberwiseClone is faster here, as it avoid state initialization in constructor.
            //Note: There must be no reference types in the base classes, otherwise we can't use this method.
            var clone = (MersenneTwister32)MemberwiseClone();

            //No need to copy value fields, but we need new array instance.
            clone.state = new uint[StateSize];
            Buffer.BlockCopy(state, 0, clone.state, 0, StateSize * sizeof(uint));
            
            return clone;
        }

        public virtual bool IsConcordant(RNG other) {
            if(IsBaseConcordant(other)) {
                var temp = (MersenneTwister32)other;
                if(temp.stateIdx == stateIdx) {
                    var sloc = state;
                    var tloc = temp.state;

                    for(int n = 0; n < StateSize; ++n) {
                        if(tloc[n] != sloc[n]) {
                            return false;
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        //Override for performance. Disable to test base methods.
        public override RNG NewInstance() {
            return new MersenneTwister32();
        }

        public override uint Next32() {
            return NextImpl();
        }

        public override ulong Next64() {
            return ((ulong)NextImpl() << 32) | NextImpl();
        }

        public override void Discard(long count) {
            int idx = stateIdx;
            int max = StateSize + 1;

            while(--count > -1) {
                if(++idx == max) {
                    Reload();
                    idx = 1;
                }
            }

            stateIdx = idx;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected uint NextImpl() {
            if(stateIdx == StateSize) {
                Reload();
                stateIdx = 0;
            }

            uint y = state[stateIdx++];

            y ^= (y >> ShiftU);
            y ^= (y << ShiftS) & MaskB;
            y ^= (y << ShiftT) & MaskC;

            return y ^ (y >> ShiftL);
        }

        private void Reload() {
            int nrot = 1;
            int mrot = Offset;

            var sloc = state;

            for(int n = 0; n < StateSize; ++n) {
                uint x = ((sloc[n] & UMask) | (sloc[nrot] & LMask));
                uint xa = x >> 1;

                if((x & 0x01U) == 1) {
                    xa ^= Matrix;
                }

                sloc[n] = sloc[mrot] ^ xa;

                if(++nrot == StateSize) {
                    nrot = 0;
                }
                if(++mrot == StateSize) {
                    mrot = 0;
                }
            }
        }
    }
}