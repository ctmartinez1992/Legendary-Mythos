namespace LM.MyRNG {
    //An interface for determining whether two generators are not only of the same concrete type, but also have the same internal state.
    //Do not confuse with Object.Equals(), which provides reference equality.
    //Furthermore, overriding Object.Equals() so that it provide "value equality" was deemed unsuitable
    //because random number generators are reference types, rather than "value types", with highly mutable "hidden" states.
    //Therefore, the term "concordant" is used in this context to refer to "deep value equality",
    //which may not otherwise be apparent from the object's public properties.
    //Normally, only generators which derive from SeedableRNG would inherit this interface.
    public interface IConcordant {
        //Returns true if other is a generator of the same concrete type, and has the same internal state.
        //Two generators for which IsConcordant() is true will produce identical results, provided they are called in the same way.
        //An implementation of IsConcordant() should initially call Rng.IsBaseConcordant(), which will return false if 'other' is null, or
        //if other is an object of a different concrete class type.
        //Moreover, it will also compare internal field values in the base class, and return true if they have "deep value equality".
        //If IsBaseConcordant() returns false, then the IsConcordant() implementation should return false immediately.
        //If IsBaseConcordant() is true, then the internal state of the subclass should then be compared for "deep" value equality.
        bool IsConcordant(RNG other);
    }
}