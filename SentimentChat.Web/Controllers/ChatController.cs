using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SentimentChat.Web.Models;
using SentimentChat.Web.Services;
using SentimentChat.Web.Hubs;

namespace SentimentChat.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly BotService _botService;
        private readonly SentimentService _sentimentService;
        private readonly IHubContext<ChatHub> _hubContext;
        
        public ChatController(BotService botService, SentimentService sentimentService, IHubContext<ChatHub> hubContext)
        {
            _botService = botService;
            _sentimentService = sentimentService;
            _hubContext = hubContext;
        }

        [HttpPost("message")]
        public async Task<ActionResult<ChatMessage>> SendMessage([FromBody] ChatMessage message)
        {
            try
            {
                message.Timestamp = DateTime.UtcNow;
                
                // Analyze sentiment of user message
                var (sentiment, emoji, confidence) = _sentimentService.Analyze(message.Text);
                message.Sentiment = sentiment;
                message.SentimentEmoji = emoji;
                message.Confidence = confidence;
                
                // Send user message to all screens
                await _hubContext.Clients.All.SendAsync("ReceiveMessage", message);
                
                // Generate sentiment-based AI response
                var botReply = await GetSentimentBasedResponse(message.Text, sentiment);
                var (botSentiment, botEmoji, botConfidence) = _sentimentService.Analyze(botReply);
                
                var botMessage = new ChatMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    Text = botReply,
                    Sender = "Bot",
                    Timestamp = DateTime.UtcNow,
                    Sentiment = botSentiment,
                    SentimentEmoji = botEmoji,
                    Confidence = botConfidence,
                    OriginalMessage = message.Text
                };
                
                // Send bot response to all screens
                await _hubContext.Clients.All.SendAsync("ReceiveMessage", botMessage);
                
                return Ok(message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        private async Task<string> GetSentimentBasedResponse(string userMessage, string sentiment)
        {
            try
            {
                // Create sentiment-specific system prompts
                string systemPrompt = sentiment switch
                {
                    "Positive" => "You are a very positive and enthusiastic AI assistant. Respond with positive energy, optimism, and encouragement. Keep responses short and uplifting.",
                    "Negative" => "You are an empathetic and understanding AI assistant. Respond with compassion, support, and gentle encouragement. Keep responses short and comforting.",
                    "Neutral" => "You are a calm and balanced AI assistant. Respond with neutrality, objectivity, and helpful information. Keep responses short and informative.",
                    _ => "You are a friendly AI assistant. Respond naturally and helpfully. Keep responses short."
                };

                // Create a custom bot request with sentiment-specific prompt
                var request = new
                {
                    model = "claude-3-haiku-20240307",
                    max_tokens = 300,
                    system = systemPrompt,
                    messages = new[]
                    {
                        new { role = "user", content = userMessage }
                    }
                };

                // For now, return sentiment-based fallback responses
                // You can integrate the actual Claude API call here
                return sentiment switch
                {
                    "Positive" => GetPositiveResponse(userMessage),
                    "Negative" => GetNegativeResponse(userMessage),
                    "Neutral" => GetNeutralResponse(userMessage),
                    _ => "Thanks for your message! How can I help you today?"
                };
            }
            catch (Exception ex)
            {
                // Fallback response if AI service fails
                return $"I understand you're feeling {sentiment.ToLower()}. I'm here to help!";
            }
        }

        private string GetPositiveResponse(string userMessage)
        {
            var responses = new[]
            {
                "That's wonderful! I love your positive energy! 😊",
                "Amazing! Your positivity is inspiring! 🌟",
                "Fantastic! Keep that great attitude going! 💫",
                "Wonderful! I love your enthusiasm! 🎉",
                "That's great! Your positive vibes are contagious! 😄"
            };
            
            return responses[new Random().Next(responses.Length)];
        }

        private string GetNegativeResponse(string userMessage)
        {
            var responses = new[]
            {
                "I understand you're feeling down. I'm here to support you. 🤗",
                "That sounds tough. You're not alone in this. 💙",
                "I hear your frustration. Let's work through this together. 🫂",
                "That sounds difficult. Remember, tough times pass. 🌈",
                "I understand your concerns. We'll figure this out step by step. 🤝"
            };
            
            return responses[new Random().Next(responses.Length)];
        }

        private string GetNeutralResponse(string userMessage)
        {
            var responses = new[]
            {
                "I understand. Let me help you with that. 📋",
                "That's interesting. Tell me more about it. 🤔",
                "I see. How can I assist you further? 🛠️",
                "Got it. Let's approach this systematically. 📊",
                "I understand. Here's what I can help with. 💡"
            };
            
            return responses[new Random().Next(responses.Length)];
        }
    }
}
