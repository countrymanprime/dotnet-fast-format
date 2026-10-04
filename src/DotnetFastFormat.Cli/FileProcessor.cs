using System.Text;
using DotnetFastFormat.Core;

namespace DotnetFastFormat.Cli;

/// <summary>Formats one file: decode, format, check the output, and write it only if it changed (invariant 4, fail safe).</summary>
internal sealed class FileProcessor(IFormatter formatter, IFileStore store)
{
    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>Formats the file at <paramref name="path"/> in place.</summary>
    /// <param name="path">The file.</param>
    /// <returns>The outcome and, for a failure, the reason. A failed file is never modified.</returns>
    public (FileOutcome Outcome, string Message) Process(string path)
    {
        try
        {
            byte[] original = store.Read(path);
            bool bom = original.AsSpan().StartsWith(Utf8Bom);
            string source = StrictUtf8.GetString(original, bom ? Utf8Bom.Length : 0, original.Length - (bom ? Utf8Bom.Length : 0));

            string formatted = formatter.Format(source);
            VerificationResult check = OutputVerifier.Verify(source, formatted);
            if (!check.Succeeded)
            {
                return (FileOutcome.Failed, $"internal error, output not written ({check.Violation}): {check.Message}");
            }

            byte[] result = [.. bom ? Utf8Bom : [], .. StrictUtf8.GetBytes(formatted)];
            if (result.AsSpan().SequenceEqual(original))
            {
                return (FileOutcome.Unchanged, string.Empty);
            }

            store.ReplaceAtomically(path, result);
            return (FileOutcome.Formatted, string.Empty);
        }
        catch (DecoderFallbackException)
        {
            return (FileOutcome.Failed, "the file is not valid UTF-8");
        }
        catch (Exception ex) when (ex is FormatterException or IOException or UnauthorizedAccessException)
        {
            return (FileOutcome.Failed, ex.Message);
        }
    }
}
