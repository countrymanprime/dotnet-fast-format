using System.Text;

namespace DotnetFastFormat.Core.Layout;

/// <summary>
/// Lays a document out to a line width: a group is printed flat when it fits in the remaining width and broken
/// otherwise, outermost first. The algorithm follows Prettier's document printer. It uses explicit stacks, so
/// the depth of a document is limited by memory, not by the call stack.
/// </summary>
internal static class DocPrinter
{
    private static readonly List<Command> NoCommands = [];

    [ThreadStatic]
    private static List<(Mode Mode, Doc Doc)>? measureStack;

    private enum Mode
    {
        Break,
        Flat,
    }

    private enum FitResult
    {
        Continue,
        Fits,
        DoesNotFit,
    }

    /// <summary>Prints a document.</summary>
    /// <param name="doc">The document.</param>
    /// <param name="options">The width, indent size and line terminator.</param>
    /// <returns>The printed text.</returns>
    public static string Print(Doc doc, DocPrintOptions options)
    {
        var state = new PrintState(options);
        var commands = new List<Command> { new(0, Mode.Break, doc) };

        while (commands.Count > 0)
        {
            Command command = commands[^1];
            commands.RemoveAt(commands.Count - 1);
            Step(command, commands, state);
        }

        state.FlushSuffix();
        return state.Output.ToString();
    }

    private static void Step(Command command, List<Command> commands, PrintState state)
    {
        switch (command.Doc)
        {
            case TextDoc text:
                state.Output.Append(text.Value);
                state.Position += text.Value.Length;
                break;

            case VerbatimDoc verbatim:
                state.AppendVerbatim(verbatim.Value);
                break;

            case ConcatDoc concat:
                PushReversed(commands, concat.Parts, command.Indent, command.Mode);
                break;

            case IndentDoc indent:
                commands.Add(new Command(command.Indent + 1, command.Mode, indent.Contents));
                break;

            case GroupDoc group:
                PrintGroup(group, command, commands, state.Options.Width - state.Position, state.Options.Width);
                break;

            case ConditionalGroupDoc conditional:
                PrintConditional(conditional, command, commands, state.Options.Width - state.Position, state.Options.Width);
                break;

            case LineDoc line:
                state.AppendLine(line.Kind, command);
                break;

            case FillDoc fill:
                PrintFill(fill, command, commands, state.Options.Width - state.Position, state.Options.Width);
                break;

            case LineSuffixDoc suffix:
                state.PendingSuffix += suffix.Text;
                break;

            case ColumnZeroDoc columnZero:
                state.TrimLineIndent();
                commands.Add(new Command(0, command.Mode, columnZero.Contents));
                break;

            case IfBreakDoc ifBreak:
                commands.Add(command with { Doc = command.Mode == Mode.Break ? ifBreak.BreakContents : ifBreak.FlatContents });
                break;

            default:
                throw new InvalidOperationException($"Unknown document type {command.Doc.GetType().Name}.");
        }
    }

    private static void PrintGroup(GroupDoc group, Command command, List<Command> commands, int remaining, int width)
    {
        if (command.Mode == Mode.Flat)
        {
            commands.Add(new Command(command.Indent, group.ForcesBreak ? Mode.Break : Mode.Flat, group.Contents));
            return;
        }

        var flat = new Command(command.Indent, Mode.Flat, group.Contents);
        commands.Add(!group.ForcesBreak && Fits(flat, commands, remaining, width, mustBeFlat: false)
            ? flat
            : new Command(command.Indent, Mode.Break, group.Contents));
    }

    private static void PrintConditional(ConditionalGroupDoc group, Command command, List<Command> commands, int remaining, int width)
    {
        IReadOnlyList<Doc> states = group.States;
        if (command.Mode == Mode.Flat)
        {
            commands.Add(new Command(command.Indent, states[0].ForcesBreak ? Mode.Break : Mode.Flat, states[0]));
            return;
        }

        for (int i = 0; i < states.Count - 1; i++)
        {
            var candidate = new Command(command.Indent, Mode.Flat, states[i]);
            if (Fits(candidate, commands, remaining, width, mustBeFlat: false))
            {
                commands.Add(candidate);
                return;
            }
        }

        commands.Add(new Command(command.Indent, Mode.Break, states[^1]));
    }

