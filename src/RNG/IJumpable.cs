namespace LM.MyRNG {
    //An interface for generators which implement a "jump ahead" or "fast jump" function.
    //A "jump ahead" function provides an efficient means for a rng to advance its internal state by a number of equivalent calls to Next() or Discard().
    //It is used to ensure that generators, initially seeded with the same value, do not produce the same or overlapping results.
    //This is useful in scenarios where parallelism and repeatability are both important.
    //The jump size is generator dependent, and is described by the JumpSig and JumpExp properties, such that it equals JumpSig^JumpExp.
    //The actual jump size value, however, may exceed the maximum range of integer types. For example, the jump size of Xorshift1024* algorithm is 2^512.
    //Normally, only generators which derive from Seedable would inherit this interface.
    public interface IJumpable {
        //The number of equivalent calls to Next() performed by Jump() is described by JumpSig^JumpExp.
        //The actual jump size value, however, may exceed the maximum range of integer types. For example, the jump size of Xorshift1024* algorithm is 2^512.
        ulong JumpSig { get; }

        //See JumpSig for information. 
        int JumpExp { get; }

        //Performs a "fast jump", equivalent to calling Next() JumpSig^JumpExp times.
        void Jump();
    }
}