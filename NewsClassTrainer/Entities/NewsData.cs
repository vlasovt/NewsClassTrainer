using Microsoft.ML.Runtime.Api;

namespace NewsClassTrainer.Entities
{
    public class NewsData
    {
        [Column(ordinal: "0")]
        public string Text;

        [Column(ordinal: "1", name: "Label")]
        public string Label;
    }
}
