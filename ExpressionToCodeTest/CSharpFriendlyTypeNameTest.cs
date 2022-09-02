using System.Text.RegularExpressions;

namespace ExpressionToCodeTest;

public class CSharpFriendlyTypeNameTest
{
    [Fact]
    public void SupportsBuiltInCases()
    {
        Assert.Equal("object", typeof(object).ToCSharpFriendlyTypeName());
        Assert.Equal("string", typeof(string).ToCSharpFriendlyTypeName());
        Assert.Equal("char", typeof(char).ToCSharpFriendlyTypeName());
        Assert.Equal("byte", typeof(byte).ToCSharpFriendlyTypeName());
        Assert.Equal("sbyte", typeof(sbyte).ToCSharpFriendlyTypeName());
        Assert.Equal("short", typeof(short).ToCSharpFriendlyTypeName());
        Assert.Equal("ushort", typeof(ushort).ToCSharpFriendlyTypeName());
        Assert.Equal("int", typeof(int).ToCSharpFriendlyTypeName());
        Assert.Equal("uint", typeof(uint).ToCSharpFriendlyTypeName());
        Assert.Equal("long", typeof(long).ToCSharpFriendlyTypeName());
        Assert.Equal("ulong", typeof(ulong).ToCSharpFriendlyTypeName());
        Assert.Equal("void", typeof(void).ToCSharpFriendlyTypeName());
        Assert.Equal("float", typeof(float).ToCSharpFriendlyTypeName());
        Assert.Equal("decimal", typeof(decimal).ToCSharpFriendlyTypeName());
    }

    [Fact]
    public void SupportsSimpleExamples()
    {
        Assert.Equal("DateTime", typeof(DateTime).ToCSharpFriendlyTypeName());
        Assert.Equal("Regex", typeof(Regex).ToCSharpFriendlyTypeName());
        Assert.Equal("ExpressionToCode", typeof(ExpressionToCode).ToCSharpFriendlyTypeName());
    }

    [Fact]
    public void IntArray()
        => Assert.Equal("int[]", typeof(int[]).ToCSharpFriendlyTypeName());

    [Fact]
    public void NullableValueType()
        => Assert.Equal("ConsoleKey?", typeof(ConsoleKey?).ToCSharpFriendlyTypeName());

    [Fact]
    public void GenericList()
        => Assert.Equal("List<DateTime>", typeof(List<DateTime>).ToCSharpFriendlyTypeName());

    [Fact]
    public void MultiDimArray()
        => Assert.Equal("string[,,]", typeof(string[,,]).ToCSharpFriendlyTypeName());

    [Fact] //Has always been broken
    public void MultiDimOfSingleDimArray()
        => Assert.Equal("object[,][]", typeof(object[,][]).ToCSharpFriendlyTypeName());

    [Fact] //Has always been broken
    public void SingleDimOfMultiDimArray()
        => Assert.Equal("object[][,]", typeof(object[][,]).ToCSharpFriendlyTypeName());

    [Fact] //Has always been broken
    public void ConstructedSingleDimOfMultiDimArray()
    {
        // ReSharper disable once SuggestUseVarKeywordEvident
        // ReSharper disable once RedundantArrayCreationExpression
        var v = new[] { new object[2, 3] };

        Assert.Equal("object[][,]", v.GetType().ToCSharpFriendlyTypeName());
    }

    [Fact]
    public void ArrayGenericsMessyMix()
        => Assert.Equal("List<Tuple<int[], string[,]>[][]>[]", typeof(List<Tuple<int[], string[,]>[][]>[]).ToCSharpFriendlyTypeName());

    [Fact]
    public void NestedClasses()
        => Assert.Equal("Outer<string, int>.Nested<DateTime>", typeof(Outer<string, int>.Nested<DateTime>).ToCSharpFriendlyTypeName());

    [Fact]
    public void NestedNonGenericInGenericClasses()
        => Assert.Equal("Outer<string, int>.Nested2", typeof(Outer<string, int>.Nested2).ToCSharpFriendlyTypeName());

    [Fact]
    public void NestedGenericInNonGenericClasses()
        => Assert.Equal("Outer2.Nested3<Action>", typeof(Outer2.Nested3<Action>).ToCSharpFriendlyTypeName());

    [Fact]
    public void RussianDolls()
        => Assert.Equal("Tuple<List<int>, Tuple<List<string>>>", typeof(Tuple<List<int>, Tuple<List<string>>>).ToCSharpFriendlyTypeName());

    [Fact]
    public void GenericArgumentTypes()
        => Assert.Equal("Func<Z>", typeof(Outer<,>.Nested<>).GetTypeInfo().GetMethod("Method")!.GetParameters()![0].ParameterType.ToCSharpFriendlyTypeName());

    [Fact]
    public void UnboundNested()
        => Assert.Equal("Outer<X, Y>.Nested<Z>", typeof(Outer<,>.Nested<>).ToCSharpFriendlyTypeName());

    [Fact]
    public void UnboundGenericList()
        => Assert.Equal("List<T>", typeof(List<>).ToCSharpFriendlyTypeName());

    [Fact]
    public void UnboundGenericListInTypeof()
        => Assert.Equal("() => typeof(List<>)", ExpressionToCode.ToCode(() => typeof(List<>)));

    [Fact]
    public void UnboundGenericNullableInTypeof()
        => Assert.Equal("() => typeof(Nullable<>)", ExpressionToCode.ToCode(() => typeof(Nullable<>)));

    [Fact]
    public void UnboundNestedInTypeof()
        => Assert.Equal("() => typeof(Outer<,>.Nested<>)", ExpressionToCode.ToCode(() => typeof(Outer<,>.Nested<>)));
}
