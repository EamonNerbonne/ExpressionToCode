using System.Globalization;
using System.Linq;
using ExpressionToCodeLib;
using Xunit;

namespace ExpressionToCodeTest
{
    public class EnumerableFormattingTest
    {
        [Fact]
        public void ShortArraysRenderedCompletely()
        {
            Assert.Equal("new[] {1, 2, 3}", ObjectToCode.ComplexObjectToPseudoCode(new[] { 1, 2, 3 }));
        }

        [Fact]
        public void ShortEnumerablesRenderedCompletely()
        {
            Assert.Equal("{1, 2, 3}", ObjectToCode.ComplexObjectToPseudoCode(Enumerable.Range(1, 3)));
        }

        [Fact]
        public void LongEnumerablesBreakAfter10()
        {
            Assert.Equal("{1, 2, 3, 4, 5, 6, 7, 8, 9, 10, ...}", ObjectToCode.ComplexObjectToPseudoCode(Enumerable.Range(1, 13)));
        }

        [Fact]
        public void LongArraysBreakAfter10()
        {
            Assert.Equal("new[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, ...}", ObjectToCode.ComplexObjectToPseudoCode(Enumerable.Range(1, 13).ToArray()));
        }

        [Fact]
        public void LongArraysDoNotBreakIfSoConfigured()
        {
            var config = ExpressionToCodeConfiguration.DefaultConfiguration.WithPrintedListLengthLimit(null);
            Assert.Equal("new[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13}", ObjectToCode.ComplexObjectToPseudoCode(config, Enumerable.Range(1, 13).ToArray()));
        }

        [Fact]
        public void EnumerableElisionIsConfigurable()
        {
            var config = ExpressionToCodeConfiguration.DefaultConfiguration.WithPrintedListLengthLimit(3);
            Assert.Equal("{1, 2, 3, ...}", ObjectToCode.ComplexObjectToPseudoCode(config, Enumerable.Range(1, 13)));
        }

        [Fact]
        public void InsertsLineBreaksWhenNecessary()
        {
            Assert.Equal(
                @"{
  ""12345000"",
  ""12345001"",
  ""12345002"",
  ""12345003"",
  ""12345004"",
  ""12345005"",
  ""12345006"",
  ""12345007"",
  ""12345008"",
  ""12345009"",
  ...
}",
                ObjectToCode.ComplexObjectToPseudoCode(Enumerable.Range(12345000, 13).Select(i => i.ToString(CultureInfo.InvariantCulture))));
        }

        [Fact]
        public void InsertsLineBreaksContentsContainLineBreaks()
        {
            Assert.Equal(
                @"{
  new {
          A = 3,
          B = 'b',
        },
  new {
          A = 3,
          B = 'b',
        },
}",
                ObjectToCode.ComplexObjectToPseudoCode(Enumerable.Repeat(new { A = 3, B = 'b' }, 2)));
        }

        [Fact]
        public void NestedArraysUseProperConfig()
        {
            var config = ExpressionToCodeConfiguration.DefaultConfiguration.WithPrintedListLengthLimit(3);
            Assert.Equal(
                "new[] {\n  null,\n  new {\n          A = 3,\n          B = new[] {1, 2, 3, ...},\n        },\n}",
                ObjectToCode.ComplexObjectToPseudoCode(config, new[] { null, new { A = 3, B = new[] { 1, 2, 3, 4, 5 } } }));
        }
    }
}
