using System;
using System.Linq.Expressions;
using ExpressionToCodeLib;
using TopLevelProgramExample;

const int SomeConst = 27;
var myVariable = "implicitly closed over";
var withImplicitType = new {
    A = "ImplicitTypeMember",
};
Console.WriteLine(TopLevelProgramMarker.LambdaToMyVar = ExpressionToCode.ToCode(() => myVariable));

new InnerClass().DoIt();
LocalFunction(123);

void LocalFunction(int arg)
{
    var inLocalFunction = 42;
    Expression<Func<int>> expression1 = () => inLocalFunction + myVariable.Length - arg - withImplicitType.A.Length * SomeConst;

    Console.WriteLine(TopLevelProgramMarker.LambdaInsideLocalFunction = ExpressionToCode.ToCode(expression1));
}

sealed class InnerClass
{
    public static int StaticInt = 37;
    public int InstanceInt = 12;

    public void DoIt()
        => Console.WriteLine(TopLevelProgramMarker.LambdaInsideNestedClassMethod = ExpressionToCode.ToCode(() => StaticInt + InstanceInt));
}
