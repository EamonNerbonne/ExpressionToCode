using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ExpressionToCodeLib;
using Xunit;

namespace ExpressionToCodeTest
{
    class HasIndexers
    {
        public object this[string s] { get { return null; } }
        public object this[int i] { get { return null; } }
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
