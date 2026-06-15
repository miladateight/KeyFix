using System.Drawing;
using System.Windows.Forms;
using KeyboardLanguageGuard.Core;

namespace KeyboardLanguageGuard.App.UI;

public sealed class SettingsForm : Form
{
    private readonly CheckBox _english = new() { Text = "English", AutoSize = true };
    private readonly CheckBox _persian = new() { Text = "Persian", AutoSize = true };
    private readonly CheckBox _arabic = new() { Text = "Arabic", AutoSize = true };
    private readonly CheckBox _german = new() { Text = "German", AutoSize = true };
    private readonly ComboBox _mode = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown _threshold = new() { Minimum = 4, Maximum = 40, Value = 8 };
    private readonly NumericUpDown _minimumCharacters = new() { Minimum = 3, Maximum = 30, Value = 5 };
    private readonly CheckBox _playSound = new() { Text = "Play alert sound", AutoSize = true };
    private readonly CheckBox _showNotification = new() { Text = "Show Windows tray notification", AutoSize = true };
    private readonly CheckBox _autoCorrectTypedText = new() { Text = "Correct mistyped word automatically in AutoSwitch mode", AutoSize = true };
    private readonly CheckBox _launchAtStartup = new() { Text = "Start KeyFix when Windows starts", AutoSize = true };
    private readonly TextBox _soundPath = new() { Width = 390 };
    private readonly TextBox _excludedProcesses = new() { Multiline = true, ScrollBars = ScrollBars.Vertical, Height = 90, Width = 480 };

    public SettingsForm(AppSettings settings)
    {
        Settings = Clone(settings);
        Text = "KeyFix Settings";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(590, 585);
        Font = new Font("Segoe UI", 9F);

        _mode.Items.AddRange(Enum.GetNames<DetectionMode>());

        BuildLayout();
        LoadSettings();
    }

    public AppSettings Settings { get; private set; }

    private void BuildLayout()
    {
        TableLayoutPanel root = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 1,
            RowCount = 11
        };

        root.RowStyles.Clear();
        for (int index = 0; index < 10; index++)
        {
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        GroupBox languages = new() { Text = "Enabled languages", Width = 510, Height = 70 };
        FlowLayoutPanel languagePanel = new() { Dock = DockStyle.Fill, Padding = new Padding(10) };
        languagePanel.Controls.AddRange([_english, _persian, _arabic, _german]);
        languages.Controls.Add(languagePanel);

        root.Controls.Add(languages);
        root.Controls.Add(Field("Mode", _mode));
        root.Controls.Add(Field("Detection threshold", _threshold));
        root.Controls.Add(Field("Minimum characters", _minimumCharacters));
        root.Controls.Add(_playSound);
        root.Controls.Add(_showNotification);
        root.Controls.Add(_autoCorrectTypedText);
        root.Controls.Add(_launchAtStartup);
        root.Controls.Add(SoundPicker());
        root.Controls.Add(Field("Excluded processes, one per line", _excludedProcesses));
        root.Controls.Add(Buttons());

        Controls.Add(root);
    }