    private static void PrintFill(FillDoc fill, Command command, List<Command> commands, int remaining, int width)
    {
        IReadOnlyList<Doc> parts = fill.Parts;
        int left = parts.Count - command.FillOffset;
        if (left == 0)
        {
            return;
        }

        Doc content = parts[command.FillOffset];
        var contentFlat = new Command(command.Indent, Mode.Flat, content);
        var contentBroken = new Command(command.Indent, Mode.Break, content);
        bool contentFits = Fits(contentFlat, NoCommands, remaining, width, mustBeFlat: true);

        if (left == 1)
        {
            commands.Add(contentFits ? contentFlat : contentBroken);
            return;
        }

        Doc separator = parts[command.FillOffset + 1];
        var separatorFlat = new Command(command.Indent, Mode.Flat, separator);
        var separatorBroken = new Command(command.Indent, Mode.Break, separator);

        if (left == 2)
        {
            commands.Add(contentFits ? separatorFlat : separatorBroken);
            commands.Add(contentFits ? contentFlat : contentBroken);
            return;
        }

        Doc second = parts[command.FillOffset + 2];
        var pair = new Command(command.Indent, Mode.Flat, new ConcatDoc([content, separator, second]));
        bool pairFits = Fits(pair, NoCommands, remaining, width, mustBeFlat: true);

        commands.Add(new Command(command.Indent, command.Mode, fill, command.FillOffset + 2));
        if (pairFits)
        {
            commands.Add(separatorFlat);
            commands.Add(contentFlat);
        }
        else if (contentFits)
        {
            commands.Add(separatorBroken);
            commands.Add(contentFlat);
        }
        else
        {
            commands.Add(separatorBroken);
            commands.Add(contentBroken);
        }
    }

    /// <summary>
    /// Whether <paramref name="next"/> fits in <paramref name="remaining"/> columns, counting what follows it up to
    /// the first line break that will be taken.
    /// </summary>
    private static bool Fits(Command next, List<Command> rest, int remaining, int width, bool mustBeFlat)
    {
        List<(Mode Mode, Doc Doc)> stack = measureStack ??= [];
        stack.Clear();
        stack.Add((next.Mode, next.Doc));
        int restIndex = rest.Count;

        while (remaining >= 0)
        {
            if (stack.Count == 0)
            {
                if (restIndex == 0)
                {
                    return true;
                }

                Command following = rest[--restIndex];
                stack.Add((following.Mode, following.Doc));
                continue;
            }

            (Mode mode, Doc doc) = stack[^1];
            stack.RemoveAt(stack.Count - 1);

            FitResult result = Measure(mode, doc, stack, ref remaining, width, mustBeFlat);
            if (result != FitResult.Continue)
            {
                return result == FitResult.Fits;
            }
        }

        return false;
    }

    private static FitResult Measure(Mode mode, Doc doc, List<(Mode Mode, Doc Doc)> stack, ref int remaining, int width, bool mustBeFlat)
    {
        switch (doc)
        {
            case TextDoc text:
                remaining -= text.Value.Length;
                break;

            case VerbatimDoc verbatim:
                return MeasureVerbatim(verbatim, ref remaining, width);

            case GroupDoc group:
                if (mustBeFlat && group.ForcesBreak)
                {
                    return FitResult.DoesNotFit;
                }

                stack.Add((group.ForcesBreak ? Mode.Break : mode, group.Contents));
                break;

            case LineDoc line:
                if (mode == Mode.Break || line.Kind == LineKind.Hard)
                {
                    return FitResult.Fits;
                }

                if (line.Kind == LineKind.Normal)
                {
                    remaining--;
                }

                break;

            case ConditionalGroupDoc conditional:
                stack.Add((mode, mode == Mode.Break ? conditional.States[^1] : conditional.States[0]));
                break;

            default:
                Expand(mode, doc, stack);
                break;
        }

        return FitResult.Continue;
    }

