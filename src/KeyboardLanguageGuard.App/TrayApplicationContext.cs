using System.Drawing;
using System.Windows.Forms;
using KeyboardLanguageGuard.App.Services;
using KeyboardLanguageGuard.App.UI;
using KeyboardLanguageGuard.Core.Correction;
using KeyboardLanguageGuard.Core.Diagnostics;
using KeyboardLanguageGuard.Core.Dictionaries;
using KeyboardLanguageGuard.Core.Learning;
using KeyboardLanguageGuard.Core.Settings;
using KeyboardLanguageGuard.Core.Text;

namespace KeyboardLanguageGuard.App;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly SettingsStore _settingsStore;
    private readonly KeyboardLayoutService _layoutService = new();
    private readonly CorrectionDecisionEngine _engine = new();
    private readonly UserDictionaryStore _userDictionaryStore = new();
    private readonly CorrectionMemoryStore _memoryStore = new();
    private readonly FileDiagnosticLog _diagnosticLog;
    private readonly TextRingBuffer _buffer = new();
    private readonly AlertService _alertService = new();
    private readonly TextCorrectionService _textCorrectionService = new();
    private readonly StartupService _startupService = new();
    private readonly KeyboardHookService _hookService;
    private readonly NotifyIcon _notifyIcon;
    private readonly SynchronizationContext _uiContext;
    private UserDictionary _userDictionary;
    private CorrectionMemory _memory = new();
    private bool _memoryDirty;
    private AppSettings _settings;
    private bool _paused;
    private DateTimeOffset _lastAlert = DateTimeOffset.MinValue;
    private long _inputVersion;
    private UndoState? _pendingUndo;

    public TrayApplicationContext(SettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
        _uiContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        _settings = _settingsStore.Load();
        _settings.LaunchAtStartup = _startupService.IsEnabled();
        _paused = _settings.StartPaused;
        _userDictionary = _userDictionaryStore.Load();
        _memory = _memoryStore.Load();
        _engine.SetMemory(_settings.EnablePersonalLearning ? _memory : NullCorrectionMemory.Instance);
        _diagnosticLog = new FileDiagnosticLog(_settings.EnableDiagnosticLogging);
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
        _hookService.UndoRequested += OnUndoRequested;
        _hookService.BackspaceShouldUndo = ShouldUndoBackspace;

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
            if (_memoryDirty)
            {
                _memoryStore.Save(_memory);
                _memoryDirty = false;
            }

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
            _pendingUndo = null;
            _diagnosticLog.SetEnabled(_settings.EnableDiagnosticLogging);
            _engine.SetMemory(_settings.EnablePersonalLearning ? _memory : NullCorrectionMemory.Instance);
            if (_memoryDirty)
            {
                _memoryStore.Save(_memory);
                _memoryDirty = false;
            }

            WarmupSpellingIfEnabled();
        }
    }

    private void OnCharacterTyped(object? sender, char character)
    {
        _inputVersion++;
        _pendingUndo = null; // any real typing ends the undo window
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
            _buffer.PreviousToken,
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
        _pendingUndo = null; // a non-undo Backspace also ends the undo window
        _buffer.Backspace();
    }

    private void OnBreakKeyPressed(object? sender, EventArgs eventArgs)
    {
        _inputVersion++;
        _pendingUndo = null;
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

        long startTicks = System.Diagnostics.Stopwatch.GetTimestamp();
        CorrectionDecision decision = _engine.Decide(request.Scope, request.CurrentLanguage, _settings, _userDictionary, request.PreviousToken);
        double durationMs = System.Diagnostics.Stopwatch.GetElapsedTime(startTicks).TotalMilliseconds;
        LogDecision(request, decision, durationMs);

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
            RememberForUndo(decision, request);
            RecordLearningAccepted(decision);
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

    private void LogDecision(CorrectionRequest request, CorrectionDecision decision, double durationMs)
    {
        if (!_diagnosticLog.IsEnabled)
        {
            return;
        }

        string script = Scripts.IsLatinLanguage(request.CurrentLanguage) ? "Latin" : "Arabic";
        _diagnosticLog.Write(new DiagnosticEvent(
            DateTime.UtcNow,
            _layoutService.GetForegroundProcessName() ?? string.Empty,
            request.Scope.Length,
            script,
            request.CurrentLanguage,
            decision.IsCorrection ? 1 : 0,
            decision.Type,
            decision.Reason,
            DiagnosticEvent.BucketFor(decision.Confidence),
            DiagnosticEvent.BucketForMargin(decision.AmbiguityMargin),
            durationMs));
    }

    private static readonly TimeSpan UndoTimeToLive = TimeSpan.FromSeconds(6);

    private void RememberForUndo(CorrectionDecision decision, CorrectionRequest request)
    {
        if (!_settings.EnableUndo)
        {
            _pendingUndo = null;
            return;
        }

        _pendingUndo = new UndoState
        {
            Type = decision.Type,
            OriginalToken = decision.ObservedText,
            ReplacementToken = decision.ReplacementText,
            TrailingWhitespace = request.TrailingWhitespace,
            OriginalLanguage = request.CurrentLanguage,
            TargetLanguage = decision.SuggestedLanguage,
            ForegroundWindow = request.ForegroundWindow.ToInt64(),
            InputVersion = _inputVersion,
            CreatedUtc = DateTime.UtcNow
        };
    }

    private void RecordLearningAccepted(CorrectionDecision decision)
    {
        if (!_settings.EnablePersonalLearning || decision.Type == CorrectionType.LayoutCorrection)
        {
            return;
        }

        _memory.RecordAccepted(decision.SuggestedLanguage, decision.ObservedText, decision.ReplacementText);
        _memoryDirty = true;
    }

    private bool ShouldUndoBackspace()
    {
        UndoState? undo = _pendingUndo;
        if (undo is null || !_settings.EnableUndo)
        {
            return false;
        }

        long foreground = _layoutService.GetForegroundWindowHandle().ToInt64();
        return undo.IsValid(foreground, _inputVersion, DateTime.UtcNow, UndoTimeToLive);
    }

    private void OnUndoRequested(object? sender, EventArgs eventArgs)
    {
        UndoState? undo = _pendingUndo;
        _pendingUndo = null;
        if (undo is null)
        {
            return;
        }

        // Any real input after this point starts a fresh context.
        _inputVersion++;
        _buffer.Clear();

        // The user reversing an automatic correction is a rejection signal for learning.
        if (_settings.EnablePersonalLearning && undo.Type != CorrectionType.LayoutCorrection)
        {
            _memory.RecordRejected(undo.TargetLanguage, undo.OriginalToken, undo.ReplacementToken);
            _memoryDirty = true;
        }

        Thread worker = new(() => ApplyUndo(undo))
        {
            IsBackground = true,
            Name = "KeyFixUndo"
        };
        worker.SetApartmentState(ApartmentState.STA);
        worker.Start();
    }

    private void ApplyUndo(UndoState undo)
    {
        _textCorrectionService.ReplaceTextBeforeCursor(undo.CharactersToDelete, undo.RestoreText);
        if (undo.RestoresLayout)
        {
            _layoutService.SwitchTo(undo.OriginalLanguage);
        }
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
        string? PreviousToken,
        LanguageKind CurrentLanguage,
        IntPtr ForegroundWindow,
        long InputVersion);
}
