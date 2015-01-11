using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ExpressionToCodeLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace ExpressionToCodeTest {
    class HasIndexers {
        public object this[string s] { get { return null; } }
        public object this[int i] { get { return null; } }
    }

    public class MultipleIndexerTest {
        [Test]
        public void CanPrettyPrintVariousIndexers() {
            Assert.AreEqual(
                "() => new HasIndexers()[3] == new HasIndexers()[\"three\"]",
                ExpressionToCode.ToCode(() => new HasIndexers()[3] == new HasIndexers()["three"])
                );
        }
    }
}
