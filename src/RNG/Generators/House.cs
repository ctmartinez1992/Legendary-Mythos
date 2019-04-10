using System;

namespace LM.MyRNG.Generators {
    //House encapsulates an instance of the C# System.Random generator in a class derived from Seedable<int>.
    //It has a RandMax value of 2^32-1.
    //If seeded with the same value, the House.Next() and Random.Next() calls will both return the same results.
    //Unlike the System.Random() default constructor, which seeds the generator with a time value.
    //Note that the House constructor uses a static value, 0x1234ABCD, as the seed.
    //Call RandomizeInternalState() to put the generator into a unique state.
    //According to Microsoft documentation, the System.Random generator uses a subtractive random number generator algorithm proposed by Donald E. Knuth.
    //However, it is known that the implementation is compromised due to an error which Microsoft has concluded that cannot be fixed
    //without adversely affecting existing applications which are reliant on seeded repeatability.
    //Here (for laughs): https://connect.microsoft.com/VisualStudio/feedback/details/634761/system-random-serious-bug
    public class House : Seedable<int> {
        private const int DefaultSeed = 0x1234ABCD;
        private Random houseRandom;

        //The generator is initialized in its default, non-randomized starting state.
        public House() : base(Int32.MaxValue - 1) {
            SetSeed(null);
        }
        
        public House(bool randomize) : base(Int32.MaxValue - 1) {
            if(randomize) {
                RandomizeInternalState();
            }
            else {
                SetSeed(null);
            }
        }


        public override ulong Next() {
            return (ulong)houseRandom.Next();
        }

        public override int SeedLength { get { return 1; } }
        public override string AlgorithmName { get { return "House Random"; } }

        public override void SetSeed(int[] seed) {
            if(seed == null) {
                houseRandom = new Random(DefaultSeed);
                ResetCache();       //Important to reset base cache.
            }
            else if(seed.Length > 0) {
                houseRandom = new Random(seed[0]);
                ResetCache();
            }
        }

        public override uint GetUInt32(uint max, bool maxIncl) {
            //Override to ensure same results as Random, where possible.
            if(max > 0 && max < UInt32.MaxValue) {
                if(maxIncl) {
                    ++max;
                }

                return (uint)(houseRandom.Next(Int32.MinValue, (int)max + Int32.MinValue) - Int32.MinValue);
            }

            //This will handle case where max is UInt32.MaxValue inclusive, which Next() doesn't support, and throw our exceptions on range error.
            return base.GetUInt32(max, maxIncl);
        }

        public override double GetDouble() {
            return houseRandom.NextDouble();        //Override if you want to ensure the same results as Random.
        }

        //Override for performance. Disable to test base methods.
        public override RNG NewInstance() {
            return new House();
        }
    }
}
