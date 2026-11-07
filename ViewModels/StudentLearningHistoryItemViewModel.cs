using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FluentMY.ViewModels
{
    public class StudentLearningHistoryItemViewModel
    {
        public int RecordId { get; set; }
        public int LessonId { get; set; }
        public int? ExerciseId { get; set; }

        public string CourseTitle { get; set; }
        public string LessonTitle { get; set; }
        public string PromptText { get; set; }

        public string ExerciseType { get; set; }   // Quiz / Speaking / Game Play
        public int AttemptNumber { get; set; }
        public DateTime AttemptedAt { get; set; }

        // Quiz / Game Play fields
        public int? TotalQuestions { get; set; }
        public int? CorrectAnswers { get; set; }
        public int? TotalScore { get; set; }
        public int? Percentage { get; set; }
        public int? TotalXP { get; set; }

        // Speaking fields
        public string RecognizedText { get; set; }
        public decimal? AccuracyScore { get; set; }
        public decimal? FluencyScore { get; set; }
        public decimal? CompletenessScore { get; set; }
        public decimal? PronunciationScore { get; set; }

        // UI helper
        public string SummaryText { get; set; }
        public string ResultUrl { get; set; }
    }
}