using System.Text;
using System.Text.Json;
using DotNetEnv;
namespace SentimentChat.Services
{
    public class BotService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly List<object> _history = new();
        public BotService()
        {
            Env.TraversePath().Load();
            string apiKey = Environment.GetEnvironmentVariable("CLAUDE_API_KEY")
                ?? throw new Exception("CLAUDE_API_KEY nahi mili! .env check karo.");

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.anthropic.com"),
                Timeout = TimeSpan.FromSeconds(30)
            };
            _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
            _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        }

        public async Task<string> GetBotReplyAsync(string userMessage)
        {
            _history.Add(new { role = "user", content = userMessage });

            var body = new
            {
                model = "claude-sonnet-4-20250514",
                max_tokens = 150,
                system = "Tu aik WhatsApp chat bot hai jo Roman Urdu mein baat karta hai. Responses short rakho (1-2 lines). Emojis mat lagao.",
                messages = _history
            };

            var response = await _httpClient.PostAsync("/v1/messages",
                new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var reply = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? "...";

            _history.Add(new { role = "assistant", content = reply });
            if (_history.Count > 20) _history.RemoveRange(0, 2);

            return reply;
        }

        public void Dispose() => _httpClient?.Dispose();
    }
}