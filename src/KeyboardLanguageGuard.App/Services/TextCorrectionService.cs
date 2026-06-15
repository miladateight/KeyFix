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
            bool removedText = SendRepeatedKey(VkBack, charactersToReplace);
            Thread.Sleep(35);
            return removedText && (PasteText(replacement) || SendUnicodeText(replacement));
        }
        catch
        {
            return false;
        }
    }

    private static bool SendRepeatedKey(ushort virtualKey, int count)
    {
        for (int index = 0; index < count; index++)
        {
            Input[] inputs =
            [
                KeyboardInput(virtualKey, 0, 0),
                KeyboardInput(virtualKey, 0, KeyEventKeyUp)
            ];

            if (SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>()) != inputs.Length)
            {
                return false;
            }

            Thread.Sleep(6);
        }

        return true;
    }

    private static bool SendUnicodeText(string text)
    {
        Input[] inputs = new Input[text.Length * 2];
        for (int index = 0; index < text.Length; index++)
        {
            int offset = index * 2;
            ushort scanCode = unchecked((ushort)text[index]);
            inputs[offset] = KeyboardInput(0, scanCode, KeyEventUnicode);
            inputs[offset + 1] = KeyboardInput(0, scanCode, KeyEventUnicode | KeyEventKeyUp);
        }

        return SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>()) == inputs.Length;
    }

    private static bool PasteText(string text)
    {
        IDataObject? previousClipboard = null;
        try
        {
            previousClipboard = Clipboard.GetDataObject();
            Clipboard.SetText(text, TextDataFormat.UnicodeText);
            bool pasted = SendChord(0x11, 0x56);
            Thread.Sleep(180);

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
}
