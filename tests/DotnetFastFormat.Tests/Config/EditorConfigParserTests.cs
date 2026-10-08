using DotnetFastFormat.Core.Config;

namespace DotnetFastFormat.Tests.Config;

public class EditorConfigParserTests
{
    [Theory]
    [InlineData("")]
    [InlineData("\n\n")]
    [InlineData("; a comment\n# another\n   \n")]
    public void AnEmptyFileHasNoSectionsAndIsNotRoot(string text)
    {
        EditorConfigFile file = EditorConfigParser.Parse(text);

        Assert.False(file.IsRoot);
        Assert.Empty(file.Sections);
        Assert.Empty(file.Problems);
    }

    [Theory]
    [InlineData("root = true", true)]
    [InlineData("root=true", true)]
    [InlineData("ROOT = TRUE", true)]
    [InlineData("Root = True\n[*]\nindent_size = 2", true)]
    [InlineData("root = false", false)]
    [InlineData("root = yes", false)]
    [InlineData("root =", false)]
    [InlineData("[*]\nroot = true", false)]
    [InlineData("indent_size = 2\n[*]\nroot = true", false)]
    public void RootCountsOnlyInThePreamble(string text, bool expected) =>
        Assert.Equal(expected, EditorConfigParser.Parse(text).IsRoot);

    [Fact]
    public void SectionsAndPairsAreReadInOrder()
    {
        EditorConfigFile file = EditorConfigParser.Parse("root = true\n\n[*]\nindent_size = 4\nend_of_line = lf\n\n[*.cs]\nindent_size = 2\n");

        Assert.Collection(
            file.Sections,
            first =>
            {
                Assert.Equal("*", first.Name);
                Assert.Equal(3, first.Line);
                Assert.Equal([("indent_size", "4", 4), ("end_of_line", "lf", 5)], first.Pairs.Select(p => (p.Key, p.Value, p.Line)));
            },
            second =>
            {
                Assert.Equal("*.cs", second.Name);
                Assert.Equal(7, second.Line);
                Assert.Equal([("indent_size", "2", 8)], second.Pairs.Select(p => (p.Key, p.Value, p.Line)));
            });
    }

    [Fact]
    public void KeysAreLowercasedAndValuesKeepTheirCase()
    {
        EditorConfigPair pair = Assert.Single(EditorConfigParser.Parse("[*]\nIndent_Style = TAB\n").Sections[0].Pairs);

        Assert.Equal("indent_style", pair.Key);
        Assert.Equal("TAB", pair.Value);
    }

    [Theory]
    [InlineData("key = value", "key", "value")]
    [InlineData("  key   =   value  ", "key", "value")]
    [InlineData("\tkey\t=\tvalue\t", "key", "value")]
    [InlineData("key = a  b", "key", "a  b")]
    [InlineData("key = a=b=c", "key", "a=b=c")]
    [InlineData("key=", "key", "")]
    [InlineData("key =   ", "key", "")]
    [InlineData("a b = c", "a b", "c")]
    [InlineData("key = editorconfig ;)", "key", "editorconfig ;)")]
    [InlineData("key = value # not a comment", "key", "value # not a comment")]
    [InlineData("key = [*.cs]", "key", "[*.cs]")]
    public void APairIsSplitAtTheFirstEqualsSignAndTrimmed(string line, string key, string value)
    {
        EditorConfigPair pair = Assert.Single(EditorConfigParser.Parse("[*]\n" + line).Sections[0].Pairs);

        Assert.Equal((key, value), (pair.Key, pair.Value));
    }

    [Theory]
    [InlineData("[*.cs]", "*.cs")]
    [InlineData("   [*.cs]   ", "*.cs")]
    [InlineData("[*.{cs,vb}]", "*.{cs,vb}")]
    [InlineData("[a]b]", "a]b")]
    [InlineData("[[a]]", "[a]")]
    [InlineData("[ spaced name ]", " spaced name ")]
    [InlineData("[]", "")]
    [InlineData("[/src/**/*.cs]", "/src/**/*.cs")]
    public void ASectionNameIsEverythingBetweenTheOuterBrackets(string line, string name) =>
        Assert.Equal(name, Assert.Single(EditorConfigParser.Parse(line).Sections).Name);

