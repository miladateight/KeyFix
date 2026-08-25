using System.Runtime.InteropServices;
using System.Windows.Forms;
using QRCoder;

namespace KeyboardLanguageGuard.App.Services;

/// <summary>
/// Turns whatever the user has selected in the focused application into a QR-code image.
///
/// There is no supported way to read another application's selection directly, so the selection
/// is copied to the clipboard, read, and the previous clipboard contents put back. Everything
/// stays on this machine: the text is only ever rendered into a local bitmap.
/// </summary>
public sealed class QrCodeService
{
    // A version-40 symbol at ECC level M holds fewer bytes than this, but the generator is the
    // authority: this is only a cheap pre-check so a whole document does not have to be encoded
    // before being rejected.
    private const int MaxPayloadCharacters = 2000;

    private const ushort VkControl = 0x11;
    private const ushort VkShift = 0x10;
    private const ushort VkMenu = 0x12;
    private const ushort VkLeftWin = 0x5B;
    private const ushort VkRightWin = 0x5C;
    private const ushort VkC = 0x43;

    private const uint InputKeyboard = 1;
    private const uint KeyEventKeyUp = 0x0002;

    /// <summary>
    /// Copies the current selection and returns it, leaving the clipboard as it found it.
    /// Returns null when nothing readable was selected. Must be called on an STA thread.
    /// </summary>
    public string? CaptureSelectedText()
    {
        // The shortcut that got us here is still physically held. Synthesizing Ctrl+C on top of
        // held Ctrl+Shift produces Ctrl+Shift+C — which is developer tools in a browser, not a
        // copy — so the held modifiers have to come up first.
        ReleaseHeldModifiers();

        IDataObject? previousClipboard = TryGetClipboard();
        try
        {
            Clipboard.Clear();

            if (!SendChord(VkControl, VkC))
            {
                return null;
            }

            // Give the focused application time to service the copy before reading.
            Thread.Sleep(220);

            return Clipboard.ContainsText() ? Clipboard.GetText() : null;
        }
        catch (ExternalException)
        {
            // Another process was holding the clipboard open.
            return null;
        }
        finally
        {
            RestoreClipboard(previousClipboard);
        }
    }

    /// <summary>
    /// Renders <paramref name="text"/> as a PNG. Returns false with a message the caller can show
    /// rather than throwing: this runs on a background thread, where an escaping exception would
    /// take the whole tray application down.
    /// </summary>
    public bool TryGeneratePng(string text, out byte[] png, out string? error)
    {
        png = Array.Empty<byte>();
        error = null;

        if (string.IsNullOrWhiteSpace(text))
        {
            error = "There was nothing to encode.";
            return false;
        }

        if (text.Length > MaxPayloadCharacters)
        {
            error = $"The selection is too long for a QR code ({text.Length} characters; the limit is {MaxPayloadCharacters}).";
            return false;
        }

        try
        {
            using QRCodeGenerator generator = new();
            using QRCodeData data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.M);
            PngByteQRCode code = new(data);
            png = code.GetGraphic(10);
            return true;
        }
        catch (Exception exception)
        {
            error = $"Could not generate the QR code: {exception.Message}";
            return false;
        }
    }

    private static IDataObject? TryGetClipboard()
    {
        try
        {
            return Clipboard.GetDataObject();
        }
        catch (ExternalException)
        {
            return null;
        }
    }

    private static void RestoreClipboard(IDataObject? previousClipboard)
    {
        if (previousClipboard is null)
        {
            return;
        }

        try
        {
            Clipboard.SetDataObject(previousClipboard, true);
        }
        catch (ExternalException)
        {
            // Losing the restore is bad but recoverable; crashing the app over it is not.
        }
    }

    private static void ReleaseHeldModifiers()
    {
        ushort[] modifiers = [VkControl, VkShift, VkMenu, VkLeftWin, VkRightWin];
        Input[] inputs = new Input[modifiers.Length];
        for (int index = 0; index < modifiers.Length; index++)
        {
            inputs[index] = KeyboardInput(modifiers[index], KeyEventKeyUp);
        }

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
        Thread.Sleep(40);
    }

    private static bool SendChord(ushort modifier, ushort key)
    {
        Input[] inputs =
        [
            KeyboardInput(modifier, 0),
            KeyboardInput(key, 0),
            KeyboardInput(key, KeyEventKeyUp),
            KeyboardInput(modifier, KeyEventKeyUp)
        ];

        return SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>()) == inputs.Length;
    }

    private static Input KeyboardInput(ushort virtualKey, uint flags)
    {
        return new Input
        {
            Type = InputKeyboard,
            Data = new InputUnion
            {
                Keyboard = new KeyboardInputData
                {
                    VirtualKey = virtualKey,
                    ScanCode = 0,
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

        // The native INPUT union is the size of its largest member, so MOUSEINPUT has to be here
        // for Marshal.SizeOf<Input>() to match the real sizeof(INPUT). See TextCorrectionService.
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
