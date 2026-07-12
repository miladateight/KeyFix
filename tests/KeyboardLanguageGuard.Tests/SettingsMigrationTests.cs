using System.Text.Json;
using KeyboardLanguageGuard.Core.Settings;
using Xunit;

namespace KeyboardLanguageGuard.Tests;

public sealed class SettingsMigrationTests
{
    [Fact]
    public void V6_Upgrade_Preserves_User_Mode_And_First_Run()
    {
        AppSettings settings = new()
        {
            SettingsVersion = 6,
            Mode = DetectionMode.AlertOnly,
            FirstRunCompleted = true
        };

        SettingsMigrator.Migrate(settings);

        Assert.Equal(AppSettings.CurrentSettingsVersion, settings.SettingsVersion);
        Assert.Equal(DetectionMode.AlertOnly, settings.Mode);       // choice preserved
        Assert.True(settings.FirstRunCompleted);                     // not reset
    }

    [Fact]
    public void V6_Upgrade_Does_Not_Enable_Spelling_Auto_Correction()
    {
        AppSettings settings = new() { SettingsVersion = 6 };

        SettingsMigrator.Migrate(settings);

        Assert.False(settings.EnableSpellingAutoCorrection);
        Assert.False(settings.EnableSpellingDetection);
        Assert.Equal(CorrectionAggressiveness.Conservative, settings.CorrectionAggressiveness);
    }

    [Fact]
    public void Old_Pre6_File_Gets_Legacy_Reset()
    {
        AppSettings settings = new()
        {
            SettingsVersion = 3,
            Mode = DetectionMode.AlertOnly,
            FirstRunCompleted = true
        };

        SettingsMigrator.Migrate(settings);

        Assert.Equal(DetectionMode.AutoSwitch, settings.Mode);
        Assert.False(settings.FirstRunCompleted);
    }

    [Fact]
    public void Missing_New_Fields_In_Json_Default_To_Safe_Values()
    {
        // Simulate a real v6 settings.json that predates the v7 fields entirely.
        const string legacyJson = """
        { "SettingsVersion": 6, "Mode": 1, "FirstRunCompleted": true }
        """;

        AppSettings settings = JsonSerializer.Deserialize<AppSettings>(legacyJson)!;
        SettingsMigrator.Migrate(settings);

        Assert.False(settings.EnableSpellingAutoCorrection);
        Assert.True(settings.EnableWrongLayoutDetection);
        Assert.Equal(CorrectionAggressiveness.Conservative, settings.CorrectionAggressiveness);
        Assert.Equal(DetectionMode.AlertAndSuggest, settings.Mode);
    }

    [Fact]
    public void Fresh_Settings_Need_No_Migration()
    {
        AppSettings settings = new();
        bool changed = SettingsMigrator.Migrate(settings);
        Assert.False(changed);
    }
}
