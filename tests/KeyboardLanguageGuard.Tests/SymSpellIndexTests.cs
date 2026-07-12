using KeyboardLanguageGuard.Core.Spelling;
using Xunit;

namespace KeyboardLanguageGuard.Tests;

public sealed class EditDistanceTests
{
    [Theory]
    [InlineData("the", "the", 0)]
    [InlineData("teh", "the", 1)]      // adjacent transposition
    [InlineData("recieve", "receive", 1)] // transposition
    [InlineData("wierd", "weird", 1)]  // transposition
    [InlineData("cat", "cot", 1)]      // substitution
    [InlineData("cat", "cats", 1)]     // insertion
    [InlineData("cats", "cat", 1)]     // deletion
    [InlineData("kitten", "sitting", 3)]
    public void Computes_Osa_Distance(string a, string b, int expected) =>
        Assert.Equal(expected, EditDistance.Damerau(a, b));

    [Fact]
    public void Respects_Upper_Bound()
    {
        int d = EditDistance.Damerau("kitten", "sitting", max: 2);
        Assert.True(d > 2);
    }
}

public sealed class SymSpellIndexTests
{
    private static readonly string[] Words =
    [
        "the", "receive", "weird", "hello", "world", "friend", "believe", "because", "definitely"
    ];

    private readonly SymSpellIndex _index = new(Words, maxEdit: 2);

    [Fact]
    public void Finds_Exact_Term_At_Distance_Zero()
    {
        var result = _index.Lookup("hello");
        Assert.Equal("hello", result[0].Term);
        Assert.Equal(0, result[0].Distance);
    }

    [Theory]
    [InlineData("teh", "the")]
    [InlineData("recieve", "receive")]
    [InlineData("wierd", "weird")]
    [InlineData("freind", "friend")]
    [InlineData("beleive", "believe")]
    [InlineData("definately", "definitely")]
    public void Finds_Correction_For_Common_Typo(string typo, string expected)
    {
        var result = _index.Lookup(typo);
        Assert.Contains(result, s => s.Term == expected);
    }

    [Fact]
    public void Returns_Nothing_For_Distant_Input()
    {
        var result = _index.Lookup("xqzptvw");
        Assert.Empty(result);
    }

    [Fact]
    public void Suggestions_Are_Ordered_By_Distance()
    {
        var result = _index.Lookup("helo");
        Assert.Equal("hello", result[0].Term);
    }

    [Fact]
    public void Term_Count_Matches_Distinct_Words()
    {
        Assert.Equal(Words.Length, _index.TermCount);
    }
}
