using KeyboardLanguageGuard.Core.Settings;

namespace KeyboardLanguageGuard.Core.Layout;

/// <summary>
/// Converts text typed under one keyboard layout into the text that would result if the same physical
/// keys had been pressed under another layout. Pure function, no I/O.
/// </summary>
public interface IKeyboardLayoutTransformer
{
    /// <summary>
    /// Returns <paramref name="text"/> as it would appear under <paramref name="targetLayout"/>
    /// given that the user typed it while <paramref name="sourceLayout"/> was active.
    /// </summary>
    string Transform(string text, LanguageKind sourceLayout, LanguageKind targetLayout);
}