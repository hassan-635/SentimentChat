using Microsoft.ML.Data;
namespace SentimentChat.Models
{
    public class SentimentInput
    {
        [LoadColumn(0)]
        public string Text { get; set; } = string.Empty;
        [LoadColumn(1)]
        [ColumnName("Label")]
        public string Label { get; set; } = string.Empty;
    }
}