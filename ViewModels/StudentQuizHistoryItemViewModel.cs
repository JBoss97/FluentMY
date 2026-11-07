using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FluentMY.ViewModels
{
    public class StudentQuizHistoryItemViewModel
    {
        public int AttemptId { get; set; }
        public int LessonId { get; set; }
        public string LessonTitle { get; set; }
        public string CourseTitle { get; set; }
        public int AttemptNumber { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public int TotalScore { get; set; }
        public int Percentage { get; set; }
        public DateTime SubmittedAt { get; set; }
    }
}