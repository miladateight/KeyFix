namespace KeyboardLanguageGuard.Core.Settings;

/// <summary>
/// How KeyFix reacts when it thinks you typed under the wrong keyboard layout.
/// </summary>
public enum DetectionMode
{
    /// <summary>Play the alert sound and show a tray notification. Never change layout or text.</summary>
    AlertOnly = 0,

    /// <summary>Alert and suggest the better layout in the notification. Never change layout or text.</summary>
    AlertAndSuggest = 1,

    /// <summary>Correct the mistyped word after Space and switch the input language.</summary>
    AutoSwitch = 2
}