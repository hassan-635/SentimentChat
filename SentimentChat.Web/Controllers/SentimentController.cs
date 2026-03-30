using Microsoft.AspNetCore.Mvc;
using SentimentChat.Web.Models;
using SentimentChat.Web.Services;

namespace SentimentChat.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SentimentController : ControllerBase
    {
        private readonly SentimentService _sentimentService;
        
        public SentimentController(SentimentService sentimentService)
        {
            _sentimentService = sentimentService;
        }

        [HttpPost("analyze")]
        public ActionResult<SentimentResult> AnalyzeSentiment([FromBody] SentimentRequest request)
        {
            try
            {
                var (sentiment, emoji, confidence) = _sentimentService.Analyze(request.Text);
                
                return Ok(new SentimentResult
                {
                    Sentiment = sentiment,
                    Emoji = emoji,
                    Confidence = confidence,
                    Text = request.Text
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }

    public class SentimentRequest
    {
        public string Text { get; set; } = string.Empty;
    }

    public class SentimentResult
    {
        public string Text { get; set; } = string.Empty;
        public string Sentiment { get; set; } = string.Empty;
        public string Emoji { get; set; } = string.Empty;
        public float Confidence { get; set; }
    }
}
