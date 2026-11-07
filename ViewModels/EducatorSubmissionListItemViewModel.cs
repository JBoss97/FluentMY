using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FluentMY.ViewModels
{
    public class EducatorSubmissionListItemViewModel
    {
        public int SubmissionId { get; set; }
        public int? LessonSessionId { get; set; }

        public string ActivityType { get; set; }   // Quiz / Game Play
        public string StudentName { get; set; }

        public int AttemptNumber { get; set; }
        public int CorrectAnswers { get; set; }
        public int TotalQuestions { get; set; }
        public int TotalScore { get; set; }
        public int Percentage { get; set; }

        public bool IsReviewed { get; set; }
        public bool IsReviewable { get; set; }

        public DateTime SubmittedAt { get; set; }
    }
}