using System.Drawing;
using System.Windows.Forms;

namespace KeyboardLanguageGuard.App.UI;

/// <summary>
/// Shows a generated QR code and offers to save it. Deliberately small and modal: the code is
/// something the user photographs with a phone and then dismisses.
/// </summary>
public sealed class QrCodeForm : Form
{
    // Image.FromStream keeps using the stream for the lifetime of the image, so the stream is
    // held as a field and disposed with the form rather than in a using block.
    private readonly MemoryStream _imageStream;
    private readonly Image _image;
    private readonly byte[] _png;

    public QrCodeForm(byte[] png, string encodedText)
    {
        _png = png;
        _imageStream = new MemoryStream(png, writable: false);
        _image = Image.FromStream(_imageStream);

        Text = "KeyFix QR code";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(12);

        TableLayoutPanel root = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };

        PictureBox picture = new()
        {
            Image = _image,
            SizeMode = PictureBoxSizeMode.Zoom,
            Width = 320,
            Height = 320
        };

        Label preview = new()
        {
            Text = Summarize(encodedText),
            AutoSize = false,
            Width = 320,
            Height = 40,
            TextAlign = ContentAlignment.MiddleCenter
        };

        Button save = new() { Text = "Save as PNG", Width = 150, Height = 30 };
        save.Click += (_, _) => Save();

        Button close = new() { Text = "Close", Width = 150, Height = 30, DialogResult = DialogResult.Cancel };

        FlowLayoutPanel buttons = new()
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        buttons.Controls.Add(save);
        buttons.Controls.Add(close);

        root.Controls.Add(picture);
        root.Controls.Add(preview);
        root.Controls.Add(buttons);
        Controls.Add(root);

        CancelButton = close;
    }

    /// <summary>One line of context so the user can tell which selection was encoded.</summary>
    private static string Summarize(string text)
    {
        string single = text.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return single.Length <= 60 ? single : single[..57] + "...";
    }

    private void Save()
    {
        using SaveFileDialog dialog = new()
        {
            Title = "Save QR code",
            Filter = "PNG image (*.png)|*.png",
            FileName = "keyfix-qr.png",
            OverwritePrompt = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            File.WriteAllBytes(dialog.FileName, _png);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(
                this,
                $"Could not save the image: {exception.Message}",
                "KeyFix",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _image.Dispose();
            _imageStream.Dispose();
        }

        base.Dispose(disposing);
    }
}
