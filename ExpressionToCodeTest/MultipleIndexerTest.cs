using System;
using System.Collections.Generic;
using System.Linq;
using ExpressionToCodeLib;
using Xunit;

namespace ExpressionToCodeTest
{
    class HasIndexers
    {
        public object this[string s] => null;

        public object this[int i] => null;
    }

    public class MultipleIndexerTest
    {
        [Fact]
        public void CanPrettyPrintVariousIndexers()
        {
            Assert.Equal(
                "() => new HasIndexers()[3] == new HasIndexers()[\"three\"]",
                ExpressionToCode.ToCode(() => new HasIndexers()[3] == new HasIndexers()["three"])
            );
        }
    }
}
