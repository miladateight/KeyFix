using System.Drawing;
using System.Windows.Forms;
using KeyboardLanguageGuard.App.Services;
using KeyboardLanguageGuard.App.UI;
using KeyboardLanguageGuard.Core;

namespace KeyboardLanguageGuard.App;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly SettingsStore _settingsStore;
    private readonly KeyboardLayoutService _layoutService = new();
    private readonly LanguageDetector _detector = new();
    private readonly TextRingBuffer _buffer = new();
    private readonly AlertService _alertService = new();
    private readonly TextCorrectionService _textCorrectionService = new();
    private readonly StartupService _startupService = new();
    private readonly KeyboardHookService _hookService;
    private readonly NotifyIcon _notifyIcon;
    private readonly SynchronizationContext _uiContext;
    private AppSettings _settings;
    private bool _paused;
    private DateTimeOffset _lastAlert = DateTimeOffset.MinValue;
    private long _inputVersion;

    public TrayApplicationContext(SettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
        _uiContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        _settings = _settingsStore.Load();
        _settings.LaunchAtStartup = _startupService.IsEnabled();
        _paused = _settings.StartPaused;

        _notifyIcon = new NotifyIcon
        {
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application,
            Text = "KeyFix",
            Visible = true,
            ContextMenuStrip = BuildMenu()
        };

        _notifyIcon.DoubleClick += (_, _) => ShowSettings();

        _hookService = new KeyboardHookService(_layoutService);
        _hookService.CharacterTyped += OnCharacterTyped;
        _hookService.BackspacePressed += OnBackspacePressed;
        _hookService.BreakKeyPressed += OnBreakKeyPressed;
        if (!_hookService.Start())
        {
            _paused = true;
            _notifyIcon.BalloonTipTitle = "KeyFix could not start protection";
            _notifyIcon.BalloonTipText = $"Windows keyboard hook failed. Error: {_hookService.LastStartError}";
            _notifyIcon.ShowBalloonTip(5000);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hookService.Dispose();
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }

        base.Dispose(disposing);
    }

    private ContextMenuStrip BuildMenu()
    {
        ContextMenuStrip menu = new();

        ToolStripMenuItem pauseItem = new(_paused ? "Resume protection" : "Pause protection");
        pauseItem.Click += (_, _) =>
        {
            _paused = !_paused;
            _buffer.Clear();
            _notifyIcon.ContextMenuStrip = BuildMenu();
        };

        ToolStripMenuItem settingsItem = new("Settings...");
        settingsItem.Click += (_, _) => ShowSettings();

        ToolStripMenuItem testAlertItem = new("Test alert");
        testAlertItem.Click += (_, _) => _alertService.Play(_settings);

        ToolStripMenuItem exitItem = new("Exit");
        exitItem.Click += (_, _) => ExitThread();

        menu.Items.Add(pauseItem);
        menu.Items.Add(settingsItem);
        menu.Items.Add(testAlertItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        return menu;
    }

    private void ShowSettings()
    {
        using SettingsForm form = new(_settings);
        if (form.ShowDialog() == DialogResult.OK)
        {
            _settings = form.Settings;
            _settingsStore.Save(_settings);
            _startupService.SetEnabled(_settings.LaunchAtStartup);
            _buffer.Clear();
        }
    }

    private void OnCharacterTyped(object? sender, char character)
    {
        _inputVersion++;
        if (_paused)
        {
            _buffer.Clear();
            return;
        }

        IntPtr foregroundWindow = _layoutService.GetForegroundWindowHandle();
        if (foregroundWindow == IntPtr.Zero || IsForegroundProcessExcluded())
        {
            _buffer.Clear();
            return;
        }

        LanguageKind? currentLanguage = _layoutService.GetCurrentLanguage();
        if (!currentLanguage.HasValue || !_settings.IsLanguageEnabled(currentLanguage.Value))
        {
            _buffer.Clear();
            return;
        }

        _buffer.Append(character);

        if (!char.IsWhiteSpace(character))
        {
            return;
        }

        string correctionScope = _buffer.CurrentCorrectionScope;
        if (string.IsNullOrWhiteSpace(correctionScope))
        {
            return;
        }

        string trailingWhitespace = _buffer.TrailingWhitespace;
        CorrectionRequest request = new(
            correctionScope,
            trailingWhitespace,
            currentLanguage.Value,
            foregroundWindow,
            _inputVersion);

        _ = Task.Run(async () =>
        {
            await Task.Delay(80).ConfigureAwait(false);
            _uiContext.Post(_ => ProcessCorrectionRequest(request), null);
        });
    }

    private void OnBackspacePressed(object? sender, EventArgs eventArgs)
    {
        _inputVersion++;
        _buffer.Backspace();
    }

    private void OnBreakKeyPressed(object? sender, EventArgs eventArgs)
    {
        _inputVersion++;
        _buffer.Clear();
    }

    private bool IsForegroundProcessExcluded()
    {
        string? processName = _layoutService.GetForegroundProcessName();
        if (string.IsNullOrWhiteSpace(processName))
        {
            return false;
        }

        return _settings.ExcludedProcesses.Any(item =>
            string.Equals(Path.GetFileNameWithoutExtension(item), processName, StringComparison.OrdinalIgnoreCase));
    }

    private void ProcessCorrectionRequest(CorrectionRequest request)
    {
        if (request.InputVersion != _inputVersion ||
            !_layoutService.IsForegroundWindow(request.ForegroundWindow) ||
            IsForegroundProcessExcluded())
        {
            return;
        }

        DetectionResult result = _detector.Detect(request.Scope, request.CurrentLanguage, _settings);
        if (!result.ShouldAlert)
        {
            return;
        }

        bool canNotify = DateTimeOffset.Now - _lastAlert >= TimeSpan.FromSeconds(3);
        if (canNotify)
        {
            _lastAlert = DateTimeOffset.Now;
            _alertService.Play(_settings);
        }

        if (_settings.Mode == DetectionMode.AutoSwitch)
        {
            bool fixedText = ApplyAutoFix(result, request.TrailingWhitespace);
            if (fixedText)
            {
                _buffer.Clear();
            }
        }

        if (canNotify && _settings.ShowNotification)
        {
            ShowDetectionNotification(result);
        }
    }

    private void ShowDetectionNotification(DetectionResult result)
    {
        string title = _settings.Mode == DetectionMode.AutoSwitch
            ? $"KeyFix switched to {result.SuggestedLanguage}"
            : $"Possible wrong layout: {result.SuggestedLanguage}";

        string text = _settings.Mode == DetectionMode.AlertOnly
            ? "KeyFix detected a likely layout mismatch."
            : $"Try: {result.SuggestedText}";

        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = text.Length > 240 ? text[..240] : text;
        _notifyIcon.ShowBalloonTip(2500);
    }

    private bool ApplyAutoFix(DetectionResult result, string trailingWhitespace)
    {
        int charactersToReplace = result.CharactersToReplace + trailingWhitespace.Length;
        string textToInsert = result.TextToInsert + trailingWhitespace;
        bool fixedText = true;

        if (_settings.AutoCorrectTypedText)
        {
            fixedText = _textCorrectionService.ReplaceTextBeforeCursor(charactersToReplace, textToInsert);
        }

        if (fixedText)
        {
            _layoutService.SwitchTo(result.SuggestedLanguage);
        }

        return fixedText;
    }

    private readonly record struct CorrectionRequest(
        string Scope,
        string TrailingWhitespace,
        LanguageKind CurrentLanguage,
        IntPtr ForegroundWindow,
        long InputVersion);
}
