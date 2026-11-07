using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FluentMY.ViewModels
{
    public class SpeakingHistoryItemViewModel
    {
        public int SpeakingAttemptId { get; set; }
        public int ExerciseId { get; set; }
        public int LessonId { get; set; }

        public string LessonTitle { get; set; }
        public string CourseTitle { get; set; }
        public string PromptText { get; set; }

        public int AttemptNumber { get; set; }
        public string RecognizedText { get; set; }

        public decimal? AccuracyScore { get; set; }
        public decimal? FluencyScore { get; set; }
        public decimal? CompletenessScore { get; set; }
        public decimal? PronunciationScore { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}