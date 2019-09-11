using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExpressionToCodeLib
{
    public static class FormatStringParser
    {
        public struct Segment
        {
            public string InitialStringPart;
            public object FollowedByValue;
            public string WithFormatString;
        }

#if !NET452
        public static (Segment[] segments, string Tail) ParseFormatString(FormattableString formattableString)
            => new FormattableStringParser(formattableString.Format, formattableString.GetArguments()).Finish();
#endif

        public static (Segment[] segments, string Tail) ParseFormatString(string formatString, object[] formatArguments)
            => new FormattableStringParser(formatString, formatArguments).Finish();

        sealed class FormattableStringParser : IFormatProvider, ICustomFormatter
        {
            //Format strings have exceptions for stuff like double curly braces
            //There are also corner-cases that non-compiler generated format strings might hit
            //All in all: rather than reimplement a custom parser, this
            readonly StringBuilder sb = new StringBuilder();
            readonly List<Segment> Segments = new List<Segment>();

            public FormattableStringParser(string formatString, object[] formatArguments)
                => sb.AppendFormat(this, formatString, formatArguments);

            object IFormatProvider.GetFormat(Type formatType)
                => this;

            public (Segment[] segments, string Tail) Finish()
                => (Segments.ToArray(), sb.ToString());

            string ICustomFormatter.Format(string format, object arg, IFormatProvider formatProvider)
            {
                Segments.Add(
                    new Segment {
                        InitialStringPart = sb.ToString(),
                        FollowedByValue = arg,
                        WithFormatString = format,
                    });
                sb.Length = 0;
                return "";
            }
        }
    }
}
