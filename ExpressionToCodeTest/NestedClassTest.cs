using System;
using System.Collections.Generic;
using System.Linq;
using ExpressionToCodeLib;
using Xunit;

namespace ExpressionToCodeTest;

public class Parent
{
    public class Nested { }

    // ReSharper disable once UnusedTypeParameter
    public class NestedGen<T> { }
}

// ReSharper disable once UnusedTypeParameter
public class ParentGen<T>
{
    public class Nested { }

    // ReSharper disable once UnusedTypeParameter
    public class NestedGen<T2> { }
}

public class NestedClassTest
{
    [Fact]
    public void PlainNested()
        => Assert.Equal(
            "() => null as Parent.Nested",
            ExpressionToCode.ToCode(() => null as Parent.Nested));

    [Fact]
    public void GenericNested()
    {
        Assert.Equal("() => null as Parent.NestedGen<int>", ExpressionToCode.ToCode(() => null as Parent.NestedGen<int>));
        Assert.Equal("() => null as Parent.NestedGen<Parent.NestedGen<object>>", ExpressionToCode.ToCode(() => null as Parent.NestedGen<Parent.NestedGen<object>>));
    }

    [Fact]
    public void NestedInGeneric()
    {
        Assert.Equal("() => null as ParentGen<int>.Nested", ExpressionToCode.ToCode(() => null as ParentGen<int>.Nested));
        Assert.Equal("() => null as ParentGen<ParentGen<string>.Nested>.Nested", ExpressionToCode.ToCode(() => null as ParentGen<ParentGen<string>.Nested>.Nested));
    }

    [Fact]
    public void GenericNestedInGeneric()
    {
        Assert.Equal("() => null as ParentGen<int>.NestedGen<string>", ExpressionToCode.ToCode(() => null as ParentGen<int>.NestedGen<string>));
        Assert.Equal(
            "() => null as ParentGen<Parent.NestedGen<object>>.NestedGen<string>",
            ExpressionToCode.ToCode(() => null as ParentGen<Parent.NestedGen<object>>.NestedGen<string>));
        Assert.Equal(
            "() => null as ParentGen<int>.NestedGen<ParentGen<int>.Nested>",
            ExpressionToCode.ToCode(() => null as ParentGen<int>.NestedGen<ParentGen<int>.Nested>));
        Assert.Equal(
            "() => null as ParentGen<ParentGen<int>.Nested>.NestedGen<ParentGen<int>.NestedGen<string>>",
            ExpressionToCode.ToCode(() => null as ParentGen<ParentGen<int>.Nested>.NestedGen<ParentGen<int>.NestedGen<string>>));
    }
}