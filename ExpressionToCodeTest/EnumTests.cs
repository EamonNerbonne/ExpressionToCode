using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExpressionToCodeLib;
using ExpressionToCodeLib.Unstable_v2_Api;
using Xunit;

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

    public class EnumTests {
        [Fact]
        public void EnumConstant() {
            Assert.Equal(
                @"() => new object().Equals((object)MidpointRounding.ToEven)",
                ExpressionToCode.ToCode(() => new object().Equals(MidpointRounding.ToEven)));
        }

        [Fact]
        public void EnumVariables() {
            var a = SomeEnum.A;
            var b = SomeEnum.B;
            Assert.Equal(
                @"() => a == b",
                ExpressionToCode.ToCode(() => a == b));
        }

        [Fact]
        public void NullableEnumVariables() {
            var a = SomeEnum.A;
            SomeEnum? b = SomeEnum.B;
            Assert.Equal(
                @"() => (SomeEnum?)a == b",
                ExpressionToCode.ToCode(() => a == b));
        }

        [Fact]
        public void EnumVarEqConstant() {
            var a = SomeEnum.A;
            Assert.Equal(
                @"() => a == SomeEnum.B",
                ExpressionToCode.ToCode(() => a == SomeEnum.B));
            Assert.Equal(
                @"() => SomeEnum.B == a",
                ExpressionToCode.ToCode(() => SomeEnum.B == a));
        }

        [Fact]
        public void NullableEnumVarEqConstant() {
            SomeEnum? a = SomeEnum.A;
            Assert.Equal(
                @"() => a == (SomeEnum?)SomeEnum.B",
                ExpressionToCode.ToCode(() => a == SomeEnum.B));
            Assert.Equal(
                @"() => (SomeEnum?)SomeEnum.B == a",
                ExpressionToCode.ToCode(() => SomeEnum.B == a));
        }

        [Fact]
        public void EnumVarNeqConstant() {
            var a = SomeEnum.A;
            Assert.Equal(
                @"() => a != SomeEnum.B",
                ExpressionToCode.ToCode(() => a != SomeEnum.B));
            Assert.Equal(
                @"() => SomeEnum.B != a",
                ExpressionToCode.ToCode(() => SomeEnum.B != a));
        }

        [Fact]
        public void NullableEnumVarNeqConstant() {
            SomeEnum? a = SomeEnum.A;
            Assert.Equal(
                @"() => a != (SomeEnum?)SomeEnum.B",
                ExpressionToCode.ToCode(() => a != SomeEnum.B));
            Assert.Equal(
                @"() => (SomeEnum?)SomeEnum.B != a",
                ExpressionToCode.ToCode(() => SomeEnum.B != a));
        }

        [Fact]
        public void EnumVarLtConstant() {
            var a = SomeEnum.A;
            Assert.Equal(
                @"() => a < SomeEnum.B",
                ExpressionToCode.ToCode(() => a < SomeEnum.B));
            Assert.Equal(
                @"() => SomeEnum.B > a",
                ExpressionToCode.ToCode(() => SomeEnum.B > a));
        }

        [Fact]
        public void NullableEnumVarLtConstant() {
            SomeEnum? a = SomeEnum.A;
            Assert.Equal(
                @"() => a < (SomeEnum?)SomeEnum.B",
                ExpressionToCode.ToCode(() => a < SomeEnum.B));
            Assert.Equal(
                @"() => (SomeEnum?)SomeEnum.B > a",
                ExpressionToCode.ToCode(() => SomeEnum.B > a));
        }

        [Fact]
        public void EnumCornerCases() {
            var a = SomeEnum.A;
            var b = SomeFlagsEnum.B;
            Assert.Equal(
                @"() => a == SomeEnum.B",
                //C# compiler does not preserve this type information.
                ExpressionToCode.ToCode(() => a == (SomeEnum)SomeFlagsEnum.A));
            Assert.Equal(
                @"() => a == ((SomeEnum)4)",
                //C# compiler does not preserve this type information; requires cast
                ExpressionToCode.ToCode(() => a == (SomeEnum)SomeFlagsEnum.C));
            Assert.Equal(
                @"() => a == (SomeEnum)b",
                //but it does here!
                ExpressionToCode.ToCode(() => a == (SomeEnum)b));
            Assert.Equal(
                @"() => (SomeFlagsEnum)a == b",
                //but it does here!
                ExpressionToCode.ToCode(() => (SomeFlagsEnum)a == b));
        }

        [Fact]
        public void NullableEnumCornerCases() {
            SomeEnum? a = SomeEnum.A;
            SomeFlagsEnum? b = SomeFlagsEnum.B;

            Assert.Equal(
                @"() => a == (SomeEnum?)SomeEnum.B",
                //C# 6 compiler does not preserve this type information.
                ExpressionToCode.ToCode(() => a == (SomeEnum)SomeFlagsEnum.A));
            Assert.Equal(
                @"() => a == (SomeEnum?)((SomeEnum)4)",
                //C# 6 compiler does not preserve this type information; requires cast
                ExpressionToCode.ToCode(() => a == (SomeEnum)SomeFlagsEnum.C));
            Assert.Equal(
                @"() => a == (SomeEnum?)(SomeEnum)b",
                //but it does here!
                ExpressionToCode.ToCode(() => a == (SomeEnum)b));
            Assert.Equal(
                @"() => (SomeFlagsEnum?)a == b",
                //but it does here!
                ExpressionToCode.ToCode(() => (SomeFlagsEnum?)a == b));
        }
        
        [Fact]
        public void NullableEnumCornerCases_FullNames()
        {
            SomeEnum? a = SomeEnum.A;
            SomeFlagsEnum? b = SomeFlagsEnum.B;

            var exprToCode = ExpressionStringify.With(fullTypeNames: true);

            Assert.Equal(
                @"() => a == (ExpressionToCodeTest.SomeEnum?)ExpressionToCodeTest.SomeEnum.B",
                //C# compiler does not preserve this type information.
                exprToCode.ToCode(() => a == (SomeEnum)SomeFlagsEnum.A));
            Assert.Equal(
                @"() => a == (ExpressionToCodeTest.SomeEnum?)((ExpressionToCodeTest.SomeEnum)4)",
                //C# compiler does not preserve this type information; requires cast
                exprToCode.ToCode(() => a == (SomeEnum)SomeFlagsEnum.C));
            Assert.Equal(
                @"() => a == (ExpressionToCodeTest.SomeEnum?)(ExpressionToCodeTest.SomeEnum)b",
                //but it does here!
                exprToCode.ToCode(() => a == (SomeEnum)b));
            Assert.Equal(
                @"() => (ExpressionToCodeTest.SomeFlagsEnum?)a == b",
                //but it does here!
                exprToCode.ToCode(() => (SomeFlagsEnum?)a == b));
        }

        [Fact]
        public void FlagsEnumConstant() {
            var ab = SomeFlagsEnum.A | SomeFlagsEnum.B;
            Assert.Equal(
                @"() => ab == SomeFlagsEnum.AB",
                ExpressionToCode.ToCode(() => ab == SomeFlagsEnum.AB));
        }

        [Fact]
        public void NullableFlagsEnumConstant() {
            SomeFlagsEnum? ab = SomeFlagsEnum.A | SomeFlagsEnum.B;
            Assert.Equal(
                @"() => ab == (SomeFlagsEnum?)SomeFlagsEnum.AB",
                ExpressionToCode.ToCode(() => ab == SomeFlagsEnum.AB));
        }

        [Fact]
        public void FlagsEnumOr() {
            var a = SomeFlagsEnum.A;
            var b = SomeFlagsEnum.B;
            Assert.Equal(
                @"() => (SomeFlagsEnum)(a | b) == SomeFlagsEnum.AB",
                ExpressionToCode.ToCode(() => (a | b) == SomeFlagsEnum.AB)); //would be nice if this worked better, but not critical
        }

        [Fact]
        public void NullableFlagsEnumOr() {
            SomeFlagsEnum a = SomeFlagsEnum.A;
            SomeFlagsEnum? b = SomeFlagsEnum.B;
            Assert.Equal(
                @"() => (SomeFlagsEnum?)(a | b) == SomeFlagsEnum.AB",
                ExpressionToCode.ToCode(() => (a | b) == SomeFlagsEnum.AB));
        }

        [Fact]
        public void FlagsEnumComplexConstant() {
            var abc = SomeFlagsEnum.A | SomeFlagsEnum.B | SomeFlagsEnum.C;
            Assert.Equal(
                @"() => abc == (SomeFlagsEnum.AB | SomeFlagsEnum.C)",
                ExpressionToCode.ToCode(() => abc == (SomeFlagsEnum.AB | SomeFlagsEnum.C)));
        }

        [Fact]
        public void NullableFlagsEnumComplexConstant() {
            SomeFlagsEnum? abc = SomeFlagsEnum.A | SomeFlagsEnum.B | SomeFlagsEnum.C;
            Assert.Equal(
                @"() => abc == (SomeFlagsEnum?)(SomeFlagsEnum.AB | SomeFlagsEnum.C)",
                ExpressionToCode.ToCode(() => abc == (SomeFlagsEnum.AB | SomeFlagsEnum.C)));
        }
    }
}
