using NewsClassTrainer.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NewsClassTrainer
{
    class Program
    {

        static void Main(string[] args)
        {
            var dataToTrain = TrainDataManager.GetDataToTrain();

            if (!dataToTrain.Any())
            {
                return;
            }

            var trainingData = TrainDataManager.GetTrainingDataList();

            if(!trainingData.Any())
            {
                return;
            }

            dataToTrain = dataToTrain.Where(a => !trainingData.Any(t => t.Title == a.Title)).ToList();

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

                    var category = Convert.ToInt32(Console.ReadLine());

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

            if(!newTrainData.Any())
            {
                return;
            }

            trainingData.AddRange(newTrainData);

            using (StreamWriter file = File.CreateText(TrainDataManager.TrainDataFilePath))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, trainingData);
            }
        }
    }

}
