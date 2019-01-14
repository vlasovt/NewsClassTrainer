using NewsClassTrainer.Entities;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using X.Web.RSS;
using X.Web.RSS.Structure;

namespace NewsClassTrainer
{
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
        pirate = 14,
        health_crisis = 15,
        military = 16,
        environment = 17,
        corruption = 18,
        econ_develop = 19,
        human_crisis = 20,
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

        public static string TrainDataFilePath
        {
            get
            {
                return Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "assets/train-data.json");
            }
        }

        private static string BackupFilePath
        {
            get
            {
                return Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "assets/backup-train-data.json");
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
    }
}
