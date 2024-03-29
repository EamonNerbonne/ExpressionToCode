namespace ExpressionToCodeLib;

public static class FormatStringParser
{
    public struct Segment
    {
        public string InitialStringPart;
        public object? FollowedByValue;
        public string? WithFormatString;
    }

    public static (Segment[] segments, string Tail) ParseFormatString(string formatString, object[] formatArguments)
        => new FormattableStringParser(formatString, formatArguments).Finish();

    sealed class FormattableStringParser : IFormatProvider, ICustomFormatter
    {
        //Format strings have exceptions for stuff like double curly braces
        //There are also corner-cases that non-compiler generated format strings might hit
        //All in all: rather than reimplement a custom parser, this
        readonly StringBuilder sb = new();
        readonly List<Segment> Segments = new();

        public FormattableStringParser(string formatString, object[] formatArguments)
            => sb.AppendFormat(this, formatString, formatArguments);

        object IFormatProvider.GetFormat(Type? formatType)
            => this;

        public (Segment[] segments, string Tail) Finish()
            => (Segments.ToArray(), sb.ToString());

        string ICustomFormatter.Format(string? format, object? arg, IFormatProvider? formatProvider)
        {
            Segments.Add(
                new() {
                    InitialStringPart = sb.ToString(),
                    FollowedByValue = arg,
                    WithFormatString = format,
                });
            sb.Length = 0;
            return "";
        }
    }
}
