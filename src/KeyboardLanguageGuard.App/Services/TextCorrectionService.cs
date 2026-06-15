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
            if (!SendRepeatedKey(VkBack, charactersToReplace))
            {
                return false;
            }

            Thread.Sleep(20);
            return SendUnicodeText(replacement) || PasteText(replacement);
        }
        catch
        {
            return false;
        }
    }

    private static bool SendRepeatedKey(ushort virtualKey, int count)
    {
        Input[] inputs = new Input[count * 2];
        for (int index = 0; index < count; index++)
        {
            int offset = index * 2;
            inputs[offset] = KeyboardInput(virtualKey, 0, 0);
            inputs[offset + 1] = KeyboardInput(virtualKey, 0, KeyEventKeyUp);
        }

        return SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>()) == inputs.Length;
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
            SendChord(0x11, 0x56);
            Thread.Sleep(120);

            if (previousClipboard is not null)
            {
                Clipboard.SetDataObject(previousClipboard, true);
            }

            return true;
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

    private static void SendChord(ushort modifier, ushort key)
    {
        Input[] inputs =
        [
            KeyboardInput(modifier, 0, 0),
            KeyboardInput(key, 0, 0),
            KeyboardInput(key, 0, KeyEventKeyUp),
            KeyboardInput(modifier, 0, KeyEventKeyUp)
        ];

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
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
