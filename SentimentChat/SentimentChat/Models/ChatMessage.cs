namespace SentimentChat.Models
{
    public class ChatMessage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Sender { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Sentiment { get; set; } = string.Empty;
        public string Emoji { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}