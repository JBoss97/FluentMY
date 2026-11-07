using System;
using System.Collections.Generic;

namespace FluentMY.ViewModels
{
    public class SubmissionResultViewModel
    {
        public int SubmissionId { get; set; }
        public int LessonId { get; set; }

        public string LessonTitle { get; set; }
        public string CourseTitle { get; set; }
        public string StudentName { get; set; }

        public int AttemptNumber { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public int TotalScore { get; set; }
        public int Percentage { get; set; }
        public DateTime SubmittedAt { get; set; }

        public bool IsReviewed { get; set; }
        public DateTime? ReviewedAt { get; set; }

        public List<SubmissionAnswerResultViewModel> Answers { get; set; } = new List<SubmissionAnswerResultViewModel>();
        public List<FeedbackItemViewModel> Feedbacks { get; set; } = new List<FeedbackItemViewModel>();
    }

    public class SubmissionAnswerResultViewModel
    {
        public string QuestionText { get; set; }
        public string SelectedAnswer { get; set; }
        public string CorrectAnswer { get; set; }
        public bool IsCorrect { get; set; }
        public int Score { get; set; }
    }

    public class FeedbackItemViewModel
    {
        public string FeedbackType { get; set; }
        public string AuthorName { get; set; }
        public string CommentText { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
