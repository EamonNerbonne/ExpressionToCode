using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExpressionToCodeLib;
using NUnit.Framework;

namespace ExpressionToCodeTest {
    public enum SomeEnum {
        A,
        B
    }

    [Flags]
    public enum SomeFlagsEnum {
        None = 0,
        A = 1,
        B = 2,
        AB = 3,
        C = 4,
    }

    class EnumTests {
        [Test]
        public void EnumConstant() {
            Assert.AreEqual(
                @"() => new object().Equals((object)MidpointRounding.ToEven)",
                ExpressionToCode.ToCode(() => new object().Equals(MidpointRounding.ToEven)));
        }

        [Test]
        public void EnumVariables() {
            var a = SomeEnum.A;
            var b = SomeEnum.B;
            Assert.AreEqual(
                @"() => a == b",
                ExpressionToCode.ToCode(() => a == b));
        }

        [Test]
        public void NullableEnumVariables() {
            var a = SomeEnum.A;
            SomeEnum? b = SomeEnum.B;
            Assert.AreEqual(
                @"() => a == b",
                ExpressionToCode.ToCode(() => a == b));
        }

        [Test]
        public void EnumVarEqConstant() {
            var a = SomeEnum.A;
            Assert.AreEqual(
                @"() => a == SomeEnum.B",
                ExpressionToCode.ToCode(() => a == SomeEnum.B));
            Assert.AreEqual(
                @"() => SomeEnum.B == a",
                ExpressionToCode.ToCode(() => SomeEnum.B == a));
        }

        [Test]
        public void NullableEnumVarEqConstant() {
            SomeEnum? a = SomeEnum.A;
            Assert.AreEqual(
                @"() => a == SomeEnum.B",
                ExpressionToCode.ToCode(() => a == SomeEnum.B));
            Assert.AreEqual(
                @"() => SomeEnum.B == a",
                ExpressionToCode.ToCode(() => SomeEnum.B == a));
        }

        [Test]
        public void EnumVarNeqConstant() {
            var a = SomeEnum.A;
            Assert.AreEqual(
                @"() => a != SomeEnum.B",
                ExpressionToCode.ToCode(() => a != SomeEnum.B));
            Assert.AreEqual(
                @"() => SomeEnum.B != a",
                ExpressionToCode.ToCode(() => SomeEnum.B != a));
        }

        [Test]
        public void NullableEnumVarNeqConstant() {
            SomeEnum? a = SomeEnum.A;
            Assert.AreEqual(
                @"() => a != SomeEnum.B",
                ExpressionToCode.ToCode(() => a != SomeEnum.B));
            Assert.AreEqual(
                @"() => SomeEnum.B != a",
                ExpressionToCode.ToCode(() => SomeEnum.B != a));
        }

        [Test]
        public void EnumVarLtConstant() {
            var a = SomeEnum.A;
            Assert.AreEqual(
                @"() => a < SomeEnum.B",
                ExpressionToCode.ToCode(() => a < SomeEnum.B));
            Assert.AreEqual(
                @"() => SomeEnum.B > a",
                ExpressionToCode.ToCode(() => SomeEnum.B > a));
        }

        [Test]
        public void NullableEnumVarLtConstant() {
            SomeEnum? a = SomeEnum.A;
            Assert.AreEqual(
                @"() => a < SomeEnum.B",
                ExpressionToCode.ToCode(() => a < SomeEnum.B));
            Assert.AreEqual(
                @"() => SomeEnum.B > a",
                ExpressionToCode.ToCode(() => SomeEnum.B > a));
        }

        [Test]
        public void EnumCornerCases() {
            var a = SomeEnum.A;
            var b = SomeFlagsEnum.B;
            Assert.AreEqual(
                @"() => a == SomeEnum.B",
                //C# compiler does not preserve this type information.
                ExpressionToCode.ToCode(() => a == (SomeEnum)SomeFlagsEnum.A));
            Assert.AreEqual(
                @"() => a == ((SomeEnum)4)",
                //C# compiler does not preserve this type information; requires cast
                ExpressionToCode.ToCode(() => a == (SomeEnum)SomeFlagsEnum.C));
            Assert.AreEqual(
                @"() => a == (SomeEnum)b",
                //but it does here!
                ExpressionToCode.ToCode(() => a == (SomeEnum)b));
            Assert.AreEqual(
                @"() => (SomeFlagsEnum)a == b",
                //but it does here!
                ExpressionToCode.ToCode(() => (SomeFlagsEnum)a == b));
        }

