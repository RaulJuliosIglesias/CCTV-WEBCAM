using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RTSPVirtualCam.Models;
using Serilog;

namespace RTSPVirtualCam.Services;

/// <summary>
/// REST API server for mobile companion app control.
/// Provides endpoints for camera control, PTZ, recording, and status.
/// </summary>
public class ApiServerService : IApiServerService
{
    private readonly IMultiCameraService _cameraService;
    private readonly IAdvancedPtzService _ptzService;
    private readonly IRecordingService _recordingService;
    private readonly IRtmpStreamingService _streamingService;
    private readonly ConcurrentDictionary<string, DateTime> _authTokens = new();
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _serverTask;
    private bool _authRequired = true;
    private bool _disposed;

    public event EventHandler<ApiRequestEventArgs>? RequestReceived;
    public event Action<string>? OnLog;

    public bool IsRunning => _listener?.IsListening ?? false;
    public int Port { get; private set; } = 8080;
    public string[] ListeningUrls => _listener?.Prefixes.ToArray() ?? Array.Empty<string>();

    public ApiServerService(
        IMultiCameraService cameraService,
        IAdvancedPtzService ptzService,
        IRecordingService recordingService,
        IRtmpStreamingService streamingService)
    {
        _cameraService = cameraService;
        _ptzService = ptzService;
        _recordingService = recordingService;
        _streamingService = streamingService;
    }

    public async Task StartAsync(int port = 8080)
    {
        if (IsRunning)
        {
            await StopAsync();
        }

        Port = port;
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://+:{port}/api/");

        try
        {
            _listener.Start();
            _cts = new CancellationTokenSource();
            _serverTask = Task.Run(() => ProcessRequestsAsync(_cts.Token));

            LogMessage($"API server started on port {port}");
            LogMessage($"Endpoints available at http://localhost:{port}/api/");
        }
        catch (HttpListenerException ex)
        {
            LogMessage($"Failed to start API server: {ex.Message}");
            LogMessage("Try running as administrator or use: netsh http add urlacl url=http://+:8080/api/ user=Everyone");
            throw;
        }
    }

    public async Task StopAsync()
    {
        _cts?.Cancel();
        _listener?.Stop();
        _listener?.Close();

        if (_serverTask != null)
        {
            try
            {
                await _serverTask;
            }
            catch (OperationCanceledException) { }
        }

        _listener = null;
        LogMessage("API server stopped");
    }

