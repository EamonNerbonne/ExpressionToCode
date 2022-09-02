namespace ExpressionToCodeTest;

public class PAssertTest
{
    static readonly ExpressionToCodeConfiguration config =
        ExpressionToCodeConfiguration.DefaultAssertionConfiguration.WithAnnotator(CodeAnnotators.ValuesOnStalksCodeAnnotator).WithCompiler(ExpressionTreeCompilers.DotnetExpressionCompiler);

    [Fact]
    public void TestBasicStalks()
    {
        var msgLines = PAssertLines(
            () =>
                config.Assert(
                    () =>
                        TimeSpan.FromMilliseconds(10.0).CompareTo(TimeSpan.FromMinutes(1.0)) > 0
                ));
        Assert.Equal(@"TimeSpan.FromMilliseconds(10.0).CompareTo(TimeSpan.FromMinutes(1.0)) > 0  :  assertion failed", msgLines[0]);
        var expectedMessage = @"
TimeSpan.FromMilliseconds(10.0).CompareTo(TimeSpan.FromMinutes(1.0)) > 0  :  assertion failed
                 │                  │                   │
                 │                  │                   00:01:00
                 │                  -1
                 00:00:00.0100000
";
        Assert.Equal(expectedMessage.Replace("\r", "").Trim(), string.Join("\n", msgLines));
    }

    [Fact]
    public void NoValuesForBoringCasts()
    {
        var msgLines = PAssertLines(
            () =>
                config.Assert(
                    () =>
                        Equals(3, 4)
                ));
        Assert.Contains("failed", msgLines[0]);
        Assert.Contains("Equals", msgLines[0]);
        Assert.Single(msgLines);
    }

    [Fact]
    public void ValuesForNonBoringCasts()
    {
        var x = ulong.MaxValue;
        var msgLines = PAssertLines(
            () => config.Assert(
                () => 0 == (ulong)(uint)x
            ));
        Assert.Equal(@"0UL == (uint)x  :  assertion failed", (object)msgLines[0]);
        Assert.Equal(2, (object)msgLines[1].Count(c => c == '│'));
    }

    [Fact]
    public void AppendsAssertionFailedOnFailure()
    {
        var msgLines = PAssertLines(() => config.Assert(() => false));
        Assert.Equal(@"false  :  assertion failed", (object)msgLines[0]);
        Assert.Equal(1, (object)msgLines.Length);
    }

    [Fact]
    public void AppendsSingleLineMessageOnFailure()
    {
        var msgLines = PAssertLines(() => config.Assert(() => false, "oops"));
        Assert.Equal(@"false  :  oops", (object)msgLines[0]);
        Assert.Equal(1, (object)msgLines.Length);
    }

    [Fact]
    public void AppendsSingleLineMessageWithNewlineOnFailure()
    {
        var msgLines = PAssertLines(() => config.Assert(() => false, "oops\n"));
        Assert.Equal(@"false  :  oops", (object)msgLines[0]);
        Assert.Equal(1, (object)msgLines.Length);
    }

    [Fact]
    public void AppendsSingleLineMessageBeforeStalks()
    {
        var x = 0;
        var msgLines = PAssertLines(() => config.Assert(() => x == 1, "oops\n"));
        Assert.Equal(@"x == 1  :  oops", (object)msgLines[0]);
        Assert.Equal(3, (object)msgLines.Length);
    }

    [Fact]
    public void PrependsMultiLineMessage()
    {
        var x = 0;
        var msgLines = PAssertLines(() => config.Assert(() => x == 1, "oops\nagain"));
        Assert.Equal(@"oops", (object)msgLines[0]);
        Assert.Equal(@"again", (object)msgLines[1]);
        Assert.Equal(@"x == 1", (object)msgLines[2]);
        Assert.Equal(5, (object)msgLines.Length);
    }

    static string[] PAssertLines(Action action)
    {
        var exc = Assert.ThrowsAny<Exception>(action);
        return exc.Message.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
    }
}