    private Control SoundPicker()
    {
        FlowLayoutPanel panel = new() { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        Label label = new() { Text = "Custom WAV", Width = 130, TextAlign = ContentAlignment.MiddleLeft };
        Button browse = new() { Text = "Browse...", Width = 90 };
        browse.Click += (_, _) =>
        {
            using OpenFileDialog dialog = new()
            {
                Filter = "WAV files (*.wav)|*.wav|All files (*.*)|*.*",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _soundPath.Text = dialog.FileName;
            }
        };

        panel.Controls.Add(label);
        panel.Controls.Add(_soundPath);
        panel.Controls.Add(browse);
        return panel;
    }

    private static Control Field(string labelText, Control control)
    {
        FlowLayoutPanel panel = new() { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(0, 8, 0, 0) };
        Label label = new() { Text = labelText, Width = 170, TextAlign = ContentAlignment.MiddleLeft };
        panel.Controls.Add(label);
        panel.Controls.Add(control);
        return panel;
    }

    private Control Buttons()
    {
        FlowLayoutPanel panel = new()
        {
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Bottom,
            Padding = new Padding(0, 18, 0, 0)
        };

        Button save = new() { Text = "Save", DialogResult = DialogResult.OK, Width = 90 };
        Button cancel = new() { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 90 };
        save.Click += (_, _) => SaveSettings();
        panel.Controls.Add(save);
        panel.Controls.Add(cancel);
        AcceptButton = save;
        CancelButton = cancel;
        return panel;
    }

    private void LoadSettings()
    {
        _english.Checked = Settings.IsLanguageEnabled(LanguageKind.English);
        _persian.Checked = Settings.IsLanguageEnabled(LanguageKind.Persian);
        _arabic.Checked = Settings.IsLanguageEnabled(LanguageKind.Arabic);
        _german.Checked = Settings.IsLanguageEnabled(LanguageKind.German);
        _mode.SelectedItem = Settings.Mode.ToString();
        _threshold.Value = Settings.DetectionThreshold;
        _minimumCharacters.Value = Settings.MinimumCharacters;
        _playSound.Checked = Settings.PlaySound;
        _showNotification.Checked = Settings.ShowNotification;
        _autoCorrectTypedText.Checked = Settings.AutoCorrectTypedText;
        _launchAtStartup.Checked = Settings.LaunchAtStartup;
        _soundPath.Text = Settings.CustomSoundPath ?? string.Empty;
        _excludedProcesses.Text = string.Join(Environment.NewLine, Settings.ExcludedProcesses);
    }

    private void SaveSettings()
    {
        Settings.SettingsVersion = AppSettings.CurrentSettingsVersion;
        Settings.Mode = Enum.Parse<DetectionMode>(_mode.SelectedItem?.ToString() ?? nameof(DetectionMode.AutoSwitch));
        Settings.DetectionThreshold = (int)_threshold.Value;
        Settings.MinimumCharacters = (int)_minimumCharacters.Value;
        Settings.PlaySound = _playSound.Checked;
        Settings.ShowNotification = _showNotification.Checked;
        Settings.AutoCorrectTypedText = _autoCorrectTypedText.Checked;
        Settings.LaunchAtStartup = _launchAtStartup.Checked;
        Settings.CustomSoundPath = string.IsNullOrWhiteSpace(_soundPath.Text) ? null : _soundPath.Text.Trim();
        Settings.ExcludedProcesses = _excludedProcesses.Text
            .Split([Environment.NewLine, "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        Settings.Languages =
        [
            new() { Language = LanguageKind.English, Enabled = _english.Checked },
            new() { Language = LanguageKind.Persian, Enabled = _persian.Checked },
            new() { Language = LanguageKind.Arabic, Enabled = _arabic.Checked },
            new() { Language = LanguageKind.German, Enabled = _german.Checked }
        ];
    }

    private static AppSettings Clone(AppSettings settings)
    {
        return new AppSettings
        {
            Mode = settings.Mode,
            SettingsVersion = settings.SettingsVersion,
            DetectionThreshold = settings.DetectionThreshold,
            MinimumCharacters = settings.MinimumCharacters,
            PlaySound = settings.PlaySound,
            CustomSoundPath = settings.CustomSoundPath,
            ShowNotification = settings.ShowNotification,
            StartPaused = settings.StartPaused,
            AutoCorrectTypedText = settings.AutoCorrectTypedText,
            LaunchAtStartup = settings.LaunchAtStartup,
            Languages = settings.Languages
                .Select(item => new LanguageProfile { Language = item.Language, Enabled = item.Enabled })
                .ToList(),
            ExcludedProcesses = settings.ExcludedProcesses.ToList()
        };
    }
}
