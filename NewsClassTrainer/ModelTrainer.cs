using Microsoft.ML;
using Microsoft.ML.Models;
using Microsoft.ML.Runtime;
using Microsoft.ML.Trainers;
using Microsoft.ML.Transforms;
using NewsClassTrainer.Entities;
using System;

namespace NewsClassTrainer
{
    public class ModelTrainer
    {
        public static PredictionModel<NewsData, NewsPrediction> Train()
        {
            var pipeline = new LearningPipeline
            {
                new TextLoader<NewsData>(TrainDataManager.TrainingSetPath, useHeader: false, separator: "tab"),
                new TextFeaturizer("Features", "Text")
                {
                    KeepDiacritics = false,
                    KeepPunctuations = false,
                    TextCase = TextNormalizerTransformCaseNormalizationMode.Lower,
                    OutputTokens = true,
                    Language = TextTransformLanguage.English,
                    StopWordsRemover = new PredefinedStopWordsRemover(),
                    VectorNormalizer = TextTransformTextNormKind.L2,
                    CharFeatureExtractor = new NGramNgramExtractor() { NgramLength = 3, AllLengths = false },
                    WordFeatureExtractor = new NGramNgramExtractor() { NgramLength = 3, AllLengths = true }
                },
                new Dictionarizer("Label"),
                new StochasticDualCoordinateAscentClassifier()
            };
            return pipeline.Train<NewsData, NewsPrediction>();
        }

        public static void Evaluate(PredictionModel<NewsData, NewsPrediction> model)
        {
            var testData = new TextLoader<NewsData>(TrainDataManager.TrainingSetPath, useHeader: false, separator: "tab");
            var evaluator = new ClassificationEvaluator();
            var metrics = evaluator.Evaluate(model, testData);

            Console.WriteLine();
            Console.WriteLine("PredictionModel quality metrics evaluation");
            Console.WriteLine("------------------------------------------");
            Console.WriteLine($"AccuracyMacro: {metrics.AccuracyMacro:P2}");
            Console.WriteLine($"AccuracyMicro: {metrics.AccuracyMicro:P2}");
            Console.WriteLine($"LogLoss: {metrics.LogLoss:P2}");
        }
    }
}
