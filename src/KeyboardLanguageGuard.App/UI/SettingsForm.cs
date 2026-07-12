using System.Drawing;
using System.Windows.Forms;
using KeyboardLanguageGuard.Core.Settings;

namespace KeyboardLanguageGuard.App.UI;

public sealed class SettingsForm : Form
{
    private readonly CheckBox _english = new() { Text = "English", AutoSize = true };
    private readonly CheckBox _persian = new() { Text = "Persian", AutoSize = true };
    private readonly CheckBox _arabic = new() { Text = "Arabic", AutoSize = true };
    private readonly CheckBox _german = new() { Text = "German", AutoSize = true };
    private readonly ComboBox _mode = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox _playSound = new() { Text = "Play alert sound", AutoSize = true };
    private readonly CheckBox _showNotification = new() { Text = "Show Windows tray notification", AutoSize = true };
    private readonly CheckBox _autoCorrectTypedText = new() { Text = "Correct mistyped word automatically in AutoSwitch mode", AutoSize = true };
    private readonly CheckBox _enableWrongLayoutDetection = new() { Text = "Fix typing done with the wrong keyboard language", AutoSize = true };
    private readonly CheckBox _enableSpellingDetection = new() { Text = "Fix ordinary spelling mistakes (same language)", AutoSize = true };
    private readonly CheckBox _enableSpellingAutoCorrection = new() { Text = "Apply spelling fixes automatically (otherwise only suggest)", AutoSize = true };
    private readonly CheckBox _enableNormalizationSuggestions = new() { Text = "Suggest letter/half-space normalization (e.g. Arabic → Persian ی/ک, میخوام → می‌خوام)", AutoSize = true };
    private readonly CheckBox _enableUndo = new() { Text = "Undo an automatic correction by pressing Backspace right after it", AutoSize = true };
    private readonly CheckBox _enablePersonalLearning = new() { Text = "Learn from my accepted and undone corrections (stored locally)", AutoSize = true };
    private readonly CheckBox _enableDiagnosticLogging = new() { Text = "Write local diagnostic logs (metadata only, never your text)", AutoSize = true };
    private readonly ComboBox _aggressiveness = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _persianStyle = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox _launchAtStartup = new() { Text = "Start KeyFix when Windows starts", AutoSize = true };
    private readonly TextBox _soundPath = new() { Width = 390 };
    private readonly TextBox _excludedProcesses = new() { Multiline = true, ScrollBars = ScrollBars.Vertical, Height = 90, Width = 480 };
    private readonly bool _isFirstRun;

    public SettingsForm(AppSettings settings, bool isFirstRun = false)
    {
        _isFirstRun = isFirstRun;
        Settings = Clone(settings);
        Text = isFirstRun ? "KeyFix First-Run Setup" : "KeyFix Settings";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(590, 880);
        Font = new Font("Segoe UI", 9F);

        _mode.Items.AddRange(Enum.GetNames<DetectionMode>());
        _aggressiveness.Items.AddRange(Enum.GetNames<CorrectionAggressiveness>());
        _persianStyle.Items.AddRange(Enum.GetNames<PersianCorrectionStyle>());

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
            RowCount = 12
        };

        root.RowStyles.Clear();
        for (int index = 0; index < 12; index++)
        {
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        Label intro = new()
        {
            AutoSize = true,
            MaximumSize = new Size(535, 0),
            Text = _isFirstRun
                ? "Choose only the keyboard languages you actually use, then review the rest of the settings before KeyFix starts."
                : "Choose only the keyboard languages you actually use. Disable unused languages for better accuracy."
        };

        GroupBox languages = new() { Text = "Enabled languages", Width = 510, Height = 70 };
        FlowLayoutPanel languagePanel = new() { Dock = DockStyle.Fill, Padding = new Padding(10) };
        languagePanel.Controls.AddRange([_english, _persian, _arabic, _german]);
        languages.Controls.Add(languagePanel);

        GroupBox corrections = new() { Text = "Corrections", Width = 535, Height = 175, AutoSize = true };
        FlowLayoutPanel correctionsPanel = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(10), AutoSize = true };
        correctionsPanel.Controls.AddRange([
            _enableWrongLayoutDetection,
            _enableSpellingDetection,
            _enableSpellingAutoCorrection,
            _enableNormalizationSuggestions,
            _enableUndo,
            _enablePersonalLearning,
            _enableDiagnosticLogging
        ]);
        correctionsPanel.Controls.Add(Field("How eager", _aggressiveness));
        correctionsPanel.Controls.Add(Field("Persian style", _persianStyle));
        corrections.Controls.Add(correctionsPanel);

        root.Controls.Add(intro);
        root.Controls.Add(languages);
        root.Controls.Add(Field("Mode", _mode));
        root.Controls.Add(corrections);
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

