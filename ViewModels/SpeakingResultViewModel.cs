using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FluentMY.ViewModels
{
    public class SpeakingResultViewModel
    {
        public int SpeakingAttemptId { get; set; }
        public int ExerciseId { get; set; }
        public int LessonId { get; set; }

        public int AttemptNumber { get; set; }

        public string LessonTitle { get; set; }
        public string CourseTitle { get; set; }

        public string RecognizedText { get; set; }
        public string ReferenceText { get; set; }

        public decimal? AccuracyScore { get; set; }
        public decimal? FluencyScore { get; set; }
        public decimal? CompletenessScore { get; set; }
        public decimal? PronunciationScore { get; set; }

        public string FeedbackJson { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}