using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Windows.Forms;

namespace KeyboardLanguageGuard.App.Services;

/// <summary>
/// Asks the public GitHub Releases API whether a newer KeyFix exists and shows a tray balloon if
/// so. Read-only and anonymous: it sends no typed text, no telemetry and no account information,
/// downloads nothing automatically, and the only action it ever takes is opening the release page
/// when the user clicks the balloon.
/// </summary>
public sealed class UpdateCheckerService : IDisposable
{
    private const string LatestReleaseUrl = "https://api.github.com/repos/miladateight/KeyFix/releases/latest";
    private const string ReleasesPageUrl = "https://github.com/miladateight/KeyFix/releases/latest";

    // The interval the user configures is a day by default, but a session can outlive it, so the
    // timer wakes up hourly and only acts once the configured interval has actually elapsed.
    private static readonly TimeSpan PollInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan FirstCheckDelay = TimeSpan.FromMinutes(2);

    private readonly NotifyIcon _notifyIcon;
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _checkLock = new(1, 1);
    private System.Threading.Timer? _timer;
    private DateTimeOffset _lastCheck = DateTimeOffset.MinValue;
    private string? _pendingReleaseUrl;
    private bool _disposed;

    public UpdateCheckerService(NotifyIcon notifyIcon)
    {
        _notifyIcon = notifyIcon;
        _notifyIcon.BalloonTipClicked += OnBalloonTipClicked;

        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        // GitHub rejects API requests that arrive without a User-Agent.
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("KeyFix", CurrentVersion().ToString()));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
    }

    /// <summary>Whether the periodic check runs at all. A manual check ignores this.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Hours between automatic checks; values below 1 are treated as 1.</summary>
    public int CheckIntervalHours { get; set; } = 24;

    public void Start()
    {
        _timer ??= new System.Threading.Timer(OnTimerTick, null, FirstCheckDelay, PollInterval);
    }

    /// <summary>
    /// Performs one check now. Never throws: no network problem should be able to interrupt
    /// typing, so a failure is silent except in the manual case, where the user is waiting for an
    /// answer and gets one either way.
    /// </summary>
    public async Task CheckAsync(bool announceWhenUpToDate = false)
    {
        if (_disposed || !await _checkLock.WaitAsync(0).ConfigureAwait(false))
        {
            return;
        }

        try
        {
            _lastCheck = DateTimeOffset.UtcNow;

            ReleaseInfo? release = await FetchLatestReleaseAsync().ConfigureAwait(false);
            if (release is null)
            {
                if (announceWhenUpToDate)
                {
                    ShowBalloon("KeyFix", "Could not check for updates right now.", null);
                }

                return;
            }

            if (release.Version > CurrentVersion())
            {
                ShowBalloon(
                    "KeyFix update available",
                    $"Version {release.Version} is available. Click to open the release page.",
                    release.Url);
            }
            else if (announceWhenUpToDate)
            {
                ShowBalloon("KeyFix", $"You are on the latest version ({CurrentVersion()}).", null);
            }
        }
        finally
        {
            _checkLock.Release();
        }
    }

    private void OnTimerTick(object? state)
    {
        if (_disposed || !IsEnabled)
        {
            return;
        }

        if (DateTimeOffset.UtcNow - _lastCheck < TimeSpan.FromHours(Math.Max(1, CheckIntervalHours)))
        {
            return;
        }

        _ = CheckAsync();
    }

    private async Task<ReleaseInfo?> FetchLatestReleaseAsync()
    {
        try
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(LatestReleaseUrl).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using JsonDocument document = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);

            if (!document.RootElement.TryGetProperty("tag_name", out JsonElement tag))
            {
                return null;
            }

            Version? version = ParseTag(tag.GetString());
            if (version is null)
            {
                return null;
            }

            string url = document.RootElement.TryGetProperty("html_url", out JsonElement htmlUrl)
                ? htmlUrl.GetString() ?? ReleasesPageUrl
                : ReleasesPageUrl;

            return new ReleaseInfo(version, url);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            return null;
        }
    }

    /// <summary>Turns a "v1.2.3" release tag into a comparable version.</summary>
    private static Version? ParseTag(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return null;
        }

        string trimmed = tag.Trim().TrimStart('v', 'V');
        return Version.TryParse(trimmed, out Version? version) ? Normalize(version) : null;
    }

    /// <summary>
    /// Compares on the three parts a release tag actually carries. The assembly version has a
    /// fourth (1.0.0.0), and comparing that against a parsed 1.0.0 would report every build as
    /// newer than itself.
    /// </summary>
    private static Version Normalize(Version version) =>
        new(version.Major, version.Minor, Math.Max(0, version.Build));

    private static Version CurrentVersion()
    {
        Version? assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
        return assemblyVersion is null ? new Version(0, 0, 0) : Normalize(assemblyVersion);
    }

    private void ShowBalloon(string title, string message, string? url)
    {
        _pendingReleaseUrl = url;
        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.ShowBalloonTip(5000);
    }

    private void OnBalloonTipClicked(object? sender, EventArgs eventArgs)
    {
        string? url = _pendingReleaseUrl;
        if (string.IsNullOrEmpty(url))
        {
            return;
        }

        // One balloon, one navigation: a later click on an unrelated balloon must not reopen this.
        _pendingReleaseUrl = null;

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            // No default browser, or the shell refused. Nothing useful to do about it here.
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _notifyIcon.BalloonTipClicked -= OnBalloonTipClicked;
        _timer?.Dispose();
        _httpClient.Dispose();
        _checkLock.Dispose();
    }

    private sealed record ReleaseInfo(Version Version, string Url);
}
