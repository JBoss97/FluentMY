using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FluentMY.ViewModels
{
    public class StudentSubmissionHistoryItemViewModel
    {
        public int RecordId { get; set; }
        public int LessonId { get; set; }

        public string CourseTitle { get; set; }
        public string LessonTitle { get; set; }

        public string ExerciseType { get; set; }   // Quiz / Speaking
        public int AttemptNumber { get; set; }
        public DateTime SubmittedAt { get; set; }

        public int? TotalQuestions { get; set; }
        public int? CorrectAnswers { get; set; }
        public int? TotalScore { get; set; }
        public int? Percentage { get; set; }

        public decimal? AccuracyScore { get; set; }
        public decimal? FluencyScore { get; set; }
        public decimal? CompletenessScore { get; set; }
        public decimal? PronunciationScore { get; set; }

        public string SummaryText { get; set; }
    }
}