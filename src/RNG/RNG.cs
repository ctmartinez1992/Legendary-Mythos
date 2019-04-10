using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using LM.RNG;

namespace LM.MyRNG {
    //Abstract base class for all random number generators in the RNG namespace.
    //The Rng base class provides a common implementation for a range of routines relating to random number generation.
    //
    //All random results ultimately derive from a single abstract method, Next().
    //Next() represents the native integer generation routine of the underlying random number generator algorithm.
    //Next() returns an unsigned value between 0 and Rng.RandMax (inclusive). Both Next() and RandMax are defined by the algorithm implementation.
    //For pseudo-random generators, its output can be expected to match known test vectors from a defined starting state.
    //Results from all other methods should be considered implementation dependent, unless otherwise stated.
    //
    //The Rng base class does not support seeding.
    //Therefore, it can be directly inherited by concrete implementations of hardware based or unseeded cryptographic generators.
    //Seedable generator implementations should inherit from SeedableRng instead.
    //
    //Concrete implementations are required to override Next() and AlgorithmName property, and provide the value of RandMax to the Rng constructor.
    //Other methods in the base class have implementations, but a number of them can be overridden if required.
    public abstract class RNG {
        private const ulong I3E64Const = 0x3FF0000000000000UL;
        private const string CommonRangeError = "Invalid range error.";

        //All fields must be copy/compared in both CloneBaseRng() and IsBaseConcordant() methods.
        //If reference fields are ever introduced, all CloneInstance() implementations in the RNG classes need to be checked.
        //As I cheated and used MemberwiseClone() on some them for performance reasons. These would need replacing with CloneBaseRng().
        //Note: Don't introduce reference fields here, if it can be avoided.
        private ulong nativeMax;
        private bool native64;
        private bool native32;
        private int shiftBits;
        private ulong shiftMax;

        //All cache fields must additionally be reset in ResetCache().
        private ulong flipCache;
        private int flipCacheSize;
        private uint next32Cache;
        private bool next32Flag;
        private double gaussCache;
        private bool gaussFlag;

        //Subclasses must supply randMax, the maximum possible value returned by the implementation of the Next() method.
        public RNG(ulong randMax) {
            if(randMax == 0) {
                throw new ArgumentException("randMax", "RandMax cannot be zero!");
            }
                
            nativeMax = randMax;
            native64 = (randMax == UInt64.MaxValue);
            native32 = (randMax == UInt32.MaxValue);

            //Get number of bits to use from Next(). A RandMax of 2^31-1 gives us 31.
            shiftBits = RNGUtil.BitSize(randMax);
            shiftMax = UInt64.MaxValue >> (64 - shiftBits);

            if(randMax != shiftMax) {
                --shiftBits;
                shiftMax >>= 1;
            }

            ResetCache();           // Initialize cache values

            // Useful for confirming properties when debugging
            /*Console.WriteLine();
            Console.WriteLine("NAME       : " + AlgorithmName);
            Console.WriteLine("RandMax    : " + randMax);
            Console.WriteLine("RandMax    : 0x" + randMax.ToString("X16"));
            Console.WriteLine("Native32   : " + native32);
            Console.WriteLine("Native64   : " + native64);
            Console.WriteLine("BitShift  : " + shiftBits);
            Console.WriteLine();*/
        }

        //The common name of the generator algorithm, i.e. "MT19937-32". This property is abstract and should be implemented by the generator subclass.
        public abstract string AlgorithmName { get; }

        //The maximum possible value of the algorithm's native generation routine Next(), inclusive.
        public ulong RandMax {
            get { return nativeMax; }
        }

        //The value is true if the generator subclass inherits from the SeedableRng class.
        //This property is provided for convenience, as the C# "is" keyword cannot be used on generic SeedableRng type unless the seed integer type is known.
        public virtual bool IsSeedable {
            get { return false; }
        }

        //Next() represents the native integer generation routine of the underlying random number generator. All other values are derived from this.
        //The result is an unsigned integer in the range 0 to RandMax, inclusive, where the value of RandMax is algorithm dependent.
        //For pseudo random generators, its output can be expected to match known test vectors from a defined starting state.
        //Results from all other methods should be considered implementation dependent, unless otherwise stated.
        //This method is abstract in the Rng base class.
        public abstract ulong Next();

