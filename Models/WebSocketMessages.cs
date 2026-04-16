using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DesktopAssistant.Models;

public class ServerMessage
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("payload")]
    public Dictionary<string, object> Payload { get; set; } = new();
}

public static class ServerMessageTypes
{
    public const string Connected = "connected";
    public const string ActionPlan = "action_plan";
    public const string ActionStarted = "action_started";
    public const string ActionResult = "action_result";
    public const string PlanComplete = "plan_complete";
    public const string Error = "error";
    public const string Pong = "pong";
    public const string RemoteCommand = "remote_command";
    public const string TelemetryUpdate = "telemetry_update";
}
