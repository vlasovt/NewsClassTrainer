using NewsClassTrainer.Entities;
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
        pol_crisis = 2,
        dip_crisis = 3,
        econ_crisis = 4,
        terror = 5,
        nat_desaster = 6,
        accident = 7,
        rights = 8,
        elections = 9,
        protests = 10,
        spy = 11,
        social = 12,
        diplomacy = 13,
        health_crisis = 14,
        military = 15,
        environment = 16,
        corruption = 17,
        econ_develop = 18,
        human_crisis = 19,
        unrecognized = 99
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

        public static string TrainDataFilePath
        {
            get
            {
                return Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "assets/train-data.json");
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

                foreach(var item in rss.Channel.Items)
                {
                    item.Category = new RssCategory {Domain = rss.Channel.Title};
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
            var trainData = new List<FeedTrainData>();

            if (!File.Exists(TrainDataFilePath))
            {
                return trainData;
            }

            using (StreamReader r = new StreamReader(TrainDataFilePath))
            {
                string json = r.ReadToEnd();
                if (!string.IsNullOrEmpty(json))
                {
                    trainData = JsonConvert.DeserializeObject<List<FeedTrainData>>(json);
                }
            }

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

            var trainingTextsCount = (trainData.Count / 100) * 80;
            var trainingTexts = trainData.GetRange(0, trainingTextsCount);
            training.AddRange(trainingTexts.Select(s => new NewsData { Text = s.Title + " " + s.Description, Label = s.Category }).ToList());

            var testTexts = trainData.GetRange(trainingTextsCount, trainData.Count - trainingTextsCount);
            test.AddRange(testTexts.Select(s => new NewsData { Text = s.Title + " " + s.Description, Label = s.Category }).ToList());

            File.AppendAllLines(TestingSetPath, test.Select(s => $"{s.Text}\t{s.Label}"));
            File.AppendAllLines(TrainingSetPath, training.Select(s => $"{s.Text}\t{s.Label}"));
        }
    }
}
