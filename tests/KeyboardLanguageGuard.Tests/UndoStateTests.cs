using KeyboardLanguageGuard.Core.Correction;
using KeyboardLanguageGuard.Core.Settings;
using KeyboardLanguageGuard.Core.Text;
using Xunit;

namespace KeyboardLanguageGuard.Tests;

public sealed class UndoStateTests
{
    private static UndoState Make(CorrectionType type = CorrectionType.SpellingCorrection) => new()
    {
        Type = type,
        OriginalToken = "teh",
        ReplacementToken = "the",
        TrailingWhitespace = " ",
        OriginalLanguage = LanguageKind.English,
        TargetLanguage = type == CorrectionType.LayoutCorrection ? LanguageKind.Persian : LanguageKind.English,
        ForegroundWindow = 1234,
        InputVersion = 42,
        CreatedUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    [Fact]
    public void Computes_Delete_Count_And_Restore_Text()
    {
        UndoState undo = Make();
        Assert.Equal("the".Length + 1, undo.CharactersToDelete); // replacement + trailing space
        Assert.Equal("teh ", undo.RestoreText);
    }

    [Fact]
    public void Valid_Within_Window_Version_And_Ttl()
    {
        UndoState undo = Make();
        DateTime now = undo.CreatedUtc.AddSeconds(2);
        Assert.True(undo.IsValid(1234, 42, now, TimeSpan.FromSeconds(6)));
    }

    [Theory]
    [InlineData(9999, 42)]  // different window
    [InlineData(1234, 43)]  // different input version (later typing)
    public void Invalid_When_Context_Changes(long window, long version)
    {
        UndoState undo = Make();
        DateTime now = undo.CreatedUtc.AddSeconds(1);
        Assert.False(undo.IsValid(window, version, now, TimeSpan.FromSeconds(6)));
    }

    [Fact]
    public void Invalid_When_Expired()
    {
        UndoState undo = Make();
        DateTime now = undo.CreatedUtc.AddSeconds(10);
        Assert.False(undo.IsValid(1234, 42, now, TimeSpan.FromSeconds(6)));
    }

    [Fact]
    public void Layout_Undo_Restores_Layout_Spelling_Does_Not()
    {
        Assert.True(Make(CorrectionType.LayoutCorrection).RestoresLayout);
        Assert.False(Make(CorrectionType.SpellingCorrection).RestoresLayout);
    }
}

public sealed class PreviousTokenTests
{
    [Fact]
    public void Previous_Token_Is_The_Word_Before_The_Current_Scope()
    {
        var buffer = new TextRingBuffer();
        foreach (char c in "read teh ") buffer.Append(c);
        Assert.Equal("teh", buffer.CurrentCorrectionScope);
        Assert.Equal("read", buffer.PreviousToken);
    }

    [Fact]
    public void Previous_Token_Empty_When_Only_One_Word()
    {
        var buffer = new TextRingBuffer();
        foreach (char c in "teh ") buffer.Append(c);
        Assert.Equal(string.Empty, buffer.PreviousToken);
    }
}