        Button save = new() { Text = _isFirstRun ? "Start KeyFix" : "Save", DialogResult = DialogResult.None, Width = 110 };
        Button cancel = new() { Text = _isFirstRun ? "Exit" : "Cancel", DialogResult = DialogResult.Cancel, Width = 90 };
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
        _aggressiveness.SelectedItem = Settings.CorrectionAggressiveness.ToString();
        _playSound.Checked = Settings.PlaySound;
        _showNotification.Checked = Settings.ShowNotification;
        _autoCorrectTypedText.Checked = Settings.AutoCorrectTypedText;
        _enableWrongLayoutDetection.Checked = Settings.EnableWrongLayoutDetection;
        _enableSpellingDetection.Checked = Settings.EnableSpellingDetection;
        _enableSpellingAutoCorrection.Checked = Settings.EnableSpellingAutoCorrection;
        _enableNormalizationSuggestions.Checked = Settings.EnableNormalizationSuggestions;
        _enableUndo.Checked = Settings.EnableUndo;
        _enablePersonalLearning.Checked = Settings.EnablePersonalLearning;
        _enableDiagnosticLogging.Checked = Settings.EnableDiagnosticLogging;
        _persianStyle.SelectedItem = Settings.PersianCorrectionStyle.ToString();
        _launchAtStartup.Checked = Settings.LaunchAtStartup;
        _soundPath.Text = Settings.CustomSoundPath ?? string.Empty;
        _excludedProcesses.Text = string.Join(Environment.NewLine, Settings.ExcludedProcesses);
    }

    private void SaveSettings()
    {
        if (!_english.Checked && !_persian.Checked && !_arabic.Checked && !_german.Checked)
        {
            MessageBox.Show(
                this,
                "Enable at least one keyboard language before starting KeyFix.",
                "KeyFix",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        Settings.SettingsVersion = AppSettings.CurrentSettingsVersion;
        Settings.FirstRunCompleted = true;
        Settings.Mode = Enum.Parse<DetectionMode>(_mode.SelectedItem?.ToString() ?? nameof(DetectionMode.AutoSwitch));
        Settings.CorrectionAggressiveness = Enum.Parse<CorrectionAggressiveness>(
            _aggressiveness.SelectedItem?.ToString() ?? nameof(CorrectionAggressiveness.Conservative));
        Settings.PlaySound = _playSound.Checked;
        Settings.ShowNotification = _showNotification.Checked;
        Settings.AutoCorrectTypedText = _autoCorrectTypedText.Checked;
        Settings.EnableWrongLayoutDetection = _enableWrongLayoutDetection.Checked;
        Settings.EnableSpellingDetection = _enableSpellingDetection.Checked;
        Settings.EnableSpellingAutoCorrection = _enableSpellingAutoCorrection.Checked;
        Settings.EnableNormalizationSuggestions = _enableNormalizationSuggestions.Checked;
        Settings.EnableUndo = _enableUndo.Checked;
        Settings.EnablePersonalLearning = _enablePersonalLearning.Checked;
        Settings.EnableDiagnosticLogging = _enableDiagnosticLogging.Checked;
        Settings.PersianCorrectionStyle = Enum.Parse<PersianCorrectionStyle>(
            _persianStyle.SelectedItem?.ToString() ?? nameof(PersianCorrectionStyle.PreserveUserStyle));
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

        DialogResult = DialogResult.OK;
        Close();
    }

    private static AppSettings Clone(AppSettings settings)
    {
        return new AppSettings
        {
            Mode = settings.Mode,
            SettingsVersion = settings.SettingsVersion,
            FirstRunCompleted = settings.FirstRunCompleted,
            DetectionThreshold = settings.DetectionThreshold,
            MinimumCharacters = settings.MinimumCharacters,
            PlaySound = settings.PlaySound,
            CustomSoundPath = settings.CustomSoundPath,
            ShowNotification = settings.ShowNotification,
            StartPaused = settings.StartPaused,
            AutoCorrectTypedText = settings.AutoCorrectTypedText,
            LaunchAtStartup = settings.LaunchAtStartup,
            EnableWrongLayoutDetection = settings.EnableWrongLayoutDetection,
            EnableWrongLayoutAutoCorrection = settings.EnableWrongLayoutAutoCorrection,
            EnableSpellingDetection = settings.EnableSpellingDetection,
            EnableSpellingAutoCorrection = settings.EnableSpellingAutoCorrection,
            EnableNormalizationSuggestions = settings.EnableNormalizationSuggestions,
            EnablePersonalLearning = settings.EnablePersonalLearning,
            EnableUndo = settings.EnableUndo,
            EnableDiagnosticLogging = settings.EnableDiagnosticLogging,
            CorrectionAggressiveness = settings.CorrectionAggressiveness,
            PersianCorrectionStyle = settings.PersianCorrectionStyle,
            ShowCorrectionNotification = settings.ShowCorrectionNotification,
            Languages = settings.Languages
                .Select(item => new LanguageProfile { Language = item.Language, Enabled = item.Enabled })
                .ToList(),
            ExcludedProcesses = settings.ExcludedProcesses.ToList()
        };
    }
}
