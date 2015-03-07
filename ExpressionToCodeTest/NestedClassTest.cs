using System;
using System.Collections.Generic;
using System.Linq;
using ExpressionToCodeLib;
using NUnit.Framework;

namespace ExpressionToCodeTest {
    public class Parent {
        public class Nested { }

        public class NestedGen<T> { }
    }

    public class ParentGen<T> {
        public class Nested { }

        public class NestedGen<T2> { }
    }

    class NestedClassTest {
        [Test]
        public void PlainNested() {
            Assert.That(
                ExpressionToCode.ToCode(() => null as Parent.Nested),
                Is.EqualTo("() => null as Parent.Nested"));
        }

        [Test]
        public void GenericNested() {
            Assert.That(
                ExpressionToCode.ToCode(() => null as Parent.NestedGen<int>),
                Is.EqualTo("() => null as Parent.NestedGen<int>"));
            Assert.That(
                ExpressionToCode.ToCode(() => null as Parent.NestedGen<Parent.NestedGen<object>>),
                Is.EqualTo("() => null as Parent.NestedGen<Parent.NestedGen<object>>"));
        }

        [Test]
        public void NestedInGeneric() {
            Assert.That(
                ExpressionToCode.ToCode(() => null as ParentGen<int>.Nested),
                Is.EqualTo("() => null as ParentGen<int>.Nested"));
            Assert.That(
                ExpressionToCode.ToCode(() => null as ParentGen<ParentGen<string>.Nested>.Nested),
                Is.EqualTo("() => null as ParentGen<ParentGen<string>.Nested>.Nested"));
        }

        [Test]
        public void GenericNestedInGeneric() {
            Assert.That(
                ExpressionToCode.ToCode(() => null as ParentGen<int>.NestedGen<string>),
                Is.EqualTo("() => null as ParentGen<int>.NestedGen<string>"));
            Assert.That(
                ExpressionToCode.ToCode(() => null as ParentGen<Parent.NestedGen<object>>.NestedGen<string>),
                Is.EqualTo("() => null as ParentGen<Parent.NestedGen<object>>.NestedGen<string>"));
            Assert.That(
                ExpressionToCode.ToCode(() => null as ParentGen<int>.NestedGen<ParentGen<int>.Nested>),
                Is.EqualTo("() => null as ParentGen<int>.NestedGen<ParentGen<int>.Nested>"));
            Assert.That(
                ExpressionToCode.ToCode(() => null as ParentGen<ParentGen<int>.Nested>.NestedGen<ParentGen<int>.NestedGen<string>>),
                Is.EqualTo("() => null as ParentGen<ParentGen<int>.Nested>.NestedGen<ParentGen<int>.NestedGen<string>>"));
        }
    }
}