        [Test]
        public void NullableEnumCornerCases() {
            SomeEnum? a = SomeEnum.A;
            SomeFlagsEnum? b = SomeFlagsEnum.B;

            Assert.AreEqual(
                @"() => a == SomeEnum.B",
                //C# compiler does not preserve this type information.
                ExpressionToCode.ToCode(() => a == (SomeEnum)SomeFlagsEnum.A));
            Assert.AreEqual(
                @"() => a == ((SomeEnum)4)",
                //C# compiler does not preserve this type information; requires cast
                ExpressionToCode.ToCode(() => a == (SomeEnum)SomeFlagsEnum.C));
            Assert.AreEqual(
                @"() => a == (SomeEnum)b",
                //but it does here!
                ExpressionToCode.ToCode(() => a == (SomeEnum)b));
            Assert.AreEqual(
                @"() => (SomeFlagsEnum?)a == b",
                //but it does here!
                ExpressionToCode.ToCode(() => (SomeFlagsEnum?)a == b));
        }
        
        [Test]
        public void NullableEnumCornerCases_FullNames()
        {
            SomeEnum? a = SomeEnum.A;
            SomeFlagsEnum? b = SomeFlagsEnum.B;

            var expressionToCode = ExpressionToCode.With(r => r.WithFullTypeNames());

            Assert.AreEqual(
                @"() => a == ExpressionToCodeTest.SomeEnum.B",
                //C# compiler does not preserve this type information.
                expressionToCode.ToCode(() => a == (SomeEnum)SomeFlagsEnum.A));
            Assert.AreEqual(
                @"() => a == ((ExpressionToCodeTest.SomeEnum)4)",
                //C# compiler does not preserve this type information; requires cast
                expressionToCode.ToCode(() => a == (SomeEnum)SomeFlagsEnum.C));
            Assert.AreEqual(
                @"() => a == (ExpressionToCodeTest.SomeEnum)b",
                //but it does here!
                expressionToCode.ToCode(() => a == (SomeEnum)b));
            Assert.AreEqual(
                @"() => (ExpressionToCodeTest.SomeFlagsEnum?)a == b",
                //but it does here!
                expressionToCode.ToCode(() => (SomeFlagsEnum?)a == b));
        }

        [Test]
        public void FlagsEnumConstant() {
            var ab = SomeFlagsEnum.A | SomeFlagsEnum.B;
            Assert.AreEqual(
                @"() => ab == SomeFlagsEnum.AB",
                ExpressionToCode.ToCode(() => ab == SomeFlagsEnum.AB));
        }

        [Test]
        public void NullableFlagsEnumConstant() {
            SomeFlagsEnum? ab = SomeFlagsEnum.A | SomeFlagsEnum.B;
            Assert.AreEqual(
                @"() => ab == SomeFlagsEnum.AB",
                ExpressionToCode.ToCode(() => ab == SomeFlagsEnum.AB));
        }

        [Test]
        public void FlagsEnumOr() {
            var a = SomeFlagsEnum.A;
            var b = SomeFlagsEnum.B;
            Assert.AreEqual(
                @"() => (SomeFlagsEnum)(a | b) == SomeFlagsEnum.AB",
                ExpressionToCode.ToCode(() => (a | b) == SomeFlagsEnum.AB)); //would be nice if this worked better, but not critical
        }

        [Test]
        public void NullableFlagsEnumOr() {
            SomeFlagsEnum a = SomeFlagsEnum.A;
            SomeFlagsEnum? b = SomeFlagsEnum.B;
            Assert.AreEqual(
                @"() => (SomeFlagsEnum?)(a | b) == SomeFlagsEnum.AB",
                ExpressionToCode.ToCode(() => (a | b) == SomeFlagsEnum.AB));
        }

        [Test]
        public void FlagsEnumComplexConstant() {
            var abc = SomeFlagsEnum.A | SomeFlagsEnum.B | SomeFlagsEnum.C;
            Assert.AreEqual(
                @"() => abc == (SomeFlagsEnum.AB | SomeFlagsEnum.C)",
                ExpressionToCode.ToCode(() => abc == (SomeFlagsEnum.AB | SomeFlagsEnum.C)));
        }

        [Test]
        public void NullableFlagsEnumComplexConstant() {
            SomeFlagsEnum? abc = SomeFlagsEnum.A | SomeFlagsEnum.B | SomeFlagsEnum.C;
            Assert.AreEqual(
                @"() => abc == (SomeFlagsEnum.AB | SomeFlagsEnum.C)",
                ExpressionToCode.ToCode(() => abc == (SomeFlagsEnum.AB | SomeFlagsEnum.C)));
        }
    }
}
