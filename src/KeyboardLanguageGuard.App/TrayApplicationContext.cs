using System.Drawing;
using System.Windows.Forms;
using KeyboardLanguageGuard.App.Services;
using KeyboardLanguageGuard.App.UI;
using KeyboardLanguageGuard.Core.Correction;
using KeyboardLanguageGuard.Core.Dictionaries;
using KeyboardLanguageGuard.Core.Settings;
using KeyboardLanguageGuard.Core.Text;

namespace KeyboardLanguageGuard.App;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly SettingsStore _settingsStore;
    private readonly KeyboardLayoutService _layoutService = new();
    private readonly CorrectionDecisionEngine _engine = new();
    private readonly UserDictionaryStore _userDictionaryStore = new();
    private readonly TextRingBuffer _buffer = new();
    private readonly AlertService _alertService = new();
    private readonly TextCorrectionService _textCorrectionService = new();
    private readonly StartupService _startupService = new();
    private readonly KeyboardHookService _hookService;
    private readonly NotifyIcon _notifyIcon;
    private readonly SynchronizationContext _uiContext;
    private UserDictionary _userDictionary;
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
        _userDictionary = _userDictionaryStore.Load();
        WarmupSpellingIfEnabled();

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

        if (!_settings.FirstRunCompleted && !ShowFirstRunSetup())
        {
            _notifyIcon.Visible = false;
            ExitThread();
            return;
        }

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

    private bool ShowFirstRunSetup()
    {
        using SettingsForm form = new(_settings, isFirstRun: true);
        if (form.ShowDialog() != DialogResult.OK)
        {
            return false;
        }

        _settings = form.Settings;
        _settingsStore.Save(_settings);
        _startupService.SetEnabled(_settings.LaunchAtStartup);
        _buffer.Clear();
        _notifyIcon.ContextMenuStrip = BuildMenu();
        return true;
    }

    private ContextMenuStrip BuildMenu()
    {
        ContextMenuStrip menu = new();
        ToolStripMenuItem statusItem = new(GetStatusText()) { Enabled = false };

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

        menu.Items.Add(statusItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(pauseItem);
        menu.Items.Add(settingsItem);
        menu.Items.Add(testAlertItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        return menu;
    }

    private string GetStatusText()
    {
        string languages = string.Join(", ", _settings.Languages
            .Where(item => item.Enabled)
            .Select(item => item.Language.ToString()));

        if (string.IsNullOrWhiteSpace(languages))
        {
            languages = "no languages";
        }

        string mode = _settings.Mode == DetectionMode.AutoSwitch && _settings.AutoCorrectTypedText
            ? "Auto-correct after Space"
            : _settings.Mode.ToString();

        return $"Status: {mode} | {languages}";
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
            WarmupSpellingIfEnabled();
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
            // Short settle so the typed word and its trailing space have reached the focused
            // control before we correct, while staying small enough that quick typing of the
            // next word does not cancel the correction (the input-version guard).
            await Task.Delay(45).ConfigureAwait(false);
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
        _engine.LayoutContext.Clear();
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

        CorrectionDecision decision = _engine.Decide(request.Scope, request.CurrentLanguage, _settings, _userDictionary);
        if (!decision.IsCorrection)
        {
            // Record the current language as the user's intent so the context
            // can bias future detections toward it.
            _engine.LayoutContext.Record(request.CurrentLanguage);
            return;
        }

        // Record the suggested language so the context knows the user is now typing in it.
        _engine.LayoutContext.Record(decision.SuggestedLanguage);

        bool canNotify = DateTimeOffset.Now - _lastAlert >= TimeSpan.FromSeconds(3);
        if (canNotify)
        {
            _lastAlert = DateTimeOffset.Now;
            _alertService.Play(_settings);
        }

        // Only AutoSwitch mode may modify text, and only when the decision itself cleared the
        // conservative confidence/ambiguity gate (CanAutoApply already respects the per-type
        // enable flags — e.g. spelling stays off unless the user opted in).
        if (_settings.Mode == DetectionMode.AutoSwitch && decision.CanAutoApply)
        {
            _buffer.Clear();
            RunAutoFixOffUiThread(decision, request.TrailingWhitespace);
        }

        if (canNotify && _settings.ShowNotification && _settings.ShowCorrectionNotification)
        {
            ShowDecisionNotification(decision);
        }
    }

    private void RunAutoFixOffUiThread(CorrectionDecision decision, string trailingWhitespace)
    {
        Thread worker = new(() => ApplyAutoFix(decision, trailingWhitespace))
        {
            IsBackground = true,
            Name = "KeyFixAutoCorrect"
        };

        // STA so the clipboard fallback in TextCorrectionService stays valid.
        worker.SetApartmentState(ApartmentState.STA);
        worker.Start();
    }

    private void ShowDecisionNotification(CorrectionDecision decision)
    {
        bool willApply = _settings.Mode == DetectionMode.AutoSwitch && decision.CanAutoApply;
        string what = decision.Type switch
        {
            CorrectionType.LayoutCorrection => $"keyboard layout ({decision.SuggestedLanguage})",
            CorrectionType.SpellingCorrection => "spelling",
            CorrectionType.Normalization => "spelling normalization",
            CorrectionType.UserDictionaryCorrection => "your dictionary",
            _ => "text"
        };

        string title = willApply ? $"KeyFix fixed {what}" : $"KeyFix suggestion ({what})";
        string text = _settings.Mode == DetectionMode.AlertOnly
            ? "KeyFix detected a likely mistake."
            : $"Try: {decision.ReplacementText}";

        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = text.Length > 240 ? text[..240] : text;
        _notifyIcon.ShowBalloonTip(2500);
    }

    private bool ApplyAutoFix(CorrectionDecision decision, string trailingWhitespace)
    {
        int charactersToReplace = decision.CharactersToReplace + trailingWhitespace.Length;
        string textToInsert = decision.ReplacementText + trailingWhitespace;
        bool fixedText = true;

        // For layout corrections the AutoCorrectTypedText toggle decides whether we only switch
        // the layout or also rewrite the word. Spelling / normalization / user-dictionary fixes are
        // always about rewriting the word, so they always replace.
        bool shouldReplace = decision.Type != CorrectionType.LayoutCorrection || _settings.AutoCorrectTypedText;
        if (shouldReplace)
        {
            fixedText = _textCorrectionService.ReplaceTextBeforeCursor(charactersToReplace, textToInsert);
        }

        if (decision.RequiresLayoutSwitch)
        {
            _layoutService.SwitchTo(decision.SuggestedLanguage);
        }

        return fixedText;
    }

    private void WarmupSpellingIfEnabled()
    {
        if (!_settings.EnableSpellingDetection)
        {
            return;
        }

        Thread worker = new(() =>
        {
            foreach (LanguageProfile profile in _settings.Languages.Where(l => l.Enabled))
            {
                _engine.WarmupSpelling(profile.Language);
            }
        })
        {
            IsBackground = true,
            Name = "KeyFixSpellingWarmup"
        };

        worker.Start();
    }

    private readonly record struct CorrectionRequest(
        string Scope,
        string TrailingWhitespace,
        LanguageKind CurrentLanguage,
        IntPtr ForegroundWindow,
        long InputVersion);
}
