using System;
using System.Globalization;
using System.Linq;
using ExpressionToCodeLib;
using ExpressionToCodeLib.Internal;
using Xunit;

#pragma warning disable 252,253

// ReSharper disable NegativeEqualityExpression
// ReSharper disable EqualExpressionComparison
// ReSharper disable ConvertToConstant.Local
// ReSharper disable RedundantCast
namespace ExpressionToCodeTest
{
    public sealed class EqualityComparingTest
    {
        static readonly string bla = "bla";
        static readonly object bla2object = "bla2object";
        static readonly string bla2string = "bla2string";

        [Fact]
        public void EqualsOpDetected()
        {
            Assert.Equal(EqualityExpressionClass.EqualsOp, EqualityExpressions.CheckForEquality(() => bla == bla2object));
            Assert.Equal(EqualityExpressionClass.EqualsOp, EqualityExpressions.CheckForEquality(() => new DateTime(2011, 05, 17) == DateTime.Today));
        }

        [Fact]
        public void NotEqualsOpDetected()
        {
            Assert.Equal(EqualityExpressionClass.NotEqualsOp, EqualityExpressions.CheckForEquality(() => bla != bla2object));
            Assert.Equal(EqualityExpressionClass.NotEqualsOp, EqualityExpressions.CheckForEquality(() => new DateTime(2011, 05, 17) != DateTime.Today));
        }

        [Fact]
        public void EquatableDetected()
        { //TODO: makes interesting expressiontocode test too
            Assert.Equal(EqualityExpressionClass.EquatableEquals, EqualityExpressions.CheckForEquality(() => ((IEquatable<string>)bla).Equals(bla2string)));
            Assert.Equal(EqualityExpressionClass.EquatableEquals, EqualityExpressions.CheckForEquality(() => bla.Equals(bla2string)));
            Assert.Equal(EqualityExpressionClass.EquatableEquals, EqualityExpressions.CheckForEquality(() => new DateTime(2011, 05, 17).Equals(DateTime.Today)));
        }

        [Fact]
        public void NoneDetected()
        {
            Assert.Equal(EqualityExpressionClass.None, EqualityExpressions.CheckForEquality(() => bla.StartsWith("bla", StringComparison.Ordinal)));
            Assert.Equal(EqualityExpressionClass.None, EqualityExpressions.CheckForEquality(() => !(bla == bla2object)));
            Assert.Equal(EqualityExpressionClass.None, EqualityExpressions.CheckForEquality(() => DateTime.Equals(new DateTime(2011, 05, 17), DateTime.Today)));
            // no match since specific method
            Assert.Equal(EqualityExpressionClass.None, EqualityExpressions.CheckForEquality(() => !ReferenceEquals(null, null)));
        }

        [Fact]
        public void ObjectEqualsDetected()
        {
            Assert.Equal(EqualityExpressionClass.ObjectEquals, EqualityExpressions.CheckForEquality(() => bla2object.Equals(bla2object)));
            var anon = new { X = bla, Y = bla2object };
            Assert.Equal(EqualityExpressionClass.ObjectEquals, EqualityExpressions.CheckForEquality(() => anon.Equals(anon)));
        }

        [Fact]
        public void ObjectEqualsStaticDetected()
        {
            Assert.Equal(EqualityExpressionClass.ObjectEqualsStatic, EqualityExpressions.CheckForEquality(() => Equals(bla, null)));
            Assert.Equal(EqualityExpressionClass.ObjectEqualsStatic, EqualityExpressions.CheckForEquality(() => Equals(null, 42)));
        }

        [Fact]
        public void ObjectReferenceEqualsDetected()
        {
            Assert.Equal(EqualityExpressionClass.ObjectReferenceEquals, EqualityExpressions.CheckForEquality(() => ReferenceEquals(bla2object, bla2object)));
            Assert.Equal(EqualityExpressionClass.ObjectReferenceEquals, EqualityExpressions.CheckForEquality(() => ReferenceEquals(null, null)));
        }

        [Fact]
        public void SequenceEqualsDetected()
        {
            Assert.Equal(EqualityExpressionClass.SequenceEqual, EqualityExpressions.CheckForEquality(() => bla.AsEnumerable().SequenceEqual(bla2string)));
            Assert.Equal(EqualityExpressionClass.SequenceEqual, EqualityExpressions.CheckForEquality(() => new[] { 'b', 'l', 'a' }.SequenceEqual(bla2string)));
        }

        static Tuple<EqualityExpressionClass, bool>[] eqclasses(params EqualityExpressionClass[] classes)
            => classes.Select(eqClass => (eqClass, false).ToTuple()).ToArray();

        [Fact]
        public void StringEqDisagreement()
        {
            Assert.Equal(
                EqualityExpressions.DisagreeingEqualities(ExpressionToCodeConfiguration.DefaultAssertionConfiguration, () => ReferenceEquals(1000.ToString(CultureInfo.InvariantCulture), 10 + "00"))
                    .OrderBy(x => x),
                eqclasses(
                        EqualityExpressionClass.EqualsOp,
                        EqualityExpressionClass.NotEqualsOp,
                        EqualityExpressionClass.ObjectEquals,
                        EqualityExpressionClass.ObjectEqualsStatic,
                        EqualityExpressionClass.EquatableEquals,
                        EqualityExpressionClass.SequenceEqual,
                        EqualityExpressionClass.StructuralEquals
                    )
                    .OrderBy(x => x));
            Assert.Equal(
                EqualityExpressions.DisagreeingEqualities(ExpressionToCodeConfiguration.DefaultAssertionConfiguration, () => 1000.ToString(CultureInfo.InvariantCulture).Equals(10 + "00")).ToArray(),
                eqclasses(EqualityExpressionClass.ObjectReferenceEquals));
        }

        [Fact]
        public void DtRefEqDisagreement()
            => Assert.Equal(
                EqualityExpressions.DisagreeingEqualities(
                        ExpressionToCodeConfiguration.DefaultAssertionConfiguration,
                        // ReSharper disable ReferenceEqualsWithValueType
                        () => ReferenceEquals(new DateTime(2011, 05, 17), new DateTime(2011, 05, 17)))
                    .ToArray(),
                // ReSharper restore ReferenceEqualsWithValueType
                eqclasses(
                    EqualityExpressionClass.ObjectEquals,
                    EqualityExpressionClass.ObjectEqualsStatic,
                    EqualityExpressionClass.StructuralEquals
                ));

        [Fact]
        public void DtEqDisagreement()
            => Assert.Equal(
                EqualityExpressions.DisagreeingEqualities(ExpressionToCodeConfiguration.DefaultAssertionConfiguration, () => new DateTime(2011, 05, 17).Equals(new DateTime(2011, 05, 17))).ToArray(),
                eqclasses(EqualityExpressionClass.ObjectReferenceEquals));
    }
}
