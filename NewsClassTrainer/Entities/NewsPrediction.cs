using Microsoft.ML.Runtime.Api;

namespace NewsClassTrainer.Entities
{
    public class NewsPrediction
    {
        [ColumnName("Score")]
        public float[] Score;
    }
}