        //Generates a random 32-bit unsigned integer uniformly distributed in the range 0 to 2^32-1, inclusive, irrespective of the output range of Next().
        //Although the result is derived from Next(), do not expect values to necessarily match those of Next().
        //You should always consider results of this method to be implementation dependent.
        //If you need future-proof repeatability call Next() instead or, alternatively, override this method in a subclass in order to control result values.
        //If you override, ensure all 32 bits are randomly populated without bias and that no Rng method other than Next() is called.
        public virtual uint Next32() {
            //Split a 64-bit value into 2, and toggle between a cached value.
            if(!(next32Flag = !next32Flag)) {
                return next32Cache;
            }

            ulong temp;

            if(native64) {
                temp = Next();
            }
            else if(native32) {
                temp = (Next() << 32) | Next();
            }
            else {
                //Build unbiased 64-bit for non 32/64 bit types. Relatively slow.
                temp = GetUnbiased64();
            }

            next32Cache = (uint)temp;
            return (uint)(temp >> 32);
        }

        //Generates a random 64-bit unsigned integer uniformly distributed in the range 0 to 2^64-1, inclusive, irrespective of the output range of Next().
        //Although the result is derived from Next(), do not expect values to necessarily match those of Next().
        //You should always consider results of this method to be implementation dependent.
        //If you need future-proof repeatability call Next() instead or, alternatively, override this method in a subclass in order to control result values.
        //If you override, ensure all 64 bits are randomly populated without bias and that no Rng method other than Next() is called.
        public virtual ulong Next64() {
            if(native64) {
                return Next();
            }
            if(native32) {
                return (Next() << 32) | Next();
            }

            //Build unbiased 64-bit for non 32/64 bit types. Relatively slow.
            return GetUnbiased64();
        }

        //Generates a random 32-bit signed integer uniformly distributed in the range 0 (inclusive) to 'max' (exclusive).
        //The max value must be greater than 0.
        public int GetInt32(int max) {
            if(max > 0) {
                return (int)GetUInt32((uint)max, false);
            }
            else {
                throw new ArgumentException("max", CommonRangeError);
            }
        }

        //Generates a random 32-bit signed integer uniformly distributed in the range 0 (inclusive) to 'max' (inclusive if 'maxIsInclusive' is true).
        //The max value must be greater than 0. It may also be 0 if maxIsInclusive is true, but the result will always be 0 in this case.
        public int GetInt32(int max, bool maxIsInclusive) {
            if(max >= 0) {
                return (int)GetUInt32((uint)max, maxIsInclusive);
            }
            else {
                throw new ArgumentException("max", CommonRangeError);
            }
        }

        //Generates a random 32-bit signed integer uniformly distributed in the range 'min' (inclusive) to 'max' (exclusive).
        //One or both values may be negative provided min is less than max.
        public int GetInt32(int min, int max) {
            if(max > min) {
                return min + (int)GetUInt32((uint)(max - min), false);
            }
            else {
                throw new ArgumentException(CommonRangeError);
            }
        }

        //Generates a random 32-bit signed integer uniformly distributed in the range 'min' (inclusive) to 'max' (inclusive if 'maxIsInclusive' is true).
        //One or both values may be negative provided min is less than max.
        //If maxIsInclusive is true, min and max may also be equal, but the result will always that of min.
        public int GetInt32(int min, int max, bool maxIsInclusive) {
            if(max >= min) {
                return min + (int)GetUInt32((uint)(max - min), maxIsInclusive);
            }
            else {
                throw new ArgumentException(CommonRangeError);
            }
        }

        //Generates a random 32-bit unsigned integer uniformly distributed in the range 0 (inclusive) to 'max' (exclusive).
        //The max value must be greater than 0.
        public uint GetUInt32(uint max) {
            return GetUInt32(max, false);
        }

