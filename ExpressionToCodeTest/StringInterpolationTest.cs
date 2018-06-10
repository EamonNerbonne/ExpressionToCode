using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ExpressionToCodeLib;
using Xunit;

namespace ExpressionToCodeTest
{
    public class StringInterpolationTest
    {
        // ReSharper disable once MemberCanBeMadeStatic.Local
        FormattableString Interpolation(FormattableString str)
            => str;

        [Fact]
        public void InterpolationWithoutArgumentsIsJustAString()
            => Assert.Equal(
                @"() => ""abc""",
                ExpressionToCode.ToCode(() => $"abc"));

        [Fact]
        public void ForcedInterpolationWorks()
            => Assert.Equal(
                @"() => Interpolation($""abc"")",
                ExpressionToCode.ToCode(() => Interpolation($"abc")));

        [Fact]
        public void FormattableStringFactory_IsRenderedAsInterpolation()
            => Assert.Equal(
                @"() => Interpolation($""abc"")",
                ExpressionToCode.ToCode(
                    () => Interpolation(FormattableStringFactory.Create("abc"))
                ));

        [Fact]
        public void FormattableStringFactory_NonConstantStringIsNoInterpolation()
        {
            var s = "abc";
            Assert.Equal(
                @"() => Interpolation(FormattableStringFactory.Create(s, new object[] { }))",
                ExpressionToCode.ToCode(
                    () => Interpolation(FormattableStringFactory.Create(s))
                ));
        }

        [Fact]
        public void FormattableStringFactory_NonInlineArrayIsNoInterpolation()
        {
            var arr = new object[0];

            Assert.Equal(
                @"() => Interpolation(FormattableStringFactory.Create(""abc"", arr))",
                ExpressionToCode.ToCode(
                    () => Interpolation(FormattableStringFactory.Create("abc", arr))
                ));
        }

        [Fact]
        public void ForcedInterpolationWithOneArg()
            => Assert.Equal(
                @"() => Interpolation($""abc {3.0f}"")",
                ExpressionToCode.ToCode(() => Interpolation($"abc {3f}")));

        [Fact]
        public void NonForcedInterpolationWithOneArg()
            => Assert.Equal(
                @"() => Interpolation($""abc {3.0f}"")",
                ExpressionToCode.ToCode(() => $"abc {3f}"));

        [Fact]
        public void ForcedInterpolationWithNestedString()
            => Assert.Equal(
                @"() => Interpolation($""abc {""def""}"")",
                ExpressionToCode.ToCode(() => Interpolation($"abc {"def"}")));

        [Fact]
        public void ForcedInterpolationWithNestedInterpolation()
            => Assert.Equal(
                @"() => Interpolation($""abc {Interpolation($""abc {""def""}"")}"")",
                ExpressionToCode.ToCode(
                    () => Interpolation($"abc {Interpolation($"abc {"def"}")}")
                    )
            );

        [Fact]
        public void ForcedInterpolationWithTwoArguments()
            => Assert.Equal(
                @"() => Interpolation($""abc {3.0f} X {'a'} Y"")",
                ExpressionToCode.ToCode(() => Interpolation($"abc {3f} X {'a'} Y")));

        [Fact]
        public void ForcedInterpolationWithAdditionInArgument()
            => Assert.Equal(
                @"() => Interpolation($""abc {""abc"".Length + 3.5} Z"")",
                ExpressionToCodeConfiguration.DefaultCodeGenConfiguration.WithOmitImplicitCasts(true).ToCode(() => Interpolation($"abc {"abc".Length + 3.5} Z")));

        [Fact]
        public void ForcedInterpolationWithTernaryArgumentNeedsParens()
        {
            var aBoolean = true;

            Assert.Equal(
                @"() => Interpolation($""abc {(aBoolean ? 1 : 2)} Z"")",
                ExpressionToCode.ToCode(() => Interpolation($"abc {(aBoolean ? 1 : 2)} Z")));
        }

        [Fact]
        public void ForcedInterpolationWithNewlinesInSubExprIsLiteral()
        {
            var aBoolean = true;

            Assert.Equal(
                @"() => Interpolation($@""abc {new { I = @""1
2
3
4
"", J = 1 }} Z"")",
                ExpressionToCode.ToCode(
                    () => Interpolation($@"abc {new { I = @"1
2
3
4
", J = 1 }} Z")
                ));
        }


        [Fact]
        public void ForcedInterpolationWithFormatSpecifier()
            => Assert.Equal(
                @"() => Interpolation($""abc {DateTime.Now:somespecifier: yep!} Z"")",
                ExpressionToCode.ToCode(() => Interpolation($"abc {DateTime.Now:somespecifier: yep!} Z")));

        [Fact]
        public void ForcedInterpolationWithCurlyBraces()
            => Assert.Equal(
                @"() => Interpolation($""abc {{!}}"")",
                ExpressionToCode.ToCode(() => Interpolation($"abc {{!}}")));
    }
}
