using System;
using System.Collections.Generic;

namespace FluentMY.ViewModels
{
    public class AdminReportsViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalStudents { get; set; }
        public int TotalEducators { get; set; }

        // Quiz / gameplay review workload
        public int PendingQuizReviewCount { get; set; }

        public int TotalCourses { get; set; }
        public int TotalLessons { get; set; }
        public int TotalExercises { get; set; }

        // Split activity counts
        public int TotalQuizSubmissions { get; set; }
        public int TotalSpeakingAttempts { get; set; }
        public int TotalLearningActivities { get; set; }
        public int TotalGamePlaySessions { get; set; }

        public List<AdminCoursePerformanceViewModel> CoursePerformance { get; set; } = new List<AdminCoursePerformanceViewModel>();
        public List<AdminLessonPerformanceViewModel> LessonPerformance { get; set; } = new List<AdminLessonPerformanceViewModel>();
        public List<AdminRecentLearningActivityViewModel> RecentActivities { get; set; } = new List<AdminRecentLearningActivityViewModel>();
        public List<AdminTopStudentViewModel> TopStudents { get; set; } = new List<AdminTopStudentViewModel>();
    }

    public class AdminCoursePerformanceViewModel
    {
        public int CourseId { get; set; }
        public string CourseTitle { get; set; }

        public int QuizSubmissionCount { get; set; }
        public int SpeakingAttemptCount { get; set; }
        public int TotalLearningActivities { get; set; }

        public double AverageQuizPercentage { get; set; }
        public double AverageSpeakingOverallScore { get; set; }
        public double OverallPerformanceScore { get; set; }

        public int EnrolledStudents { get; set; }
        public int CompletedStudents { get; set; }
    }

    public class AdminLessonPerformanceViewModel
    {
        public int LessonId { get; set; }
        public string LessonTitle { get; set; }
        public string CourseTitle { get; set; }

        public int QuizSubmissionCount { get; set; }
        public int SpeakingAttemptCount { get; set; }
        public int TotalLearningActivities { get; set; }

        public double AverageQuizPercentage { get; set; }
        public double AverageSpeakingOverallScore { get; set; }
        public double OverallPerformanceScore { get; set; }

        public bool HasSpeakingActivity { get; set; }
        public bool NeedsAttention { get; set; }
    }

    public class AdminRecentLearningActivityViewModel
    {
        public int RecordId { get; set; }
        public string ActivityType { get; set; }   // Quiz / Game Play / Speaking

        public string StudentName { get; set; }
        public string CourseTitle { get; set; }
        public string LessonTitle { get; set; }
        public string PromptText { get; set; }

        // Quiz fields
        public int? Percentage { get; set; }
        public bool? IsReviewed { get; set; }

        // Speaking fields
        public decimal? AccuracyScore { get; set; }
        public decimal? FluencyScore { get; set; }
        public decimal? CompletenessScore { get; set; }
        public decimal? PronunciationScore { get; set; }
        public int? SpeakingOverallScore { get; set; }

        public DateTime AttemptedAt { get; set; }
    }

    public class AdminTopStudentViewModel
    {
        public string StudentId { get; set; }
        public string StudentName { get; set; }

        public int QuizSubmissionCount { get; set; }
        public int SpeakingAttemptCount { get; set; }
        public int TotalLearningActivities { get; set; }

        public double AverageQuizPercentage { get; set; }
        public double AverageSpeakingOverallScore { get; set; }
        public double OverallPerformanceScore { get; set; }

        public int CompletedCourses { get; set; }
    }
}