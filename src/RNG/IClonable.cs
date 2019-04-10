namespace LM.MyRNG {
    //An interface for cloning LM.RNG generators, where clones have a "deep" copy of the original's internal state.
    //Normally, only generators which derive from Seedable would inherit this interface.
    public interface IClonable {
        //Creates a clone of the pseudo random generator with a "deep" copy of its internal state.
        //A newly cloned generator will produce the same results as the original, provided they are called in the same way.
        //When implementing CloneInstance(), call RNG.CloneBaseRng() to create the initial instance with a "deep" copy of all field values in the base class.
        RNG CloneInstance();
    }
}