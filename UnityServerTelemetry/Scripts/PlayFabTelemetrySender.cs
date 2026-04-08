using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Sends telemetry events to the PlayFab WriteTelemetryEvents API
/// using a telemetry key for authentication.
/// </summary>
public class PlayFabTelemetrySender
{
    readonly string _titleId;
    readonly string _telemetryKey;
    readonly string _serverId;
    readonly string _url;

    const int MaxEventsPerBatch = 200;
    const int RequestTimeoutSeconds = 30;
    const int MaxEntityIdLength = 64;

    public PlayFabTelemetrySender(string titleId, string telemetryKey, string serverId)
    {
        _titleId = titleId;
        _telemetryKey = telemetryKey;
        _serverId = serverId.Length > MaxEntityIdLength ? serverId.Substring(0, MaxEntityIdLength) : serverId;
        _url = $"https://{_titleId}.playfabapi.com/Event/WriteTelemetryEvents";
    }

    /// <summary>
    /// Sends a list of metric snapshots as telemetry events.
    /// Each snapshot becomes one event. Batches into groups of 200 (API limit).
    /// </summary>
    public IEnumerator SendMetrics(List<Dictionary<string, object>> metricsList)
    {
        if (metricsList.Count == 0) yield break;

        for (int i = 0; i < metricsList.Count; i += MaxEventsPerBatch)
        {
            int count = Mathf.Min(MaxEventsPerBatch, metricsList.Count - i);
            string json = BuildRequestJson(metricsList, i, count);

            using (var request = new UnityWebRequest(_url, UnityWebRequest.kHttpVerbPOST))
            {
                byte[] bodyBytes = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyBytes) { contentType = "application/json" };
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("X-TelemetryKey", _telemetryKey);
                request.timeout = RequestTimeoutSeconds;

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[PlayFabTelemetrySender] Failed to send telemetry: {request.error} (HTTP {request.responseCode})");
                }
                else
                {
                    Debug.Log($"[PlayFabTelemetrySender] Sent {count} telemetry event(s)");
                }
            }
        }
    }

    string BuildRequestJson(List<Dictionary<string, object>> metricsList, int startIndex, int count)
    {
        var sb = new StringBuilder();
        sb.Append("{\"Events\":[");

        for (int i = startIndex; i < startIndex + count; i++)
        {
            if (i > startIndex) sb.Append(",");

            var metrics = metricsList[i];
            sb.Append("{");
            sb.Append("\"EventNamespace\":\"custom.server_telemetry\",");
            sb.Append("\"Name\":\"server_metrics\",");
            string timestamp = metrics.ContainsKey("_timestamp") ? EscapeJson(metrics["_timestamp"].ToString()) : DateTime.UtcNow.ToString("O");
            sb.Append($"\"OriginalTimestamp\":\"{timestamp}\",");
            sb.Append($"\"Entity\":{{\"type\":\"external\",\"id\":\"{EscapeJson(_serverId)}\"}},");
            sb.Append("\"Payload\":{");

            bool first = true;
            foreach (var kvp in metrics)
            {
                // Skip internal fields
                if (kvp.Key.StartsWith("_")) continue;
                if (!first) sb.Append(",");
                first = false;

                sb.Append($"\"{kvp.Key}\":");
                AppendJsonValue(sb, kvp.Value);
            }

            sb.Append("}}");
        }

        sb.Append("]}");
        return sb.ToString();
    }

    static void AppendJsonValue(StringBuilder sb, object value)
    {
        switch (value)
        {
            case int i:
                sb.Append(i);
                break;
            case long l:
                sb.Append(l);
                break;
            case float f:
                sb.Append(f.ToString("G", CultureInfo.InvariantCulture));
                break;
            case double d:
                sb.Append(d.ToString("G", CultureInfo.InvariantCulture));
                break;
            case string s:
                sb.Append($"\"{EscapeJson(s)}\"");
                break;
            case bool b:
                sb.Append(b ? "true" : "false");
                break;
            default:
                sb.Append($"\"{EscapeJson(value?.ToString() ?? "null")}\"");
                break;
        }
    }

    static string EscapeJson(string s)
    {
        return s.Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t")
                .Replace("\b", "\\b")
                .Replace("\f", "\\f");
    }
}
