using System;
using TopLevelProgramExample;
using Xunit;
using static TopLevelProgramExample.TopLevelProgramMarker;

namespace ExpressionToCodeTest
{
    public class TopLevelProgramTest
    {
        [Fact]
        public void CanRunTopLevelProgram()
        {
            LambdaInsideLocalFunction = LambdaToMyVar = LambdaInsideNestedClassMethod = null;

            var topLevelProgram = (Action<string[]>)Delegate.CreateDelegate(typeof(Action<string[]>), typeof(TopLevelProgramMarker).Assembly.EntryPoint ?? throw new("Expected non-null return"));
            topLevelProgram(new[] { "test" });

            Assert.Equal("() => myVariable", LambdaToMyVar);
            Assert.Equal("() => InnerClass.StaticInt + InstanceInt", LambdaInsideNestedClassMethod);
            Assert.Equal("() => inLocalFunction + myVariable.Length - arg - withImplicitType.A.Length * 27", LambdaInsideLocalFunction);
        }
    }
}
