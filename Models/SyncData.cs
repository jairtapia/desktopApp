using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DesktopAssistant.Models;

public class SyncCategory
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;

    [JsonPropertyName("icon")]
    public string Icon { get; set; } = string.Empty;

    [JsonPropertyName("shortcuts")]
    public List<SyncShortcut> Shortcuts { get; set; } = new();
}

public class SyncShortcut
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("icon")]
    public string Icon { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public string Size { get; set; } = "small";

    [JsonPropertyName("subtitle")]
    public string? Subtitle { get; set; }

    [JsonPropertyName("detail")]
    public string? Detail { get; set; }

    [JsonPropertyName("stats")]
    public List<SyncStat>? Stats { get; set; }

    [JsonPropertyName("progressValue")]
    public double? ProgressValue { get; set; }

    [JsonPropertyName("progressLabel")]
    public List<string>? ProgressLabel { get; set; }

    [JsonPropertyName("value")]
    public object? Value { get; set; }

    [JsonPropertyName("min")]
    public double? Min { get; set; }

    [JsonPropertyName("max")]
    public double? Max { get; set; }

    [JsonPropertyName("unit")]
    public string? Unit { get; set; }

    [JsonPropertyName("actionType")]
    public string? ActionType { get; set; }

    [JsonPropertyName("options")]
    public List<string>? Options { get; set; }

    [JsonPropertyName("logs")]
    public List<string>? Logs { get; set; }

    [JsonPropertyName("settingsGroups")]
    public List<SyncSettingsGroup>? SettingsGroups { get; set; }
}

public class SyncStat
{
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

public class SyncSettingsGroup
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("fields")]
    public List<SyncField> Fields { get; set; } = new();
}

public class SyncField
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "info";

    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public object? Value { get; set; }

    [JsonPropertyName("min")]
    public double? Min { get; set; }

    [JsonPropertyName("max")]
    public double? Max { get; set; }

    [JsonPropertyName("unit")]
    public string? Unit { get; set; }

    [JsonPropertyName("options")]
    public List<string>? Options { get; set; }
}
