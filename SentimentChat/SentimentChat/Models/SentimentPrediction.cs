using Microsoft.ML.Data;
namespace SentimentChat.Models
{
    public class SentimentPrediction
    {
        [ColumnName("PredictedLabel")]
        public string PredictedLabel { get; set; } = string.Empty;
        public float[] Score { get; set; } = Array.Empty<float>();
    }
}
