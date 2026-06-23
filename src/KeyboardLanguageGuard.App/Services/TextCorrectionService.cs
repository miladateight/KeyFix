using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KeyboardLanguageGuard.App.Services;

public sealed class TextCorrectionService
{
    private const ushort VkBack = 0x08;
    private const uint InputKeyboard = 1;
    private const uint KeyEventKeyUp = 0x0002;
    private const uint KeyEventUnicode = 0x0004;

    public bool ReplaceTextBeforeCursor(int charactersToReplace, string replacement)
    {
        if (charactersToReplace <= 0 || string.IsNullOrEmpty(replacement))
        {
            return false;
        }

        try
        {
            // Build the whole correction (all backspaces + all replacement characters) as a
            // single atomic SendInput batch. Sending it in one call makes the replacement
            // effectively instantaneous, so it cannot interleave with the user's next real
            // keystrokes when they type quickly. The events stay in order in the target's
            // input queue: the backspaces delete the mistyped word, then the Unicode
            // characters insert the corrected text.
            Input[] inputs = BuildReplacementInputs(charactersToReplace, replacement);
            uint sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());

            if (sent == inputs.Length)
            {
                return true;
            }

            // Injection was rejected by the target app: fall back to clipboard paste.
            return PasteText(replacement);
        }
        catch
        {
            return false;
        }
    }

    private static Input[] BuildReplacementInputs(int backspaceCount, string replacement)
    {
        Input[] inputs = new Input[(backspaceCount * 2) + (replacement.Length * 2)];
        int offset = 0;

        for (int index = 0; index < backspaceCount; index++)
        {
            inputs[offset++] = KeyboardInput(VkBack, 0, 0);
            inputs[offset++] = KeyboardInput(VkBack, 0, KeyEventKeyUp);
        }

        foreach (char character in replacement)
        {
            ushort scanCode = unchecked((ushort)character);
            inputs[offset++] = KeyboardInput(0, scanCode, KeyEventUnicode);
            inputs[offset++] = KeyboardInput(0, scanCode, KeyEventUnicode | KeyEventKeyUp);
        }

        return inputs;
    }

    private static bool PasteText(string text)
    {
        IDataObject? previousClipboard = null;
        try
        {
            previousClipboard = Clipboard.GetDataObject();
            Clipboard.SetText(text, TextDataFormat.UnicodeText);
            bool pasted = SendChord(0x11, 0x56);

            // Wait long enough for the target app to actually consume Ctrl+V before
            // restoring the previous clipboard, otherwise the paste can race against the
            // restore and insert stale clipboard contents.
            Thread.Sleep(320);

            if (previousClipboard is not null)
            {
                Clipboard.SetDataObject(previousClipboard, true);
            }

            return pasted;
        }
        catch
        {
            try
            {
                if (previousClipboard is not null)
                {
                    Clipboard.SetDataObject(previousClipboard, true);
                }
            }
            catch
            {
                // Clipboard restore can fail if another process locks it.
            }

            return false;
        }
    }

    private static bool SendChord(ushort modifier, ushort key)
    {
        Input[] inputs =
        [
            KeyboardInput(modifier, 0, 0),
            KeyboardInput(key, 0, 0),
            KeyboardInput(key, 0, KeyEventKeyUp),
            KeyboardInput(modifier, 0, KeyEventKeyUp)
        ];

        return SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>()) == inputs.Length;
    }

    private static Input KeyboardInput(ushort virtualKey, ushort scanCode, uint flags)
    {
        return new Input
        {
            Type = InputKeyboard,
            Data = new InputUnion
            {
                Keyboard = new KeyboardInputData
                {
                    VirtualKey = virtualKey,
                    ScanCode = scanCode,
                    Flags = flags,
                    Time = 0,
                    ExtraInfo = IntPtr.Zero
                }
            }
        };
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint inputCount, Input[] inputs, int size);

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public InputUnion Data;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public KeyboardInputData Keyboard;

        // The native INPUT union must be the size of its largest member (MOUSEINPUT).
        // Including it keeps Marshal.SizeOf<Input>() equal to the real sizeof(INPUT)
        // (40 bytes on x64), so the cbSize passed to SendInput is correct. Without this
        // the struct is too small and SendInput fails with ERROR_INVALID_PARAMETER (87).
        [FieldOffset(0)]
        public MouseInputData Mouse;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInputData
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInputData
    {
        public int X;
        public int Y;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }
}