    private async Task ProcessRequestsAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener?.IsListening == true)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = HandleRequestAsync(context, ct);
            }
            catch (HttpListenerException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                LogMessage($"Request error: {ex.Message}");
            }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken ct)
    {
        var request = context.Request;
        var response = context.Response;
        var path = request.Url?.AbsolutePath.ToLower() ?? "/";
        var method = request.HttpMethod;

        try
        {
            // CORS headers
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, X-API-Token");

            // Handle preflight
            if (method == "OPTIONS")
            {
                response.StatusCode = 204;
                response.Close();
                return;
            }

            // Authentication check
            if (_authRequired && !IsAuthenticated(request))
            {
                await SendJsonResponseAsync(response, 401, new { error = "Unauthorized" });
                RaiseRequestEvent(method, path, GetClientIp(request), 401);
                return;
            }

            // Route request
            object? result = await RouteRequestAsync(path, method, request, ct);
            var statusCode = result != null ? 200 : 404;

            if (result != null)
            {
                await SendJsonResponseAsync(response, statusCode, result);
            }
            else
            {
                await SendJsonResponseAsync(response, 404, new { error = "Not found" });
            }

            RaiseRequestEvent(method, path, GetClientIp(request), statusCode);
        }
        catch (Exception ex)
        {
            LogMessage($"Request handler error: {ex.Message}");
            await SendJsonResponseAsync(response, 500, new { error = ex.Message });
            RaiseRequestEvent(method, path, GetClientIp(request), 500);
        }
    }

    private async Task<object?> RouteRequestAsync(string path, string method, HttpListenerRequest request, CancellationToken ct)
    {
        // Parse path segments
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2) return null; // /api/...

        var resource = segments.Length > 1 ? segments[1] : "";
        var id = segments.Length > 2 ? segments[2] : null;
        var action = segments.Length > 3 ? segments[3] : null;

        return resource switch
        {
            "status" => await HandleStatusAsync(method),
            "cameras" => await HandleCamerasAsync(method, id, action, request, ct),
            "ptz" => await HandlePtzAsync(method, id, action, request, ct),
            "recording" => await HandleRecordingAsync(method, id, action, request, ct),
            "streaming" => await HandleStreamingAsync(method, id, action, request, ct),
            "auth" => await HandleAuthAsync(method, action, request),
            _ => null
        };
    }

    // Status endpoint
    private Task<object> HandleStatusAsync(string method)
    {
        if (method != "GET") return Task.FromResult<object>(new { error = "Method not allowed" });

        return Task.FromResult<object>(new
        {
            version = "2.0",
            status = "running",
            cameras = _cameraService.Cameras.Select(c => new
            {
                id = c.Id,
                name = c.Name,
                connected = c.IsConnected,
                virtualized = c.IsVirtualized,
                recording = c.IsRecording,
                streaming = c.IsStreaming
            }),
            timestamp = DateTime.UtcNow
        });
    }

    // Cameras endpoints
    private async Task<object?> HandleCamerasAsync(string method, string? id, string? action, HttpListenerRequest request, CancellationToken ct)
    {
        if (method == "GET" && id == null)
        {
            return _cameraService.Cameras.Select(CameraToDto);
        }

        if (method == "GET" && id != null)
        {
            var camera = _cameraService.GetCamera(id);
            return camera != null ? CameraToDto(camera) : null;
        }

        if (method == "POST" && id == null)
        {
            var camera = _cameraService.AddCamera();
            return CameraToDto(camera);
        }

        if (method == "DELETE" && id != null)
        {
            return new { success = _cameraService.RemoveCamera(id) };
        }

        if (id != null && action != null)
        {
            return action switch
            {
                "connect" when method == "POST" => new { success = await _cameraService.ConnectCameraAsync(id, ct) },
                "disconnect" when method == "POST" => 
                    async () => { await _cameraService.DisconnectCameraAsync(id); return new { success = true }; },
                "virtualize" when method == "POST" => new { success = await _cameraService.StartVirtualCameraAsync(id) },
                "stop-virtual" when method == "POST" =>
                    async () => { await _cameraService.StopVirtualCameraAsync(id); return new { success = true }; },
                "snapshot" when method == "POST" => new { path = await _cameraService.TakeSnapshotAsync(id) },
                "frame" when method == "GET" => GetFrameResponse(id),
                _ => null
            };
        }

        return null;
    }

    private object? GetFrameResponse(string cameraId)
    {
        var (frame, width, height) = _cameraService.GetCurrentFrame(cameraId);
        if (frame == null) return null;

        return new
        {
            width,
            height,
            format = "bgra",
            data = Convert.ToBase64String(frame)
        };
    }

    // PTZ endpoints
    private async Task<object?> HandlePtzAsync(string method, string? cameraId, string? action, HttpListenerRequest request, CancellationToken ct)
    {
        if (cameraId == null) return null;

        if (method == "POST" && action != null)
        {
            var body = await ReadBodyAsync<PtzCommandDto>(request);
            var speed = body?.Speed ?? 50;

            return action switch
            {
                "up" => new { success = await _ptzService.MoveAsync(cameraId, PtzDirection.Up, speed, ct) },
                "down" => new { success = await _ptzService.MoveAsync(cameraId, PtzDirection.Down, speed, ct) },
                "left" => new { success = await _ptzService.MoveAsync(cameraId, PtzDirection.Left, speed, ct) },
                "right" => new { success = await _ptzService.MoveAsync(cameraId, PtzDirection.Right, speed, ct) },
                "stop" => new { success = await _ptzService.StopAsync(cameraId, ct) },
                "zoom-in" => new { success = await _ptzService.ZoomAsync(cameraId, ZoomDirection.In, speed, ct) },
                "zoom-out" => new { success = await _ptzService.ZoomAsync(cameraId, ZoomDirection.Out, speed, ct) },
                "preset" when body?.PresetId != null => new { success = await _ptzService.GoToPresetAsync(cameraId, body.PresetId.Value, ct) },
                _ => null
            };
        }

        if (method == "GET" && action == "presets")
        {
            return await _ptzService.GetPresetsAsync(cameraId);
        }

        if (method == "GET" && action == "tours")
        {
            return await _ptzService.GetToursAsync(cameraId);
        }

        return null;
    }

    // Recording endpoints
    private async Task<object?> HandleRecordingAsync(string method, string? cameraId, string? action, HttpListenerRequest request, CancellationToken ct)
    {
        if (method == "GET" && cameraId == null)
        {
            return await _recordingService.GetRecordingsAsync();
        }

        if (cameraId != null && action != null)
        {
            return action switch
            {
                "start" when method == "POST" => new { success = await _recordingService.StartRecordingAsync(cameraId, null, ct) },
                "stop" when method == "POST" =>
                    async () => { await _recordingService.StopRecordingAsync(cameraId); return new { success = true }; },
                "snapshot" when method == "POST" => new { snapshot = await _recordingService.TakeSnapshotAsync(cameraId) },
                "status" when method == "GET" => new
                {
                    recording = _recordingService.IsRecording(cameraId),
                    state = _recordingService.GetRecordingState(cameraId).ToString(),
                    duration = _recordingService.GetRecordingDuration(cameraId).TotalSeconds
                },
                _ => null
            };
        }

        return null;
    }

    // Streaming endpoints
    private async Task<object?> HandleStreamingAsync(string method, string? cameraId, string? action, HttpListenerRequest request, CancellationToken ct)
    {
        if (cameraId == null) return null;

        if (method == "POST" && action == "start")
        {
            var body = await ReadBodyAsync<StreamingCommandDto>(request);
            if (body == null) return new { error = "Invalid request body" };

            var settings = new StreamingSettings
            {
                Platform = Enum.TryParse<StreamingPlatform>(body.Platform, out var p) ? p : StreamingPlatform.Custom,
                RtmpUrl = body.RtmpUrl ?? string.Empty,
                StreamKey = body.StreamKey ?? string.Empty,
                VideoBitrate = body.Bitrate ?? 4500
            };

            return new { success = await _streamingService.StartStreamingAsync(cameraId, settings, ct) };
        }

        if (method == "POST" && action == "stop")
        {
            await _streamingService.StopStreamingAsync(cameraId);
            return new { success = true };
        }

        if (method == "GET" && action == "status")
        {
            return new
            {
                streaming = _streamingService.IsStreaming(cameraId),
                state = _streamingService.GetStreamingState(cameraId).ToString(),
                stats = _streamingService.GetStreamingStats(cameraId)
            };
        }

        return null;
    }

    // Auth endpoints
    private Task<object?> HandleAuthAsync(string method, string? action, HttpListenerRequest request)
    {
        if (method == "POST" && action == "token")
        {
            var token = GenerateAuthToken();
            return Task.FromResult<object?>(new { token, expiresIn = 86400 });
        }

        if (method == "DELETE" && action == "token")
        {
            var token = request.Headers["X-API-Token"];
            if (token != null) RevokeAuthToken(token);
            return Task.FromResult<object?>(new { success = true });
        }

        return Task.FromResult<object?>(null);
    }

    // Authentication
    public string GenerateAuthToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        var token = Convert.ToBase64String(bytes);
        _authTokens[token] = DateTime.UtcNow.AddDays(1);
        return token;
    }

    public bool ValidateAuthToken(string token)
    {
        if (_authTokens.TryGetValue(token, out var expiry))
        {
            if (expiry > DateTime.UtcNow) return true;
            _authTokens.TryRemove(token, out _);
        }
        return false;
    }

    public void RevokeAuthToken(string token)
    {
        _authTokens.TryRemove(token, out _);
    }

    private bool IsAuthenticated(HttpListenerRequest request)
    {
        var token = request.Headers["X-API-Token"] ?? request.Headers["Authorization"]?.Replace("Bearer ", "");
        return token != null && ValidateAuthToken(token);
    }

    public void SetAuthRequired(bool required) => _authRequired = required;
    public void SetPort(int port) => Port = port;

    // Helpers
    private static object CameraToDto(CameraInstance c) => new
    {
        id = c.Id,
        name = c.Name,
        slotIndex = c.SlotIndex,
        rtspUrl = c.RtspUrl,
        ipAddress = c.IpAddress,
        port = c.Port,
        brand = c.Brand.ToString(),
        connected = c.IsConnected,
        virtualized = c.IsVirtualized,
        recording = c.IsRecording,
        streaming = c.IsStreaming,
        width = c.Width,
        height = c.Height,
        frameRate = c.FrameRate,
        codec = c.Codec
    };

    private static async Task<T?> ReadBodyAsync<T>(HttpListenerRequest request) where T : class
    {
        try
        {
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
            var json = await reader.ReadToEndAsync();
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }

    private static async Task SendJsonResponseAsync(HttpListenerResponse response, int statusCode, object data)
    {
        response.StatusCode = statusCode;
        response.ContentType = "application/json";

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var buffer = Encoding.UTF8.GetBytes(json);

        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer);
        response.Close();
    }

    private static string GetClientIp(HttpListenerRequest request)
    {
        return request.Headers["X-Forwarded-For"] ?? request.RemoteEndPoint?.Address.ToString() ?? "unknown";
    }

    private void RaiseRequestEvent(string method, string path, string clientIp, int statusCode)
    {
        RequestReceived?.Invoke(this, new ApiRequestEventArgs
        {
            Method = method,
            Path = path,
            ClientIp = clientIp,
            StatusCode = statusCode
        });
    }

    private void LogMessage(string message)
    {
        Log.Information($"[API] {message}");
        OnLog?.Invoke(message);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopAsync().Wait();
    }

    // DTOs for request bodies
    private class PtzCommandDto
    {
        public int? Speed { get; set; }
        public int? PresetId { get; set; }
    }

    private class StreamingCommandDto
    {
        public string? Platform { get; set; }
        public string? RtmpUrl { get; set; }
        public string? StreamKey { get; set; }
        public int? Bitrate { get; set; }
    }
}
