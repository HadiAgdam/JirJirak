
using System.Text.Json;

class MainClass
{
    private const double DefaultDuration = 10;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public static async Task Main()
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

        using var client = new HttpClient(
            new HttpClientHandler { AllowAutoRedirect = false })
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        Console.WriteLine($"Reading configuration from {configPath}");

        var targets = new Dictionary<string, TargetState>();

        while (true)
        {
            var settings = await ReadSettings(configPath);

            if (settings != null)
            {
                var activeTargets = new HashSet<string>();

                foreach (var target in settings.Targets ?? [])
                {
                    if (target?.Enabled != true)
                        continue;

                    if (!Uri.TryCreate(
                            target.Url?.Trim(),
                            UriKind.Absolute,
                            out var endpoint) ||
                        (endpoint.Scheme != Uri.UriSchemeHttp &&
                         endpoint.Scheme != Uri.UriSchemeHttps) ||
                        string.IsNullOrEmpty(endpoint.Host) ||
                        endpoint.UserInfo.Length != 0 ||
                        endpoint.Query.Length != 0 ||
                        endpoint.Fragment.Length != 0)
                    {
                        Console.Error.WriteLine(
                            "Invalid target URL. Expected an absolute HTTP(S) URL without credentials, query or fragment.");
                        continue;
                    }

                    if (!string.IsNullOrEmpty(target.Token) &&
                        ((!endpoint.IsLoopback &&
                          endpoint.Scheme != Uri.UriSchemeHttps) ||
                         target.Token.Any(c =>
                             !char.IsAsciiLetterOrDigit(c) &&
                             c != '-' && c != '_')))
                    {
                        Console.Error.WriteLine(
                            $"Invalid or insecure worker credential configuration for {endpoint}.");
                        continue;
                    }

                    var duration = target.Duration ?? DefaultDuration;

                    if (!double.IsFinite(duration) ||
                        duration < 0.001 ||
                        duration > 1440)
                    {
                        Console.Error.WriteLine(
                            $"Invalid duration for {endpoint}. Using {DefaultDuration} minutes.");

                        duration = DefaultDuration;
                    }

                    var key = endpoint.AbsoluteUri;

                    activeTargets.Add(key);

                    if (!targets.ContainsKey(key))
                    {
                        targets[key] = new TargetState
                        {
                            Endpoint = endpoint,
                            Token = target.Token,
                            Duration = duration,
                            NextExecution = DateTimeOffset.UtcNow
                        };
                    }
                    else
                    {
                        var state = targets[key];

                        if (state.Duration != duration ||
                            state.Token != target.Token)
                        {
                            state.Duration = duration;
                            state.Token = target.Token;
                            state.NextExecution = DateTimeOffset.UtcNow;
                        }
                    }
                }

                foreach (var key in targets.Keys
                             .Where(x => !activeTargets.Contains(x))
                             .ToList())
                {
                    targets.Remove(key);
                }
            }

            var now = DateTimeOffset.UtcNow;

            foreach (var state in targets.Values.ToList())
            {
                if (now < state.NextExecution)
                    continue;

                await SendRequest(client, state.Endpoint, state.Token);

                state.NextExecution = DateTimeOffset.UtcNow
                    .AddMinutes(state.Duration);
            }

            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }

    private static async Task<WorkerSettings?> ReadSettings(string path)
    {
        try
        {
            await using var stream = File.OpenRead(path);

            var settings =
                await JsonSerializer.DeserializeAsync<WorkerSettings>(
                    stream, JsonOptions);

            if (settings?.Targets == null)
                throw new JsonException("Targets are required.");

            return settings;
        }
        catch (Exception ex) when (
            ex is IOException or
            UnauthorizedAccessException or
            JsonException)
        {
            Console.Error.WriteLine(
                $"Cannot read {path}. Retrying in {DefaultDuration} minutes. {ex.Message}");

            return null;
        }
    }

    private static async Task SendRequest(
        HttpClient client,
        Uri endpoint,
        string? token)
    {
        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get, endpoint);

            if (!string.IsNullOrEmpty(token))
                request.Headers.Add("X-Absence-Worker-Token", token);

            using var response = await client.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead);

            Console.WriteLine(
                $"{DateTimeOffset.UtcNow:O} {endpoint} returned " +
                $"{(int)response.StatusCode} {response.StatusCode}.");

            if (!response.IsSuccessStatusCode)
            {
                Console.Error.WriteLine(
                    $"Request failed with status code {(int)response.StatusCode}.");
            }
        }
        catch (Exception ex) when (
            ex is HttpRequestException or
            OperationCanceledException or
            IOException)
        {
            Console.Error.WriteLine(
                $"{DateTimeOffset.UtcNow:O} {endpoint} failed. {ex}");
        }
    }

    private sealed record WorkerSettings(List<Target?>? Targets);

    private sealed record Target(
        string? Url,
        double? Duration,
        bool Enabled,
        string? Token);

    private sealed class TargetState
    {
        public required Uri Endpoint { get; init; }
        public string? Token { get; set; }
        public double Duration { get; set; }
        public DateTimeOffset NextExecution { get; set; }
    }
}
