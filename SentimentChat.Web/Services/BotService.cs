using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SentimentChat.Web.Services
{
    internal class ClaudeRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "claude-3-haiku-20240307";

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; } = 512;

        [JsonPropertyName("system")]
        public string System { get; set; } = "";

        [JsonPropertyName("messages")]
        public List<ClaudeMessage> Messages { get; set; } = new();
    }

    internal class ClaudeMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "";

        [JsonPropertyName("content")]
        public string Content { get; set; } = "";
    }

    public class BotService : IDisposable
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;

        private const string API_URL = "https://api.anthropic.com/v1/messages";
        private const string ANTHROPIC_VER = "2023-06-01";

        private const string SYSTEM_PROMPT =
            "You are a friendly WhatsApp-style AI chatbot. " +
            "Reply in 1-2 short sentences only. " +
            "Be warm, natural, and conversational.";

        public BotService()
        {
            LoadDotEnv();

            _apiKey = Environment.GetEnvironmentVariable("CLAUDE_API_KEY")
                   ?? throw new ArgumentException(
                        "CLAUDE_API_KEY not found!\n\n" +
                        "Create .env file in project root:\n" +
                        "    CLAUDE_API_KEY=sk-ant-api03-XXXX");

            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            _http.DefaultRequestHeaders.Add("x-api-key", _apiKey);
            _http.DefaultRequestHeaders.Add("anthropic-version", ANTHROPIC_VER);
            _http.DefaultRequestHeaders.Accept
                 .Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        private static void LoadDotEnv()
        {
            string[] paths =
            {
                Path.Combine(Directory.GetCurrentDirectory(), ".env"),
                Path.Combine(Directory.GetCurrentDirectory(), @"..\.env"),
                Path.Combine(Directory.GetCurrentDirectory(), @"..\..\.env"),
            };

            foreach (string p in paths)
            {
                string full = Path.GetFullPath(p);
                if (!File.Exists(full)) continue;

                foreach (string raw in File.ReadAllLines(full))
                {
                    string line = raw.Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith('#')) continue;
                    int eq = line.IndexOf('=');
                    if (eq < 1) continue;
                    string k = line[..eq].Trim();
                    string v = line[(eq + 1)..].Trim().Trim('"').Trim('\'');
                    Environment.SetEnvironmentVariable(k, v);
                }
                break;
            }
        }

        public async Task<string> GetBotReplyAsync(string userMessage)
        {
            var request = new ClaudeRequest
            {
                Model = "claude-3-haiku-20240307",
                MaxTokens = 512,
                System = SYSTEM_PROMPT,
                Messages = new List<ClaudeMessage>
                {
                    new() { Role = "user", Content = userMessage }
                }
            };

            var body = JsonSerializer.Serialize(request);
            var content = new StringContent(body, Encoding.UTF8, "application/json");

            HttpResponseMessage resp;
            try
            {
                resp = await _http.PostAsync(API_URL, content);
            }
            catch (TaskCanceledException)
            {
                throw new Exception("Request timed out. Check internet.");
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Network error: {ex.Message}");
            }

            string json = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                string detail = TryGetErrorMessage(json);
                throw new Exception(resp.StatusCode switch
                {
                    System.Net.HttpStatusCode.Unauthorized => "Invalid API key — check .env file.",
                    System.Net.HttpStatusCode.TooManyRequests => "Rate limit — wait and retry.",
                    System.Net.HttpStatusCode.BadRequest => $"Bad request: {detail}",
                    _ => $"HTTP {(int)resp.StatusCode}: {detail}"
                });
            }

            try
            {
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement
                          .GetProperty("content")[0]
                          .GetProperty("text")
                          .GetString()
                          ?.Trim() ?? "(empty reply)";
            }
            catch
            {
                throw new Exception(
                    $"Unexpected response format: {json[..Math.Min(200, json.Length)]}");
            }
        }

        private static string TryGetErrorMessage(string json)
        {
            try
            {
                using var d = JsonDocument.Parse(json);
                if (d.RootElement.TryGetProperty("error", out var e) &&
                    e.TryGetProperty("message", out var m))
                    return m.GetString() ?? "";
            }
            catch { }
            return json.Length > 100 ? json[..100] : json;
        }

        public void Dispose() => _http.Dispose();
    }
}
