namespace PlayFab.Samples.UnityMpsTelemetry
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;
    using UnityEngine;
    using UnityEngine.Networking;

    public sealed class PlayFabTelemetryClient : MonoBehaviour
    {
        private readonly object _syncRoot = new object();
        private readonly List<Dictionary<string, object>> _pendingEvents = new List<Dictionary<string, object>>();
        private readonly TelemetryJsonSerializer _json = new TelemetryJsonSerializer();

        private Dictionary<string, object> _customTags;
        private string _endpoint;
        private string _eventNamespace;
        private string _externalEntityId;
        private string _telemetryKey;
        private bool _configured;
        private bool _flushInProgress;
        private bool _lastFlushSucceeded = true;
        private bool _missingKeyLogged;

        public int PendingEventCount
        {
            get
            {
                lock (_syncRoot)
                {
                    return _pendingEvents.Count;
                }
            }
        }

        public void Configure(string titleId, string telemetryKey, string eventNamespaceName, string externalEntityId)
        {
            if (string.IsNullOrWhiteSpace(titleId))
            {
                throw new ArgumentException("A PlayFab title ID is required to configure telemetry.", nameof(titleId));
            }

            _telemetryKey = telemetryKey;
            _eventNamespace = string.IsNullOrWhiteSpace(eventNamespaceName)
                ? TelemetrySampleConfig.EventNamespace
                : eventNamespaceName;
            _externalEntityId = NormalizeExternalEntityId(externalEntityId);
            _endpoint = string.Format("https://{0}.playfabapi.com/Event/WriteTelemetryEvents", titleId);
            _customTags = new Dictionary<string, object>
            {
                { "sample", "UnityMpsTelemetry" },
                { "sampleVersion", "1" }
            };
            _configured = true;

            if (!TelemetrySampleConfig.IsConfiguredTelemetryKey(_telemetryKey))
            {
                LogMissingTelemetryKey();
            }
        }

        public void SetExternalEntityId(string externalEntityId)
        {
            _externalEntityId = NormalizeExternalEntityId(externalEntityId);
        }

        public bool Enqueue(string eventName, IDictionary<string, object> payload)
        {
            if (!_configured)
            {
                Debug.LogError("Telemetry event was not queued because the telemetry client is not configured.");
                return false;
            }

            if (!TelemetrySampleConfig.IsConfiguredTelemetryKey(_telemetryKey))
            {
                LogMissingTelemetryKey();
                return false;
            }

            if (string.IsNullOrWhiteSpace(eventName))
            {
                Debug.LogError("Telemetry event was not queued because the event name is empty.");
                return false;
            }

            IDictionary<string, object> eventPayload = payload;
            if (eventPayload == null || eventPayload.Count == 0)
            {
                eventPayload = new Dictionary<string, object>
                {
                    { "schemaVersion", 1 }
                };
            }

            Dictionary<string, object> telemetryEvent = new Dictionary<string, object>
            {
                { "EventNamespace", _eventNamespace },
                { "Name", eventName },
                { "OriginalId", Guid.NewGuid().ToString("N") },
                { "OriginalTimestamp", DateTime.UtcNow.ToString("o") },
                {
                    "Entity",
                    new Dictionary<string, object>
                    {
                        { "Type", "external" },
                        { "Id", _externalEntityId }
                    }
                },
                { "Payload", eventPayload }
            };

            lock (_syncRoot)
            {
                if (_pendingEvents.Count >= TelemetrySampleConfig.MaxQueuedEvents)
                {
                    _pendingEvents.RemoveAt(0);
                    Debug.LogWarning("Telemetry queue is full. Dropped the oldest pending telemetry event.");
                }

                _pendingEvents.Add(telemetryEvent);
            }

            return true;
        }

        public IEnumerator FlushAsync()
        {
            if (_flushInProgress)
            {
                yield break;
            }

            if (!_configured || !TelemetrySampleConfig.IsConfiguredTelemetryKey(_telemetryKey))
            {
                yield break;
            }

            List<Dictionary<string, object>> batch = DequeueBatch();
            if (batch.Count == 0)
            {
                yield break;
            }

            _flushInProgress = true;
            _lastFlushSucceeded = false;

            UnityWebRequest request;
            try
            {
                Dictionary<string, object> requestBody = new Dictionary<string, object>
                {
                    { "Events", batch },
                    { "CustomTags", _customTags }
                };

                string payload = _json.SerializeObject(requestBody);
                byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);

                if (TelemetrySampleConfig.LogTelemetryPayloads)
                {
                    Debug.Log(payload);
                }

                request = CreateRequest(payloadBytes);
            }
            catch (Exception ex)
            {
                RequeueBatch(batch);
                _flushInProgress = false;
                Debug.LogException(ex);
                yield break;
            }

            bool batchHandled = false;

            try
            {
                using (request)
                {
                    yield return request.SendWebRequest();

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        RequeueBatch(batch);
                        batchHandled = true;
                        string responseBody = request.downloadHandler == null ? string.Empty : request.downloadHandler.text;
                        Debug.LogErrorFormat(
                            "PlayFab telemetry flush failed. HttpCode={0}, Error={1}, Body={2}",
                            request.responseCode,
                            request.error,
                            responseBody);
                    }
                    else
                    {
                        batchHandled = true;
                        _lastFlushSucceeded = true;
                        Debug.LogFormat("Flushed {0} telemetry event(s) to PlayFab.", batch.Count);
                    }
                }
            }
            finally
            {
                if (!batchHandled)
                {
                    RequeueBatch(batch);
                }

                _flushInProgress = false;
            }
        }

        public IEnumerator FlushAllAsync(float timeoutSeconds)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (PendingEventCount > 0 && Time.realtimeSinceStartup < deadline)
            {
                while (_flushInProgress && Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                }

                yield return FlushAsync();

                if (!_lastFlushSucceeded && PendingEventCount > 0 && Time.realtimeSinceStartup < deadline)
                {
                    yield return new WaitForSecondsRealtime(TelemetrySampleConfig.TelemetryRetryDelaySeconds);
                }
            }

            if (PendingEventCount > 0)
            {
                Debug.LogWarningFormat(
                    "Telemetry shutdown flush timed out with {0} pending event(s).",
                    PendingEventCount);
            }
        }

        private UnityWebRequest CreateRequest(byte[] payloadBytes)
        {
            UnityWebRequest request = new UnityWebRequest(_endpoint, UnityWebRequest.kHttpVerbPOST);
            request.SetRequestHeader("Accept", "application/json");
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-TelemetryKey", _telemetryKey);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.uploadHandler = new UploadHandlerRaw(payloadBytes) { contentType = "application/json" };
            return request;
        }

        private List<Dictionary<string, object>> DequeueBatch()
        {
            lock (_syncRoot)
            {
                int count = Math.Min(TelemetrySampleConfig.MaxEventsPerBatch, _pendingEvents.Count);
                List<Dictionary<string, object>> batch = _pendingEvents.GetRange(0, count);
                _pendingEvents.RemoveRange(0, count);
                return batch;
            }
        }

        private void RequeueBatch(List<Dictionary<string, object>> batch)
        {
            lock (_syncRoot)
            {
                int availableSlots = TelemetrySampleConfig.MaxQueuedEvents - _pendingEvents.Count;
                if (availableSlots <= 0)
                {
                    Debug.LogWarningFormat(
                        "Telemetry queue is full. Dropped {0} event(s) after a failed flush.",
                        batch.Count);
                    return;
                }

                if (batch.Count > availableSlots)
                {
                    int droppedEventCount = batch.Count - availableSlots;
                    batch.RemoveRange(0, droppedEventCount);
                    Debug.LogWarningFormat(
                        "Telemetry queue is near capacity. Dropped {0} oldest event(s) after a failed flush.",
                        droppedEventCount);
                }

                _pendingEvents.InsertRange(0, batch);
            }
        }

        private void LogMissingTelemetryKey()
        {
            if (_missingKeyLogged)
            {
                return;
            }

            _missingKeyLogged = true;
            Debug.LogWarning(
                "PlayFab telemetry is disabled because TelemetrySampleConfig.TelemetryKey still contains the placeholder value.");
        }

        private static string NormalizeExternalEntityId(string value)
        {
            string normalized = string.IsNullOrWhiteSpace(value) ? "unity-mps-server" : value.Trim();
            return normalized.Length <= 64 ? normalized : normalized.Substring(0, 64);
        }
    }
}
