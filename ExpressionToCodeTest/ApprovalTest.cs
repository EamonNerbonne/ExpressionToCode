using System.IO;

namespace ExpressionToCodeTest;

static class ApprovalTest
{
    public static void Verify(string text, [CallerFilePath] string? filePath = null, [CallerMemberName] string? memberName = null)
    {
        var filename = Path.GetFileNameWithoutExtension(filePath);
        var fileDir = Path.GetDirectoryName(filePath) ?? throw new InvalidOperationException("path " + filePath + " has no directory");
        var approvalPath = Path.Combine(fileDir , filename + "." + memberName + ".approved.txt");
        var isChanged = !File.Exists(approvalPath) || File.ReadAllText(approvalPath) != text;
        if (isChanged) {
            File.WriteAllText(approvalPath, text);
            throw new($"Approval changed; get git path: {approvalPath}");
        }
    }
}