    /// <summary>
    /// Text with a forced line break ends the measure at its first line break. A string literal that spans lines needs its
    /// first line to fit, and what follows it starts after its last line, so the width left is measured from there.
    /// </summary>
    private static FitResult MeasureVerbatim(VerbatimDoc verbatim, ref int remaining, int width)
    {
        ReadOnlySpan<char> value = verbatim.Value.AsSpan();
        int firstBreak = value.IndexOfAny('\r', '\n');
        if (firstBreak < 0)
        {
            remaining -= value.Length;
            return FitResult.Continue;
        }

        if (remaining - firstBreak < 0)
        {
            return FitResult.DoesNotFit;
        }

        if (verbatim.ForcesBreak)
        {
            return FitResult.Fits;
        }

        remaining = width - (value.Length - value.LastIndexOfAny('\r', '\n') - 1);
        return FitResult.Continue;
    }

    private static void Expand(Mode mode, Doc doc, List<(Mode Mode, Doc Doc)> stack)
    {
        switch (doc)
        {
            case ConcatDoc concat:
                PushReversed(stack, concat.Parts, mode);
                break;
            case FillDoc fill:
                PushReversed(stack, fill.Parts, mode);
                break;
            case IndentDoc indent:
                stack.Add((mode, indent.Contents));
                break;
            case ColumnZeroDoc columnZero:
                stack.Add((mode, columnZero.Contents));
                break;
            case IfBreakDoc ifBreak:
                stack.Add((mode, mode == Mode.Break ? ifBreak.BreakContents : ifBreak.FlatContents));
                break;
        }
    }

    private static void PushReversed(List<Command> commands, IReadOnlyList<Doc> parts, int indent, Mode mode)
    {
        for (int i = parts.Count - 1; i >= 0; i--)
        {
            commands.Add(new Command(indent, mode, parts[i]));
        }
    }

    private static void PushReversed(List<(Mode Mode, Doc Doc)> stack, IReadOnlyList<Doc> parts, Mode mode)
    {
        for (int i = parts.Count - 1; i >= 0; i--)
        {
            stack.Add((mode, parts[i]));
        }
    }

    private readonly record struct Command(int Indent, Mode Mode, Doc Doc, int FillOffset = 0);

    private sealed class PrintState(DocPrintOptions options)
    {
        private int trimBarrier;

        public DocPrintOptions Options { get; } = options;

        public StringBuilder Output { get; } = new();

        public int Position { get; set; }

        public string PendingSuffix { get; set; } = string.Empty;

        public void FlushSuffix()
        {
            Output.Append(PendingSuffix);
            Position += PendingSuffix.Length;
            PendingSuffix = string.Empty;
        }

        public void TrimLineIndent()
        {
            // Only indentation and the spaces the printer writes can sit between the barrier and here, so a tab
            // is one of the printer's own and counts for the tab width.
            while (Output.Length > trimBarrier && Output[^1] is ' ' or '\t')
            {
                Position -= Output[^1] == '\t' ? Options.TabWidth : 1;
                Output.Length--;
            }
        }

        public void AppendVerbatim(string value)
        {
            Output.Append(value);
            int lastBreak = value.AsSpan().LastIndexOfAny('\r', '\n');
            Position = lastBreak < 0 ? Position + value.Length : value.Length - lastBreak - 1;
            trimBarrier = Output.Length;
        }

        public void AppendLine(LineKind kind, Command command)
        {
            if (command.Mode == Mode.Flat && kind != LineKind.Hard)
            {
                if (kind == LineKind.Normal)
                {
                    Output.Append(' ');
                    Position++;
                }

                return;
            }

            FlushSuffix();
            while (Output.Length > trimBarrier && Output[^1] is ' ' or '\t')
            {
                Output.Length--;
            }

            Output.Append(Options.NewLine);
            Position = command.Indent * Options.IndentSize;
            AppendIndentation(Position);
        }

        private void AppendIndentation(int columns)
        {
            if (!Options.UseTabs)
            {
                Output.Append(' ', columns);
                return;
            }

            Output.Append('\t', columns / Options.TabWidth);
            Output.Append(' ', columns % Options.TabWidth);
        }
    }
}
