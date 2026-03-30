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
                message.Sender = "User";
                
                var (sentiment, emoji, confidence) = _sentimentService.Analyze(message.Text);
                message.Sentiment = sentiment;
                message.SentimentEmoji = emoji;
                message.Confidence = confidence;
                
                await _hubContext.Clients.All.SendAsync("ReceiveMessage", message);
                
                var botReply = await _botService.GetBotReplyAsync(message.Text);
                var (botSentiment, botEmoji, botConfidence) = _sentimentService.Analyze(botReply);
                
                var botMessage = new ChatMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    Text = botReply,
                    Sender = "Bot",
                    Timestamp = DateTime.UtcNow,
                    Sentiment = botSentiment,
                    SentimentEmoji = botEmoji,
                    Confidence = botConfidence
                };
                
                await _hubContext.Clients.All.SendAsync("ReceiveMessage", botMessage);
                
                return Ok(message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
