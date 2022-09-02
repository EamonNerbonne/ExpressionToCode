namespace ExpressionToCodeTest;

// ReSharper disable UnusedTypeParameter
// ReSharper disable ClassNeverInstantiated.Global
public class Outer<X, Y>
{
    public class Nested<Z>
    {
        [UsedImplicitly]
        public void Method(Func<Z> arg) { }
    }

    public class Nested2 { }
}

public class Outer2
{
    public class Nested3<Z>
    {
        [UsedImplicitly]
        public void Method(Func<Z> arg) { }
    }
}
