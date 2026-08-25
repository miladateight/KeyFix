namespace KeyboardLanguageGuard.Core.Input;

/// <summary>Modifier keys a global shortcut can require, as a set.</summary>
[Flags]
public enum HotkeyModifiers
{
    None = 0,
    Control = 1,
    Shift = 2,
    Alt = 4,
    Win = 8
}

/// <summary>
/// A global shortcut parsed from the settings file, e.g. "Ctrl+Shift+Q". Settings are text the
/// user can edit by hand, so parsing is deliberately strict: a binding needs at least one
/// modifier and one supported key. Anything else is rejected rather than installed, because a
/// modifier-less shortcut would swallow an ordinary keystroke everywhere the user types.
/// </summary>
public readonly struct HotkeyDefinition : IEquatable<HotkeyDefinition>
{
    /// <summary>The shipped binding for the QR-code shortcut.</summary>
    public static HotkeyDefinition Default { get; } =
        new(HotkeyModifiers.Control | HotkeyModifiers.Shift, 0x51, "Q");

    private HotkeyDefinition(HotkeyModifiers modifiers, int virtualKey, string keyName)
    {
        Modifiers = modifiers;
        VirtualKey = virtualKey;
        KeyName = keyName;
    }

    public HotkeyModifiers Modifiers { get; }

    /// <summary>Windows virtual-key code of the non-modifier key, or 0 when unset.</summary>
    public int VirtualKey { get; }

    /// <summary>Canonical name of the non-modifier key, used when writing the binding back out.</summary>
    public string KeyName { get; }

    public bool IsValid => VirtualKey != 0 && Modifiers != HotkeyModifiers.None;

    /// <summary>Parses <paramref name="value"/>, falling back to <see cref="Default"/>.</summary>
    public static HotkeyDefinition Parse(string? value) =>
        TryParse(value, out HotkeyDefinition hotkey) ? hotkey : Default;

    public static bool TryParse(string? value, out HotkeyDefinition hotkey)
    {
        hotkey = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        HotkeyModifiers modifiers = HotkeyModifiers.None;
        int virtualKey = 0;
        string keyName = string.Empty;

        foreach (string rawPart in value.Split('+', StringSplitOptions.RemoveEmptyEntries))
        {
            string part = rawPart.Trim();
            if (part.Length == 0)
            {
                return false;
            }

            HotkeyModifiers modifier = ParseModifier(part);
            if (modifier != HotkeyModifiers.None)
            {
                // "Ctrl+Ctrl+Q" is a typo, not a shortcut.
                if (modifiers.HasFlag(modifier))
                {
                    return false;
                }

                modifiers |= modifier;
                continue;
            }

            // Only one non-modifier key is allowed: "Ctrl+Q+W" has no meaning to the hook, which
            // matches a single virtual key.
            if (virtualKey != 0 || !TryParseKey(part, out virtualKey, out keyName))
            {
                return false;
            }
        }

        if (virtualKey == 0 || modifiers == HotkeyModifiers.None)
        {
            return false;
        }

        hotkey = new HotkeyDefinition(modifiers, virtualKey, keyName);
        return true;
    }

    private static HotkeyModifiers ParseModifier(string part) => part.ToLowerInvariant() switch
    {
        "ctrl" or "control" or "ctl" => HotkeyModifiers.Control,
        "shift" => HotkeyModifiers.Shift,
        "alt" or "menu" => HotkeyModifiers.Alt,
        "win" or "windows" or "meta" or "cmd" => HotkeyModifiers.Win,
        _ => HotkeyModifiers.None
    };

    private static bool TryParseKey(string part, out int virtualKey, out string keyName)
    {
        virtualKey = 0;
        keyName = string.Empty;

        if (part.Length == 1)
        {
            char character = char.ToUpperInvariant(part[0]);
            if (character is >= 'A' and <= 'Z')
            {
                virtualKey = character;          // VK_A..VK_Z share the ASCII codes.
                keyName = character.ToString();
                return true;
            }

            if (character is >= '0' and <= '9')
            {
                virtualKey = character;          // VK_0..VK_9 likewise.
                keyName = character.ToString();
                return true;
            }

            return false;
        }

        string lowered = part.ToLowerInvariant();

        if (lowered.Length is 2 or 3 && lowered[0] == 'f' &&
            int.TryParse(lowered.AsSpan(1), out int functionKey) &&
            functionKey is >= 1 and <= 12)
        {
            virtualKey = 0x6F + functionKey;     // VK_F1 is 0x70.
            keyName = "F" + functionKey;
            return true;
        }

        (int VirtualKey, string Name)? named = lowered switch
        {
            "space" => (0x20, "Space"),
            "insert" or "ins" => (0x2D, "Insert"),
            "delete" or "del" => (0x2E, "Delete"),
            "home" => (0x24, "Home"),
            "end" => (0x23, "End"),
            "pageup" or "pgup" => (0x21, "PageUp"),
            "pagedown" or "pgdn" => (0x22, "PageDown"),
            _ => null
        };

        if (named is null)
        {
            return false;
        }

        virtualKey = named.Value.VirtualKey;
        keyName = named.Value.Name;
        return true;
    }

    /// <summary>
    /// Writes the binding back in the canonical form the settings file stores, so a hand-edited
    /// "control + q" is saved as "Ctrl+Q" and round-trips from then on.
    /// </summary>
    public override string ToString()
    {
        if (!IsValid)
        {
            return string.Empty;
        }

        List<string> parts = new(5);
        if (Modifiers.HasFlag(HotkeyModifiers.Control)) { parts.Add("Ctrl"); }
        if (Modifiers.HasFlag(HotkeyModifiers.Shift)) { parts.Add("Shift"); }
        if (Modifiers.HasFlag(HotkeyModifiers.Alt)) { parts.Add("Alt"); }
        if (Modifiers.HasFlag(HotkeyModifiers.Win)) { parts.Add("Win"); }
        parts.Add(KeyName);

        return string.Join("+", parts);
    }

    public bool Equals(HotkeyDefinition other) =>
        Modifiers == other.Modifiers && VirtualKey == other.VirtualKey;

    public override bool Equals(object? obj) => obj is HotkeyDefinition other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Modifiers, VirtualKey);

    public static bool operator ==(HotkeyDefinition left, HotkeyDefinition right) => left.Equals(right);

    public static bool operator !=(HotkeyDefinition left, HotkeyDefinition right) => !left.Equals(right);
}
