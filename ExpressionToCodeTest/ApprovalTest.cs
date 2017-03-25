using System.IO;
using System.Runtime.CompilerServices;
using ApprovalTests.Approvers;
using ApprovalTests.Core;
using ApprovalTests.Reporters;
using ApprovalTests.Writers;

namespace ExpressionToCodeTest
{
    internal static class ApprovalTest
    {
        public static void Verify(string text, [CallerFilePath] string filepath = null, [CallerMemberName] string membername = null)
        {
            var writer = WriterFactory.CreateTextWriter(text);
            var filename = Path.GetFileNameWithoutExtension(filepath);
            var filedir = Path.GetDirectoryName(filepath);
            var namer = new SaneNamer { Name = filename + "." + membername, SourcePath = filedir };
            var reporter = new DiffReporter();
            Approver.Verify(new FileApprover(writer, namer, true), reporter);
        }

        public class SaneNamer : IApprovalNamer
        {
            public string SourcePath { get; set; }
            public string Name { get; set; }
        }
    }
}
