using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FluentMY.ViewModels
{
    public class EducatorGamePlaySessionDetailViewModel
    {
        public int LessonSessionId { get; set; }
        public string StudentName { get; set; }
        public string CourseTitle { get; set; }
        public string LessonTitle { get; set; }

        public int CorrectAnswers { get; set; }
        public int WrongAnswers { get; set; }
        public int TotalQuestions { get; set; }
        public int TotalXP { get; set; }
        public int Percentage { get; set; }
        public DateTime SubmittedAt { get; set; }

        public List<EducatorGamePlaySessionAnswerItemViewModel> Answers { get; set; }
            = new List<EducatorGamePlaySessionAnswerItemViewModel>();
    }

    public class EducatorGamePlaySessionAnswerItemViewModel
    {
        public string PromptText { get; set; }
        public string SubmittedAnswer { get; set; }
        public string SubmittedAnswerJson { get; set; }
        public bool IsCorrect { get; set; }
        public int XPGranted { get; set; }
    }
}