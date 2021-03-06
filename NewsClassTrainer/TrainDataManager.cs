﻿using NewsClassTrainer.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using X.Web.RSS;
using X.Web.RSS.Structure;

namespace NewsClassTrainer
{
   //todo: replace pirate entries with 'terror'
    public enum Categories
    {
        mil_crisis = 1,
        pol_crisis,
        dip_crisis,
        econ_crisis,
        terror,
        nat_desaster,
        accident,
        rights,
        elections,
        protests,
        spy,
        social,
        diplomacy,
        health_crisis,
        military,
        environment,
        corruption,
        econ_develop,
        human_crisis,
        justice,
        crime,
        politics,
        unrecognized
    };

    /// <summary>
    /// This class provides tools to get new rss feeds and retrieve the existing train data
    /// </summary>
    public class TrainDataManager
    {
        private static string FeedsFilePath
        {
            get
            {
                return Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "assets/feeds.json");
            }
        }

        private static string BackupFilePath
        {
            get
            {
                return Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "assets/backup-train-data.json");
            }
        }

        public static string NewsDataFilePath
        {
            get
            {
                return Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "assets/train-data.json");
            }
        }

        public static string MergedNewsDataFilePath
        {
            get
            {
                return Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "assets/merged-train-data.json");
            }
        }

        public static string ModelPath
        {
            get
            {
                return Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "news-model.txt");
            }
        }

        public static string TrainingSetPath
        {
            get
            {
                return Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "train-set.txt");
            }
        }

        public static string TestingSetPath
        {
            get
            {
                return Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "test-set.txt");
            }
        }

        /// <summary>
        /// Retrieves the list of rss feeds fron json file
        /// </summary>
        /// <returns></returns>
        private static List<FeedUrl> GetFeeds()
        {
            var feeds = new List<FeedUrl>();

            if (!File.Exists(FeedsFilePath))
            {
                return feeds;
            }

            using (StreamReader r = new StreamReader(FeedsFilePath))
            {
                string json = r.ReadToEnd();
                if (!string.IsNullOrEmpty(json))
                {
                    feeds = JsonConvert.DeserializeObject<List<FeedUrl>>(json);
                }
            }

            return feeds;
        }

        public static List<FeedTrainData> GetTrainingDataFromFile(string filePath)
        {
            var trainData = new List<FeedTrainData>();

            if (!File.Exists(filePath))
            {
                return trainData;
            }

            using (StreamReader r = new StreamReader(filePath))
            {
                string json = r.ReadToEnd();
                if (!string.IsNullOrEmpty(json))
                {
                    trainData = JsonConvert.DeserializeObject<List<FeedTrainData>>(json);
                }
            }

            return trainData;
        }

        /// <summary>
        /// Retrieves the rss items from the rss feeds
        /// </summary>
        /// <returns></returns>
        public static List<RssItem> GetDataToTrain()
        {
            var newsItems = new List<RssItem>();

            var feeds = GetFeeds();

            if (!feeds.Any())
            {
                return newsItems;
            }

            foreach (var feed in feeds)
            {
                var request = WebRequest.Create(feed.Url);
                var response = request.GetResponse();
                var stream = response.GetResponseStream();
                var rss = RssDocument.Load(stream);

                if (rss == null || !rss.Channel.Items.Any())
                {
                    continue;
                }

                foreach (var item in rss.Channel.Items)
                {
                    item.Category = new RssCategory { Domain = rss.Channel.Title };
                }

                newsItems.AddRange(rss.Channel.Items);
            }

            return newsItems;
        }

        /// <summary>
        /// Retrieves the list of already prepared data from the json file
        /// </summary>
        /// <returns></returns>
        public static List<FeedTrainData> GetTrainingDataList()
        {
            var trainData = GetTrainingDataFromFile(NewsDataFilePath);

            if (trainData.Any())
            {
                using (StreamWriter file = File.CreateText(BackupFilePath))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(file, trainData);
                }
            }

            return trainData;
        }

        /// <summary>
        /// Prepares the data for the model training
        /// </summary>
        public static void PrepareData()
        {
            File.Delete(TrainingSetPath);
            File.Delete(TestingSetPath);

            var training = new List<NewsData>();
            var test = new List<NewsData>();
            var trainData = GetTrainingDataList();
            trainData = trainData.Where(d => !string.Equals(d.Category,
                                                "unrecognized",
                                                StringComparison.InvariantCultureIgnoreCase)).ToList();

            foreach (var i in Enum.GetValues(typeof(Categories)))
            {
                var name = Enum.GetName(typeof(Categories), i);
                var categoryData = trainData.Where(td => td.Category == name).ToList();

                if (!categoryData.Any())
                {
                    var data = new NewsData { Text = "Dummy data", Label = name };
                    training.Add(data);
                    test.Add(data);

                    continue;
                }

                var trainingTextsCount = Convert.ToInt32(((double)categoryData.Count / 100) * 80);
                var trainingTexts = categoryData.GetRange(0, trainingTextsCount);
                training.AddRange(trainingTexts.Select(s => new NewsData { Text = s.Title + " " + s.Description, Label = s.Category }).ToList());

                var testTexts = categoryData.GetRange(trainingTextsCount, categoryData.Count - trainingTextsCount);
                test.AddRange(testTexts.Select(s => new NewsData { Text = s.Title + " " + s.Description, Label = s.Category }).ToList());

            }

            File.AppendAllLines(TestingSetPath, test.Select(s => $"{s.Text}\t{s.Label}"));
            File.AppendAllLines(TrainingSetPath, training.Select(s => $"{s.Text}\t{s.Label}"));

        }

        /// <summary>
        /// Persists the tr
        /// </summary>
        /// <param name="trainingData"></param>
        public static void PersistTrainingData(List<FeedTrainData> trainingData, string filePath)
        {
            if (trainingData == null || !trainingData.Any())
            {
                return;
            }

            //var dataToPersist = trainingData.Where(i => !i.Category.Equals("unrecognized", StringComparison.InvariantCultureIgnoreCase));

            if (trainingData.Any())
            {
                using (StreamWriter file = File.CreateText(filePath))
                {
                    var serializer = new JsonSerializer();
                    serializer.Serialize(file, trainingData);
                }
            }
        }
    }
}
