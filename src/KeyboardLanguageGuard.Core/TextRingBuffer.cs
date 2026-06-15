using System.Text;

namespace KeyboardLanguageGuard.Core;

public sealed class TextRingBuffer(int capacity = 48)
{
    private readonly int _capacity = Math.Max(8, capacity);
    private readonly StringBuilder _text = new();

    public string Text => _text.ToString();

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

    public void Append(char value)
    {
        if (char.IsControl(value))
        {
            return;
        }

        _text.Append(value);
        Trim();
    }

    public void Backspace()
    {
        if (_text.Length > 0)
        {
            _text.Remove(_text.Length - 1, 1);
        }
    }

    public void Clear()
    {
        _text.Clear();
    }

    private void Trim()
    {
        if (_text.Length <= _capacity)
        {
            return;
        }

        _text.Remove(0, _text.Length - _capacity);
    }
}