        //Generates a random 32-bit unsigned integer uniformly distributed in the range 0 (inclusive) to 'max' (inclusive if 'maxIsInclusive' is true).
        //The max value must be greater than 0. It may also be 0 if maxIsInclusive is true, but the result will always be 0 in this case.
        //This method is virtual and may be overridden for specific requirements (normally there should be no need to do so).
        //All variants of GetInt32() and GetUInt32() will call this, and range shift appropriately.
        public virtual uint GetUInt32(uint max, bool maxIsInclusive) {
            //We advance the generator if max is 0 and maxIsInclusive is true. It also spares an additional check in order to return a constant value.
            if(max != 0 || maxIsInclusive) {
                if(max < UInt32.MaxValue || !maxIsInclusive) {
                    if(maxIsInclusive) {
                        ++max;
                    }

                    return (uint)(((ulong)max * Next32()) >> 32);
                }
                
                return Next32();
            }

            throw new ArgumentException(CommonRangeError);
        }

        //Generates a random 32-bit unsigned integer uniformly distributed in the range 'min' (inclusive) to 'max' (exclusive).
        //The max value must be greater than min.
        public uint GetUInt32(uint min, uint max) {
            if(max > min) {
                return min + GetUInt32(max - min, false);
            }
            else {
                throw new ArgumentException(CommonRangeError);
            }
        }

        //Generates a random 32-bit unsigned integer uniformly distributed in the range 'min' (inclusive) to 'max' (inclusive if 'maxIsInclusive' is true).
        //If maxIsInclusive is true, min and max may also be equal, but the result will always that of min. Otherwise, max must be greater than min.
        public uint GetUInt32(uint min, uint max, bool maxIsInclusive) {
            if(max >= min) {
                return min + GetUInt32(max - min, maxIsInclusive);
            }
            else {
                throw new ArgumentException(CommonRangeError);
            }
        }

        //Generates a random 64-bit signed integer uniformly distributed in the range 0 (inclusive) to 'max' (exclusive).
        //The max value must be greater than 0.
        public long GetInt64(long max) {
            if(max > 0) {
                return (long)GetUInt64((ulong)max, false);
            }
            else {
                throw new ArgumentException("max", CommonRangeError);
            }
        }

        //Generates a random 64-bit signed integer uniformly distributed in the range 0 (inclusive) to 'max' (inclusive if 'maxIsInclusive' is true).
        //The max value must be greater than 0. It may also be 0 if maxIsInclusive is true, but the result will always be 0 in this case.
        public long GetInt64(long max, bool maxIsInclusive) {
            if(max >= 0) {
                return (long)GetUInt64((ulong)max, maxIsInclusive);
            }
            else {
                throw new ArgumentException("max", CommonRangeError);
            }
        }

        //Generates a random 64-bit signed integer uniformly distributed in the range 'min' (inclusive) to 'max' (exclusive).
        //One or both values may be negative provided min is less than max.
        public long GetInt64(long min, long max) {
            if(max > min) {
                return min + (long)GetUInt64((ulong)(max - min), false);
            }
            else {
                throw new ArgumentException(CommonRangeError);
            }
        }

        //Generates a random 64-bit signed integer uniformly distributed in the range 'min' (inclusive) to 'max' (inclusive if 'maxIsInclusive' is true).
        //One or both values may be negative provided min is less than max.
        //If maxIsInclusive is true, min and max may also be equal, but the result will always that of min.
        public long GetInt64(long min, long max, bool maxIsInclusive) {
            if(max >= min) {
                return min + (long)GetUInt64((ulong)(max - min), maxIsInclusive);
            }
            else {
                throw new ArgumentException(CommonRangeError);
            }
        }

        //Generates a random 64-bit unsigned integer uniformly distributed in the range 0 (inclusive) to 'max' (exclusive).
        //The max value must be greater than 0.
        public ulong GetUInt64(ulong max) {
            return GetUInt64(max, false);
        }

        //Generates a random 64-bit unsigned integer uniformly distributed in the range 0 (inclusive) to 'max' (inclusive if 'maxIsInclusive' is true).
        //The max value must be greater than 0. It may also be 0 if maxIsInclusive is true, but the result will always be 0 in this case.
        //This method is virtual and may be overridden for specific requirements (normally there should be no need to do so).
        //All variants of GetInt64() and GetUInt64() will call this, and range shift appropriately.
        public virtual ulong GetUInt64(ulong max, bool maxIsInclusive) {
            if(max <= UInt32.MaxValue) {
                return GetUInt32((uint)max, maxIsInclusive);
            }

            //We advance the generator if max is 0 and maxIsInclusive is true. It also spares an additional check in order to return a constant value.
            if(max != 0 || maxIsInclusive) {
                if(max != UInt64.MaxValue || !maxIsInclusive) {
                    if(maxIsInclusive) {
                        ++max;
                    }

                    ulong rxh = max >> 32;
                    ulong rxl = max & UInt32.MaxValue;

                    ulong rav = Next64();
                    ulong ravh = rav >> 32;
                    ulong ravl = (uint)rav;

                    return ((rxl * ravh) >> 32) + ((rxh * ravl) >> 32) + (rxh * ravh);
                }
                
                return Next64();
            }

            throw new ArgumentException(CommonRangeError);
        }

