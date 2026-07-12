using System.Text.Json;
using KeyboardLanguageGuard.Core.Settings;
using Xunit;

namespace KeyboardLanguageGuard.Tests;

public sealed class SettingsMigrationV8Tests
{
    [Fact]
    public void V7_Upgrade_Preserves_Choices_And_Adds_Safe_V8_Defaults()
    {
        // A v7 settings.json that predates the v8 fields (diagnostic logging, Persian style).
        const string v7Json = """
        {
          "SettingsVersion": 7,
          "Mode": 2,
          "FirstRunCompleted": true,
          "EnableSpellingAutoCorrection": true,
          "CorrectionAggressiveness": 1
        }
        """;

        AppSettings settings = JsonSerializer.Deserialize<AppSettings>(v7Json)!;
        SettingsMigrator.Migrate(settings);

        Assert.Equal(AppSettings.CurrentSettingsVersion, settings.SettingsVersion); // now 8
        Assert.True(settings.EnableSpellingAutoCorrection);                          // user choice preserved
        Assert.Equal(CorrectionAggressiveness.Balanced, settings.CorrectionAggressiveness);
        Assert.False(settings.EnableDiagnosticLogging);                              // safe v8 default
        Assert.Equal(PersianCorrectionStyle.PreserveUserStyle, settings.PersianCorrectionStyle);
    }
}
