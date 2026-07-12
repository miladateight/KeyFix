using KeyboardLanguageGuard.Core.Text;
using Xunit;

namespace KeyboardLanguageGuard.Tests;

public sealed class TokenClassifierTests
{
    [Theory]
    [InlineData("hello", TokenKind.Word)]
    [InlineData("سلام", TokenKind.Word)]
    [InlineData("Straße", TokenKind.Word)]
    [InlineData("e-mail", TokenKind.Word)]
    public void Plain_Words_Are_Words(string token, TokenKind expected) =>
        Assert.Equal(expected, TokenClassifier.Classify(token));

    [Theory]
    [InlineData("https://github.com/miladateight/KeyFix", TokenKind.Url)]
    [InlineData("www.example.com", TokenKind.Url)]
    [InlineData("github.com", TokenKind.DomainName)]
    [InlineData("ateight088@gmail.com", TokenKind.Email)]
    [InlineData("C:\\Users\\Milad\\file.txt", TokenKind.FilePath)]
    [InlineData("src/Core/File.cs", TokenKind.FilePath)]
    [InlineData("--configuration", TokenKind.CommandFlag)]
    [InlineData("-v", TokenKind.CommandFlag)]
    [InlineData("v0.5.0", TokenKind.Version)]
    [InlineData("1.2.3", TokenKind.Version)]
    [InlineData("#keyfix", TokenKind.Hashtag)]
    [InlineData("@milad", TokenKind.Mention)]
    [InlineData("camelCase", TokenKind.Identifier)]
    [InlineData("PascalCase", TokenKind.Identifier)]
    [InlineData("snake_case", TokenKind.Identifier)]
    [InlineData("SCREAMING_SNAKE", TokenKind.Identifier)]
    [InlineData("API", TokenKind.Acronym)]
    [InlineData("HTTP", TokenKind.Acronym)]
    [InlineData("abc123", TokenKind.MixedAlphanumeric)]
    [InlineData("1024", TokenKind.Number)]
    [InlineData("3.14", TokenKind.Number)]
    public void Structured_Tokens_Are_Classified(string token, TokenKind expected) =>
        Assert.Equal(expected, TokenClassifier.Classify(token));

    [Theory]
    [InlineData("https://github.com")]
    [InlineData("ateight088@gmail.com")]
    [InlineData("C:\\Windows")]
    [InlineData("--release")]
    [InlineData("v1.0.0")]
    [InlineData("#tag")]
    [InlineData("@user")]
    [InlineData("myVariableName")]
    [InlineData("MAX_SIZE")]
    [InlineData("user123")]
    public void Structured_Tokens_Are_Protected(string token) =>
        Assert.True(TokenClassifier.IsProtected(token));

    [Theory]
    [InlineData("hello")]
    [InlineData("سلام")]
    [InlineData("world")]
    public void Plain_Words_Are_Not_Protected(string token) =>
        Assert.False(TokenClassifier.IsProtected(token));

    [Fact]
    public void Empty_Is_Empty_And_Protected()
    {
        Assert.Equal(TokenKind.Empty, TokenClassifier.Classify("   "));
        Assert.True(TokenClassifier.IsProtected(TokenKind.Empty));
    }
}