        //Generates a random 64-bit unsigned integer uniformly distributed in the range 'min' (inclusive) to 'max' (exclusive).
        //The max value must be greater than min.
        public ulong GetUInt64(ulong min, ulong max) {
            if(max > min) {
                return min + GetUInt64(max - min, false);
            }
            else {
                throw new ArgumentException(CommonRangeError);
            }
        }

        //Generates a random 64-bit unsigned integer uniformly distributed in the range 'min' (inclusive) to 'max' (inclusive if 'maxIsInclusive' is true).
        //If maxIsInclusive is true, min and max may also be equal, but the result will always that of min. Otherwise, max must be greater than min.
        public ulong GetUInt64(ulong min, ulong max, bool maxIsInclusive) {
            if(max >= min) {
                return min + GetUInt64(max - min, maxIsInclusive);
            }
            else {
                throw new ArgumentException(CommonRangeError);
            }
        }

        //Generates a random double value uniformly distributed in the range 0 (inclusive) to 1.0 (exclusive).
        //This method is virtual and may be overridden for specific requirements (normally there should be no need to do so).
        //All other routines which return a double type will normally call this method, and range shift appropriately.
        public virtual double GetDouble() {
            //Double type must be 64-bit IEEE format. Microsoft define double to be this as part of standard.
            //See: http://xorshift.di.unimi.it
            return BitConverter.Int64BitsToDouble((long)(I3E64Const | Next64() >> 12)) - 1;
        }

        //Generates a random double value uniformly distributed in the range 'min' (inclusive) to 'max' (exclusive).
        //The max value must be greater than min. The maximum permissible range between min and max is given by double.MaxValue.
        //The result is checked to ensure that no range combination gives a value equal to max.
        public double GetDouble(double min, double max) {
            if(max > min) {
                double rslt = min + GetDouble() * (max - min);
                while(rslt >= max) {
                    rslt = min + GetDouble() * (max - min);
                }

                return rslt;
            }

            throw new ArgumentException("max", CommonRangeError);
        }

        //Generates a random double value uniformly distributed in the open interval 0 (exclusive) to 1.0 (exclusive).
        //The result is checked to ensure that it never equals 0.
        public double GetOpenDouble() {
            double rslt = GetDouble();
            while(rslt == 0) {
                rslt = GetDouble();
            }

            return rslt;
        }

        //Generates a random double value uniformly distributed in the open interval 'min' (exclusive) to 'max' (exclusive).
        //The max value must be greater than min. The maximum permissible range between min and max is given by double.MaxValue.
        //The result is checked to ensure that no range combination gives a value equal to min or max.
        public double GetOpenDouble(double min, double max) {
            if(max > min) {
                double rslt = min + GetDouble() * (max - min);
                while(rslt >= max || rslt <= min) {
                    rslt = min + GetDouble() * (max - min);
                }

                return rslt;
            }

            throw new ArgumentException("max", CommonRangeError);
        }

        //Returns a "normally" distributed random double value, with a mean of 0 and standard deviation of +1.0.
        //The Box-Muller transformation is to generate the result. This method is virtual and may be overridden for specific requirements.
        public virtual double GetStdNormal() {
            if(!(gaussFlag = !gaussFlag)) {
                //Two values are generated, with one being cached for the next call.
                return gaussCache;
            }

            //Polar form of Box-Muller. Ref: http://www.design.caltech.edu/erik/Misc/Gaussian.html
            double r0;
            double r1;
            double w;

            do {
                r0 = 2.0 * GetDouble() - 1;
                r1 = 2.0 * GetDouble() - 1;
                w = r0 * r0 + r1 * r1;
            } while(w >= 1 || w <= double.Epsilon);

            w = Math.Sqrt((-2.0 * Math.Log(w)) / w);
            gaussCache = r0 * w;

            return r1 * w;
        }

