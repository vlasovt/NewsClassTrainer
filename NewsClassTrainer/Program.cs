using Microsoft.ML;
using NewsClassTrainer.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace NewsClassTrainer
{
    class Program
    {
        static void Main(string[] args)
        {
            while(true)
            {
                ShowMainMenu();

                var selectedOption = 0;

                if (!int.TryParse(Console.ReadLine(), out selectedOption))
                {
                    return;
                }

                switch (selectedOption)
                {
                    case 1:
                        GetNewsData();
                        break;
                    case 2:
                        TrainModel();
                        break;
                    case 3:
                        MergeNewsData();
                        break;
                    default:
                        break;
                }
            }
            
        }

        private static void ShowMainMenu()
        {
            Console.Clear();
            Console.WriteLine("Select an option:");
            Console.WriteLine("");
            Console.WriteLine("1. Retrieve news data");
            Console.WriteLine("2. Train the model");
            Console.WriteLine("3. Merge news data files");
            Console.WriteLine("4. Quit");
        }

        private static void GetNewsData()
        {
            var dataToTrain = TrainDataManager.GetDataToTrain();

            if (!dataToTrain.Any())
            {
                return;
            }

            var trainingData = TrainDataManager.GetTrainingDataList();

            if (trainingData.Any())
            {
                dataToTrain = dataToTrain.Where(a => !trainingData.Any(t => t.Title == a.Title)).ToList();
            }

            var newTrainData = new List<FeedTrainData>();

            foreach (var item in dataToTrain)
            {
                try
                {
                    var description = item.Description;

                    if (!string.IsNullOrEmpty(description)
                        && description.IndexOf('<') > 0)
                    {
                        description = description.Substring(0, description.IndexOf('<'));
                    }

                    Console.Clear();
                    Console.WriteLine(item.Category.Domain);
                    Console.WriteLine(item.Title);
                    Console.WriteLine(description);
                    Console.WriteLine("");

                    foreach (int i in Enum.GetValues(typeof(Categories)))
                    {
                        var name = Enum.GetName(typeof(Categories), i);

                        Console.WriteLine(string.Format("{0} - {1}; ", i, name));
                    }

                    Console.WriteLine("");

                    var category = 0;

                    if (!int.TryParse(Console.ReadLine(), out category))
                    {
                        break;
                    }

                    if (category == 0)
                    {
                        continue;
                    }

                    newTrainData.Add(
                        new FeedTrainData
                        {
                            Title = item.Title,
                            Description = description,
                            Category = Enum.GetName(typeof(Categories), category)
                        }
                    );
                }
                catch (Exception ex)
                {
                    break;
                }
            }

            if (!newTrainData.Any())
            {
                return;
            }

            trainingData.AddRange(newTrainData);
            TrainDataManager.PersistTrainingData(trainingData, TrainDataManager.NewsDataFilePath);
        }

        private static void TrainModel()
        {
            PredictionModel<NewsData, NewsPrediction> model = null;
            if (File.Exists(TrainDataManager.ModelPath))
            {
                model = PredictionModel.ReadAsync<NewsData, NewsPrediction>(TrainDataManager.ModelPath).Result;
            }

            if (model == null)
            {
                TrainDataManager.PrepareData();
                model = ModelTrainer.Train();
                model.WriteAsync(TrainDataManager.ModelPath).Wait();
            }

            ModelTrainer.Evaluate(model);

            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("Input text: (type Exit to quit)");
                var text = Console.ReadLine();

                if (text == "Exit")
                {
                    return;
                }

                var prediction = model.Predict(new NewsData { Text = text });

                Console.WriteLine("Prediction result:");

                var results = new Dictionary<string, float>();

                for (var i = 1; i <= prediction.Score.Count(); i++)
                {
                    results.Add(Enum.GetName(typeof(Categories), i), prediction.Score[i - 1]);
                }

                foreach(var result in results.OrderByDescending(s=> s.Value).Take(3))
                {
                    Console.WriteLine($"{result.Key}: {result.Value:P2}");
                }
            }
        }

        private static void MergeNewsData()
        {
            var trainingData = TrainDataManager.GetTrainingDataList();
            var trainigList2 = TrainDataManager.GetTrainingDataFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "new-train-data.json"));
            var mergedList = new List<FeedTrainData>();


            mergedList = trainigList2.Where(a => !trainingData.Any(t => t.Title == a.Title)).ToList();
            mergedList.AddRange(trainingData);

            TrainDataManager.PersistTrainingData(mergedList, TrainDataManager.MergedNewsDataFilePath);
        }
    }

}
