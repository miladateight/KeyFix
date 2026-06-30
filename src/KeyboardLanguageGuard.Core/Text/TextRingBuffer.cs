using System.Text;

namespace KeyboardLanguageGuard.Core.Text;

/// <summary>
/// A short rolling buffer of typed characters. Used by the tray app to keep the most recent word
/// (and its trailing whitespace) so the detector can decide what to rewrite after Space.
/// </summary>
public sealed class TextRingBuffer
{
    private readonly int _capacity;
    private readonly StringBuilder _text;

    public TextRingBuffer(int capacity = 48)
    {
        _capacity = Math.Max(8, capacity);
        _text = new StringBuilder(_capacity);
    }

    /// <summary>The full text currently held by the buffer.</summary>
    public string Text => _text.ToString();

    /// <summary>
    /// The previous word (the text between the last whitespace boundary and the cursor), trimmed
    /// of any trailing whitespace. Empty when the buffer only holds whitespace.
    /// </summary>
    public string CurrentCorrectionScope
    {
        get
        {
            string value = _text.ToString();
            int end = value.Length;
            while (end > 0 && char.IsWhiteSpace(value[end - 1]))
            {
                end--;
            }

            if (end == 0)
            {
                return string.Empty;
            }

            int start = end;
            while (start > 0 && !char.IsWhiteSpace(value[start - 1]))
            {
                start--;
            }

            return value[start..end];
        }
    }

    /// <summary>Any whitespace characters that follow the current correction scope.</summary>
    public string TrailingWhitespace
    {
        get
        {
            string value = _text.ToString();
            int start = value.Length;
            while (start > 0 && char.IsWhiteSpace(value[start - 1]))
            {
                start--;
            }

            return value[start..];
        }
    }

    /// <summary>Append a typed character. Control characters are ignored.</summary>
    public void Append(char value)
    {
        if (char.IsControl(value))
        {
            return;
        }

        _text.Append(value);
        Trim();
    }

    /// <summary>Removes the most recently typed character (Backspace).</summary>
    public void Backspace()
    {
        if (_text.Length > 0)
        {
            _text.Remove(_text.Length - 1, 1);
        }
    }

    /// <summary>Removes everything from the buffer.</summary>
    public void Clear() => _text.Clear();

    private void Trim()
    {
        if (_text.Length <= _capacity)
        {
            return;
        }

        _text.Remove(0, _text.Length - _capacity);
    }
}