        //Creates a new byte array and populates it with 'count' random values.
        //GetBytes() will generally be much faster than populating a byte array with individual calls to GetInt32() or Next().
        //However, results are implementation and endian dependent; do not expect them to align with or match the output of Next().
        public byte[] GetBytes(int count) {
            var rslt = new byte[count];
            GetBytes(rslt, 0, count);
            return rslt;
        }

        //Populates the array with 'count' random byte values.
        //GetBytes() will generally be much faster than populating a byte array with individual calls to GetInt32() or Next().
        //However, results are implementation and endian dependent; do not expect them to align with or match the output of Next().
        public int GetBytes(byte[] dst) {
            return GetBytes(dst, 0, dst.Length);
        }

        //Populates the array with 'count' random byte values, starting from the given 'offset'.
        //If count is -1, values are written from offset to the end of the array.
        //GetBytes() will generally be much faster than populating a byte array with individual calls to GetInt32() or Next().
        //However, results are implementation and endian dependent; do not expect them to align with or match the output of Next().
        //This method is virtual and may be overridden for specific requirements (normally there should be no need to do so).
        //Overloaded variants will call it.
        public virtual int GetBytes(byte[] dst, int offset, int count) {
            //Buffer size in ulongs (i.e. x8 bytes). Changing this size doesn't seem to make a lot difference to performance, unless very small.
            const int MaxBufferSize = 500;
            
            count = ArrayBoundsCheck(dst, offset, count);       //This will throw on bounds error.

            int i8Count = count;
            int i64Count = i8Count / sizeof(ulong);
            if(i8Count % sizeof(ulong) != 0) {
                ++i64Count;
            }

            int bufSize = i64Count;
            if(bufSize > MaxBufferSize) {
                bufSize = MaxBufferSize;
            }

            ulong[] buf = new ulong[bufSize];

            int bufIdx;
            int byteSize = bufSize * sizeof(long);
            if(byteSize > i8Count) {
                byteSize = i8Count;
            }

            do {
                bufIdx = 0;
                do {
                    buf[bufIdx] = Next64();
                }
                while(++bufIdx < bufSize);

                Buffer.BlockCopy(buf, 0, dst, offset, byteSize);

                if((i8Count -= byteSize) == 0) {
                    break;
                }

                offset += byteSize;

                if((i64Count -= bufSize) < bufSize) {
                    //Short last block.
                    bufSize = i64Count;
                    byteSize = i8Count;
                }
            } while(true);

            return count;
        }

        //Returns true or false, with equal probability.
        public virtual bool Flipper() {
            if(--flipCacheSize < 0) {
                flipCache = Next64();
                flipCacheSize = 63;
            }

            bool rslt = ((flipCache & 0x01UL) == 1UL);
            flipCache >>= 1;

            return rslt;
        }

        //Shuffles the items in the array according to the Fisher-Yates algorithm.
        //T - Array item type.
        //items - The array to shuffle.
        public void Shuffle<T>(T[] items) {
            Shuffle<T>(items, 0, items.Length);
        }

        //Shuffles 'count' items in the array, starting from the given offset, according to the Fisher-Yates algorithm.
        //If count is -1, items are sorted from offset to the end of the array.
        //This method is virtual and may be overridden for specific requirements (normally there should be no need to do so).
        //Overloaded variants will call it.
        //T - Array item type.
        //items - The array to shuffle.
        //offset - Start offset.
        //count - Number of items to sort.
        public virtual void Shuffle<T>(T[] items, int offset, int count) {
            //Using unsigned avoids additional cast in the loop.
            uint ucnt = (uint)ArrayBoundsCheck(items, offset, count);

            T temp;
            int j;
            int end = (int)ucnt + offset;
            
            ++ucnt;         //Increment deliberate.

            //Above +1 deliberate.
            while(--ucnt > 1) {
                //The maxIsInclusive = false tallies with initial count increment. It avoids a further in-loop increment in GetUInt32().
                //We use GetUInt32() because this is where the implementation lives, and avoids unnecessary re-direction.
                j = offset + (int)GetUInt32(ucnt, false);

                temp = items[j];
                items[j] = items[--end];
                items[end] = temp;
            }
        }