    [Fact]
    public void CommentsAreWholeLinesOnly()
    {
        EditorConfigFile file = EditorConfigParser.Parse("[*]\n; one\n# two\n   ; three\n\t# four\nkey = v\n");

        Assert.Equal(["key"], file.Sections[0].Pairs.Select(p => p.Key), StringComparer.Ordinal);
        Assert.Empty(file.Problems);
    }

    [Fact]
    public void CrlfLineEndingsAndABomAreAccepted()
    {
        EditorConfigFile file = EditorConfigParser.Parse("﻿root = true\r\n[*.cs]\r\nindent_size = 2\r\n");

        Assert.True(file.IsRoot);
        Assert.Equal(("*.cs", "indent_size", "2"), (file.Sections[0].Name, file.Sections[0].Pairs[0].Key, file.Sections[0].Pairs[0].Value));
    }

    [Fact]
    public void ALoneCarriageReturnEndsALineToo()
    {
        EditorConfigFile file = EditorConfigParser.Parse("[*]\rindent_size = 2\rend_of_line = lf");

        Assert.Equal(["indent_size", "end_of_line"], file.Sections[0].Pairs.Select(p => p.Key), StringComparer.Ordinal);
    }

    [Fact]
    public void PairsBeforeTheFirstSectionHaveNoEffect()
    {
        EditorConfigFile file = EditorConfigParser.Parse("indent_size = 2\n[*]\nend_of_line = lf\n");

        Assert.Equal(["end_of_line"], file.Sections.Single().Pairs.Select(p => p.Key), StringComparer.Ordinal);
    }

    [Fact]
    public void RepeatedSectionsAndKeysAreKeptInOrder()
    {
        EditorConfigFile file = EditorConfigParser.Parse("[*]\na = 1\na = 2\n[*]\na = 3\n");

        Assert.Equal(2, file.Sections.Count);
        Assert.Equal(["1", "2"], file.Sections[0].Pairs.Select(p => p.Value), StringComparer.Ordinal);
        Assert.Equal(["3"], file.Sections[1].Pairs.Select(p => p.Value), StringComparer.Ordinal);
    }

    [Fact]
    public void LinesThatAreNothingValidAreReportedAndSkipped()
    {
        EditorConfigFile file = EditorConfigParser.Parse("[*]\ngarbage\n[unclosed\n= no key\nkey = fine\n");

        Assert.Equal(["key"], file.Sections.Single().Pairs.Select(p => p.Key), StringComparer.Ordinal);
        Assert.Equal([2, 3, 4], file.Problems.Select(p => p.Line));
        Assert.All(file.Problems, problem => Assert.False(string.IsNullOrWhiteSpace(problem.Message)));
    }

    [Fact]
    public void ALineNumberIsCountedFromOne()
    {
        EditorConfigFile file = EditorConfigParser.Parse("\n\n[*]\n\nkey = v");

        Assert.Equal(3, file.Sections[0].Line);
        Assert.Equal(5, file.Sections[0].Pairs[0].Line);
    }

    [Fact]
    public void NeverThrowsOnArbitraryText()
    {
        var random = new Random(12345);
        const string Alphabet = "[]{}*?=;# \t\r\n\\!,.-abcz019é﻿";
        for (int i = 0; i < 2000; i++)
        {
            string text = new([.. Enumerable.Range(0, random.Next(0, 80)).Select(_ => Alphabet[random.Next(Alphabet.Length)])]);

            EditorConfigFile file = EditorConfigParser.Parse(text);

            Assert.NotNull(file);
        }
    }

    [Fact]
    public void AVeryLongLineIsAccepted()
    {
        string value = new('x', 100_000);

        EditorConfigPair pair = Assert.Single(EditorConfigParser.Parse("[*]\nkey = " + value).Sections[0].Pairs);

        Assert.Equal(value, pair.Value);
    }
}
