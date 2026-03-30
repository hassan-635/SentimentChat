namespace SentimentChat.Web.Models
{
    public class ChatMessage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Text { get; set; } = string.Empty;
        public string Sender { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Sentiment { get; set; } = string.Empty;
        public string SentimentEmoji { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public string OriginalMessage { get; set; } = string.Empty;
    }
}
