using System.Text;
using DotnetFastFormat.Core;
using DotnetFastFormat.Core.Config;

namespace DotnetFastFormat.Cli;

/// <summary>
/// Formats one file: resolve its <c>.editorconfig</c> settings, decode, format, check the output, apply the byte
/// order mark the settings ask for, and write only if the bytes changed (invariant 4, fail safe).
/// </summary>
internal sealed class FileProcessor(IFormatter formatter, IFileStore store, EditorConfigResolver settings, Action<string> warn)
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

            ResolvedSettings resolved = settings.Resolve(path);
            foreach (string warning in resolved.Warnings)
            {
                warn(warning);
            }

            string formatted = formatter.Format(source, resolved.Options);
            VerificationResult check = OutputVerifier.Verify(source, formatted);
            if (!check.Succeeded)
            {
                return (FileOutcome.Failed, $"internal error, output not written ({check.Violation}): {check.Message}");
            }

            bool writeBom = resolved.Options.ByteOrderMark switch
            {
                ByteOrderMarkPolicy.Add => formatted.Length > 0,
                ByteOrderMarkPolicy.Remove => false,
                _ => bom,
            };
            byte[] result = [.. writeBom ? Utf8Bom : [], .. StrictUtf8.GetBytes(formatted)];
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
