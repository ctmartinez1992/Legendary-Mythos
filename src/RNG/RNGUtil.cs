using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LM.MyRNG {
    static class RNGUtil {
        private const string InvalidTypeMessage = "The array must be of integer type (i.e. byte, int, long etc.).";
        
        //An internal utility method which gives the size of an integer type (i.e. byte = 1, int = 4, etc.). If the type t is invalid,
        //ArgumentException will be thrown if throwException is true. Otherwise, the result will be 0.
        public static int GenericIntegralSize(Type t, bool throwException, bool allowUnsigned = true, bool allowSigned = true) {
            if((allowUnsigned && t.Equals(typeof(ulong))) || (allowSigned && t.Equals(typeof(long)))) {
                return sizeof(ulong);
            }
            else if((allowUnsigned && t.Equals(typeof(uint))) || (allowSigned && t.Equals(typeof(int)))) {
                return sizeof(uint);
            }
            else if((allowUnsigned && t.Equals(typeof(byte))) || (allowSigned && t.Equals(typeof(sbyte)))) {
                return sizeof(byte);
            }
            else if((allowUnsigned && t.Equals(typeof(ushort))) || (allowSigned && t.Equals(typeof(short)))) {
                return sizeof(ushort);
            }

            if(throwException) {
                throw new ArgumentException("T", InvalidTypeMessage);
            }

            return 0;
        }

        //Converts a byte array to integer array of type T. Any extra bytes are discarded. Result is endian dependent.
        public static T[] ConvertBytesToArray<T>(byte[] bytes) where T : struct {
            if(bytes != null) {
                int elemSize = GenericIntegralSize(typeof(T), true);
                int len = bytes.Length / elemSize;

                var rslt = new T[len];

                Buffer.BlockCopy(bytes, 0, rslt, 0, len * elemSize);

                return rslt;
            }

            return null;
        }
        
        public static bool IsArrayZero<T>(T[] items) where T : struct, IEquatable<T> {
            //Returns true if all values are 0.
            for(int n = 0; n < items.Length; ++n) {
                if(!items.Equals(0)) {
                    return false;
                }
            }

            return items.Length > 0;
        }

        //Calculate the number of bits needed. For example: for 0x70, it would be 7.
        public static int BitSize(ulong x) {
            //Shortcuts to common values.
            if(x == UInt64.MaxValue) {
                return 64;
            }
            if(x == UInt32.MaxValue) {
                return 32;
            }
            if(x == Int32.MaxValue) {
                return 31;
            }

            if(x != 0) {
                int bits = 64;

                while((x >> --bits) == 0);

                return bits + 1;
            }

            return 0;
        }
        
        public static ulong SwapEndian(ulong x) {
            return ((x & 0x00000000000000FFUL) << 56 | (x & 0x000000000000FF00UL) << 40 | (x & 0x0000000000FF0000UL) << 24 | (x & 0x00000000FF000000UL) << 8 |
                    (x & 0x000000FF00000000UL) >> 8 | (x & 0x0000FF0000000000UL) >> 24 | (x & 0x00FF000000000000UL) >> 40 | (x & 0xFF00000000000000UL) >> 56);
        }
    }
}
