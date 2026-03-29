using Microsoft.ML;
using SentimentChat.Models;
namespace SentimentChat.Services
{
    public class SentimentService
    {
        private readonly MLContext _mlContext;
        private PredictionEngine<SentimentInput, SentimentPrediction> _predEngine;
        public SentimentService()
        {
            _mlContext = new MLContext(seed: 42);
            TrainModel();
        }

        private void TrainModel()
        {
            string dataPath = Path.Combine(AppContext.BaseDirectory, "Data", "data.csv");

            if (!File.Exists(dataPath))
                throw new FileNotFoundException("data.csv nahi mila!", dataPath);

            var dataView = _mlContext.Data.LoadFromTextFile<SentimentInput>(
                dataPath, hasHeader: true, separatorChar: ',');

            var split = _mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);

            var pipeline = _mlContext.Transforms.Conversion
                .MapValueToKey("Label")
                .Append(_mlContext.Transforms.Text.FeaturizeText(
                    outputColumnName: "Features",
                    inputColumnName: nameof(SentimentInput.Text)))
                .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(
                    labelColumnName: "Label",
                    featureColumnName: "Features"))
                .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            var model = pipeline.Fit(split.TrainSet);
            _predEngine = _mlContext.Model
                .CreatePredictionEngine<SentimentInput, SentimentPrediction>(model);
        }

        public (string sentiment, string emoji, float confidence) Analyze(string text)
        {
            var result = _predEngine.Predict(new SentimentInput { Text = text });
            float confidence = result.Score?.Max() ?? 0f;

            string emoji = result.PredictedLabel switch
            {
                "Positive" => "😊",
                "Negative" => "😡",
                "Neutral" => "😌",
                _ => "🤔"
            };

            return (result.PredictedLabel ?? "Neutral", emoji, confidence);
        }
    }
}