        //Creates a new instance of the generator class, which will be in its initial state (not randomized).
        //This method is implemented in the base class but may be trivially overridden in the "final" subclass.
        //Because creating an object instance with "new" is faster than using a reflection technique,
        //overriding will generally improve performance of this and its overloaded variants in SeedableRng which call it.
        //Moreover, this method requires the generator subclass to have a default constructor. If this is not the case, NewInstance() must be overridden.
        public virtual RNG NewInstance() {
            var con = GetType().GetConstructor(Type.EmptyTypes);
            if(con != null) {
                return (RNG)con.Invoke(Type.EmptyTypes);
            }

            throw new NotSupportedException("This RNG class type has no default constructor.");
        }

        //Resets internal cache values used by the Rng base class to their initial zero state. 
        //The Rng base class will cache certain values to improve performance.
        //For example, GetStdNormal() is a relatively expensive routine, but it actually generates two results at once.
        //Therefore, one value is cached so that results need only be derived once every two calls.
        //Other values may also be cached in a similar way.
        //This method should be called when implementing SeedableRng.SetSeed() in a derived subclass
        //to ensure that results from all RNG routines are repeatable after re-seeding.
        public virtual void ResetCache() {
            flipCache = 0;
            flipCacheSize = 0;
            next32Cache = 0;
            next32Flag = false;
            gaussCache = 0;
            gaussFlag = false;
        }

        //A protected method that should be called to create the initial clone instance when implementing IClonableRng.CloneInstance().
        //CloneBaseRng() will call NewInstance() to create an instance of the same type as the subclass.
        //It will also "deep" copy all field values in the Rng base class to the clone.
        //It will not copy data from the subclass, however. This must be done by the CloneInstance() implementation.
        protected RNG CloneBaseRng() {
            RNG clone = NewInstance();

            //Usually defined by the class type, but may be different if the subclass provides a constructor with a RandMax parameter. We need to copy them.
            nativeMax = clone.nativeMax;
            native64 = clone.native64;
            native32 = clone.native32;
            shiftBits = clone.shiftBits;

            flipCache = clone.flipCache;
            flipCacheSize = clone.flipCacheSize;
            next32Cache = clone.next32Cache;
            next32Flag = clone.next32Flag;
            gaussCache = clone.gaussCache;
            gaussFlag = clone.gaussFlag;

            return clone;
        }

        //A protected method that should be called when implementing IConcordantRng.IsConcordant() to compare the internal data of the base class.
        //IsBaseConcordant() will return false if 'other' is null, or if 'other' is an object of a different concrete class type.
        //Moreover, it will also compare internal field values in the base class, and return true if they have "deep value equality".
        //It does not compare data in the subclass. This must be done by the IsConcordant() implementation.
        //If IsBaseConcordant() returns false, then the IsConcordant() implementation should return false immediately.
        //If IsBaseConcordant() is true, then the internal state of the subclass should then be compared for "deep" value equality.
        protected bool IsBaseConcordant(RNG other) {
            if(other == null || GetType() != other.GetType()) {
                return false;
            }

            return(
                flipCache == other.flipCache && flipCacheSize == other.flipCacheSize && next32Cache == other.next32Cache &&
                next32Flag == other.next32Flag && gaussCache == other.gaussCache && gaussFlag == other.gaussFlag && nativeMax == other.nativeMax &&
                native64 == other.native64 && native32 == other.native32 && shiftBits == other.shiftBits
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ulong GetUnbiased64() {
            //Build a 64-bit unbiased integer.
            int bits = 0;
            ulong x;
            ulong rslt = 0;

            do {
                do {
                    x = Next();
                }
                while(x > shiftMax);

                rslt <<= shiftBits;
                rslt |= x;
            }
            while((bits += shiftBits) < 64);

            return rslt;
        }

        private static int ArrayBoundsCheck<T>(T[] arr, int offset, int count) {
            //Common array bounds check. We allow negative count, and return actual count instead.
            int length = arr.Length;

            if(offset >= length || offset < 0) {
                throw new ArgumentOutOfRangeException("offset", "Array offset out of range.");
            }
            if(count < 0) {
                return length - offset;
            }
            if((long)offset + count > length) {
                throw new ArgumentException("count", "Array count exceeds length of the array.");
            }

            return count;
        }
    }
}