using System;
using TopLevelProgramExample;
using Xunit;

namespace ExpressionToCodeTest
{
    public class TopLevelProgramTest
    {
        [Fact]
        public void CanRunTopLevelProgram()
        {
            var topLevelProgram = (Action<string[]>)Delegate.CreateDelegate(typeof(Action<string[]>), typeof(TopLevelProgramMarker).Assembly.EntryPoint);
            topLevelProgram(new []{"test"});
        }
    }
}
