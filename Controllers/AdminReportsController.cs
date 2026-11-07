using FluentMY.Models;
using FluentMY.ViewModels;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace FluentMY.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminReportsController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        private async Task<AdminReportsViewModel> BuildReportsViewModelAsync()
        {
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));

            var users = await db.Users
                .AsNoTracking()
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email
                })
                .ToListAsync();

            var studentNameMap = users.ToDictionary(
                u => u.Id,
                u => !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.Email);

            int totalStudents = 0;
            int totalEducators = 0;

            foreach (var user in users)
            {
                var roles = await userManager.GetRolesAsync(user.Id);

                if (roles.Contains("Student"))
                    totalStudents++;

                if (roles.Contains("Educator"))
                    totalEducators++;
            }

            var totalCourses = await db.Courses.CountAsync();
            var totalLessons = await db.Lessons.CountAsync();
            var totalExercises = await db.Exercises.CountAsync();

            var totalQuizSubmissionsOnly = await db.Submissions.CountAsync();
            var totalGamePlaySessions = await db.LessonSessions.CountAsync(x => x.IsCompleted);
            var totalSpeakingAttempts = await db.SpeakingAttempts.CountAsync();
            var pendingQuizReviewCount = await db.Submissions.CountAsync(s => !s.IsReviewed);

            var courseRows = await db.Courses
                .AsNoTracking()
                .OrderBy(c => c.Title)
                .Select(c => new
                {
                    c.CourseId,
                    CourseTitle = c.Title
                })
                .ToListAsync();

            var lessonRows = await db.Lessons
                .AsNoTracking()
                .OrderBy(l => l.Course.Title)
                .ThenBy(l => l.Title)
                .Select(l => new
                {
                    l.LessonId,
                    LessonTitle = l.Title,
                    l.CourseId,
                    CourseTitle = l.Course.Title
                })
                .ToListAsync();

            var speakingLessonIds = new HashSet<int>(
                await db.Exercises
                    .AsNoTracking()
                    .Where(e => e.ExerciseType == "Speaking" || e.ExerciseType == "speaking")
                    .Select(e => e.LessonId)
                    .Distinct()
                    .ToListAsync()
            );

            var enrollmentRows = await db.StudentCourses
                .AsNoTracking()
                .GroupBy(sc => sc.CourseId)
                .Select(g => new
                {
                    CourseId = g.Key,
                    EnrolledStudents = g.Count(),
                    CompletedStudents = g.Sum(x => x.IsCompleted ? 1 : 0)
                })
                .ToListAsync();

            var enrollmentMap = enrollmentRows.ToDictionary(
                x => x.CourseId,
                x => new
                {
                    x.EnrolledStudents,
                    x.CompletedStudents
                });

            var completedCourseRows = await db.StudentCourses
                .AsNoTracking()
                .Where(sc => sc.IsCompleted)
                .GroupBy(sc => sc.StudentId)
                .Select(g => new
                {
                    StudentId = g.Key,
                    CompletedCourses = g.Count()
                })
                .ToListAsync();

            var completedCourseMap = completedCourseRows.ToDictionary(
                x => x.StudentId,
                x => x.CompletedCourses);

            // Quiz submissions
            var quizRows = await db.Submissions
                .AsNoTracking()
                .Select(s => new
                {
                    RecordId = s.SubmissionId,
                    s.StudentId,
                    s.LessonId,
                    CourseId = s.Lesson.CourseId,
                    CourseTitle = s.Lesson.Course.Title,
                    LessonTitle = s.Lesson.Title,
                    Percentage = s.Percentage,
                    IsReviewed = (bool?)s.IsReviewed,
                    AttemptedAt = s.SubmittedAt,
                    ActivityType = "Quiz",
                    PromptText = (string)null
                })
                .ToListAsync();

            // Game play sessions
            var gamePlayRawRows = await db.LessonSessions
                .AsNoTracking()
                .Where(x => x.IsCompleted)
                .Select(x => new
                {
                    RecordId = x.LessonSessionId,
                    x.StudentId,
                    x.LessonId,
                    CourseId = x.Lesson.CourseId,
                    CourseTitle = x.Lesson.Course.Title,
                    LessonTitle = x.Lesson.Title,
                    x.CorrectCount,
                    x.WrongCount,
                    x.TotalXP,
                    AttemptedAt = x.CompletedAtUtc,
                    x.StartedAtUtc
                })
                .ToListAsync();

            var gamePlayRows = gamePlayRawRows
                .Select(x =>
                {
                    var totalAnswered = x.CorrectCount + x.WrongCount;
                    var percentage = CalculateSessionPercentage(x.CorrectCount, x.WrongCount);

                    return new
                    {
                        x.RecordId,
                        x.StudentId,
                        x.LessonId,
                        x.CourseId,
                        x.CourseTitle,
                        x.LessonTitle,
                        Percentage = (int?)percentage,
                        IsReviewed = (bool?)null,
                        AttemptedAt = x.AttemptedAt ?? x.StartedAtUtc,
                        ActivityType = "Game Play",
                        PromptText = "Correct: " + x.CorrectCount + " / " + totalAnswered + " | XP: " + x.TotalXP
                    };
                })
                .ToList();

            // Objective activity = quiz + game play
            var objectiveRows = quizRows
                .Select(x => new
                {
                    x.StudentId,
                    x.LessonId,
                    x.CourseId,
                    Percentage = (double)(x.Percentage)
                })
                .Concat(
                    gamePlayRows.Select(x => new
                    {
                        x.StudentId,
                        x.LessonId,
                        x.CourseId,
                        Percentage = (double)(x.Percentage ?? 0)
                    }))
                .ToList();

            var courseObjectiveMap = objectiveRows
                .GroupBy(x => x.CourseId)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        Count = g.Count(),
                        AveragePercentage = g.Average(x => x.Percentage)
                    });

            var lessonObjectiveMap = objectiveRows
                .GroupBy(x => x.LessonId)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        Count = g.Count(),
                        AveragePercentage = g.Average(x => x.Percentage)
                    });

            var studentObjectiveMap = objectiveRows
                .GroupBy(x => x.StudentId)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        Count = g.Count(),
                        AveragePercentage = g.Average(x => x.Percentage)
                    });

            // Speaking
            var speakingRawRows = await db.SpeakingAttempts
                .AsNoTracking()
                .Select(x => new
                {
                    x.SpeakingAttemptId,
                    x.StudentId,
                    x.LessonId,
                    CourseId = x.Lesson.CourseId,
                    CourseTitle = x.Lesson.Course.Title,
                    LessonTitle = x.Lesson.Title,
                    PromptText = x.Exercise.PromptText,
                    QuestionText = x.Exercise.QuestionText,
                    x.AccuracyScore,
                    x.FluencyScore,
                    x.CompletenessScore,
                    x.PronunciationScore,
                    AttemptedAt = x.CreatedAtUtc
                })
                .ToListAsync();

            var speakingRows = speakingRawRows
                .Select(x => new
                {
                    x.SpeakingAttemptId,
                    x.StudentId,
                    x.LessonId,
                    x.CourseId,
                    x.CourseTitle,
                    x.LessonTitle,
                    PromptText = !string.IsNullOrWhiteSpace(x.PromptText) ? x.PromptText : x.QuestionText,
                    x.AccuracyScore,
                    x.FluencyScore,
                    x.CompletenessScore,
                    x.PronunciationScore,
                    SpeakingOverallScore = CalculateSpeakingOverallScore(
                        x.AccuracyScore,
                        x.FluencyScore,
                        x.CompletenessScore,
                        x.PronunciationScore),
                    x.AttemptedAt
                })
                .ToList();

            var courseSpeakingMap = speakingRows
                .GroupBy(x => x.CourseId)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        Count = g.Count(),
                        AverageSpeakingOverallScore = g.Average(x => x.SpeakingOverallScore)
                    });

            var lessonSpeakingMap = speakingRows
                .GroupBy(x => x.LessonId)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        Count = g.Count(),
                        AverageSpeakingOverallScore = g.Average(x => x.SpeakingOverallScore)
                    });

            var studentSpeakingMap = speakingRows
                .GroupBy(x => x.StudentId)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        Count = g.Count(),
                        AverageSpeakingOverallScore = g.Average(x => x.SpeakingOverallScore)
                    });

            var coursePerformance = courseRows
                .Select(c =>
                {
                    var hasObjective = courseObjectiveMap.ContainsKey(c.CourseId);
                    var hasSpeaking = courseSpeakingMap.ContainsKey(c.CourseId);
                    var hasEnrollment = enrollmentMap.ContainsKey(c.CourseId);

                    var objectiveCount = hasObjective ? courseObjectiveMap[c.CourseId].Count : 0;
                    var speakingCount = hasSpeaking ? courseSpeakingMap[c.CourseId].Count : 0;

                    var avgObjective = hasObjective ? courseObjectiveMap[c.CourseId].AveragePercentage : 0;
                    var avgSpeaking = hasSpeaking ? courseSpeakingMap[c.CourseId].AverageSpeakingOverallScore : 0;

                    return new AdminCoursePerformanceViewModel
                    {
                        CourseId = c.CourseId,
                        CourseTitle = c.CourseTitle,
                        QuizSubmissionCount = objectiveCount,
                        SpeakingAttemptCount = speakingCount,
                        TotalLearningActivities = objectiveCount + speakingCount,
                        AverageQuizPercentage = avgObjective,
                        AverageSpeakingOverallScore = avgSpeaking,
                        OverallPerformanceScore = CombinePerformance(avgObjective, avgSpeaking, hasObjective, hasSpeaking),
                        EnrolledStudents = hasEnrollment ? enrollmentMap[c.CourseId].EnrolledStudents : 0,
                        CompletedStudents = hasEnrollment ? enrollmentMap[c.CourseId].CompletedStudents : 0
                    };
                })
                .ToList();

            var lessonPerformance = lessonRows
                .Select(l =>
                {
                    var hasObjective = lessonObjectiveMap.ContainsKey(l.LessonId);
                    var hasSpeaking = lessonSpeakingMap.ContainsKey(l.LessonId);

                    var objectiveCount = hasObjective ? lessonObjectiveMap[l.LessonId].Count : 0;
                    var speakingCount = hasSpeaking ? lessonSpeakingMap[l.LessonId].Count : 0;

                    var avgObjective = hasObjective ? lessonObjectiveMap[l.LessonId].AveragePercentage : 0;
                    var avgSpeaking = hasSpeaking ? lessonSpeakingMap[l.LessonId].AverageSpeakingOverallScore : 0;

                    var overallScore = CombinePerformance(avgObjective, avgSpeaking, hasObjective, hasSpeaking);
                    var totalActivities = objectiveCount + speakingCount;

                    return new AdminLessonPerformanceViewModel
                    {
                        LessonId = l.LessonId,
                        LessonTitle = l.LessonTitle,
                        CourseTitle = l.CourseTitle,
                        QuizSubmissionCount = objectiveCount,
                        SpeakingAttemptCount = speakingCount,
                        TotalLearningActivities = totalActivities,
                        AverageQuizPercentage = avgObjective,
                        AverageSpeakingOverallScore = avgSpeaking,
                        OverallPerformanceScore = overallScore,
                        HasSpeakingActivity = speakingLessonIds.Contains(l.LessonId),
                        NeedsAttention = totalActivities > 0 && overallScore < 50
                    };
                })
                .ToList();

            var recentActivities = quizRows
                .Select(x => new AdminRecentLearningActivityViewModel
                {
                    RecordId = x.RecordId,
                    ActivityType = x.ActivityType,
                    StudentName = studentNameMap.ContainsKey(x.StudentId) ? studentNameMap[x.StudentId] : "Student",
                    CourseTitle = x.CourseTitle,
                    LessonTitle = x.LessonTitle,
                    PromptText = x.PromptText,
                    Percentage = x.Percentage,
                    IsReviewed = x.IsReviewed,
                    AccuracyScore = null,
                    FluencyScore = null,
                    CompletenessScore = null,
                    PronunciationScore = null,
                    SpeakingOverallScore = null,
                    AttemptedAt = x.AttemptedAt
                })
                .Concat(
                    gamePlayRows.Select(x => new AdminRecentLearningActivityViewModel
                    {
                        RecordId = x.RecordId,
                        ActivityType = x.ActivityType,
                        StudentName = studentNameMap.ContainsKey(x.StudentId) ? studentNameMap[x.StudentId] : "Student",
                        CourseTitle = x.CourseTitle,
                        LessonTitle = x.LessonTitle,
                        PromptText = x.PromptText,
                        Percentage = x.Percentage,
                        IsReviewed = null,
                        AccuracyScore = null,
                        FluencyScore = null,
                        CompletenessScore = null,
                        PronunciationScore = null,
                        SpeakingOverallScore = null,
                        AttemptedAt = x.AttemptedAt
                    }))
                .Concat(
                    speakingRows
                        .Select(x => new AdminRecentLearningActivityViewModel
                        {
                            RecordId = x.SpeakingAttemptId,
                            ActivityType = "Speaking",
                            StudentName = studentNameMap.ContainsKey(x.StudentId) ? studentNameMap[x.StudentId] : "Student",
                            CourseTitle = x.CourseTitle,
                            LessonTitle = x.LessonTitle,
                            PromptText = x.PromptText,
                            Percentage = null,
                            IsReviewed = null,
                            AccuracyScore = x.AccuracyScore,
                            FluencyScore = x.FluencyScore,
                            CompletenessScore = x.CompletenessScore,
                            PronunciationScore = x.PronunciationScore,
                            SpeakingOverallScore = (int)Math.Round(x.SpeakingOverallScore),
                            AttemptedAt = x.AttemptedAt
                        }))
                .OrderByDescending(x => x.AttemptedAt)
                .Take(10)
                .ToList();

            var activeStudentIds = studentObjectiveMap.Keys
                .Union(studentSpeakingMap.Keys)
                .ToList();

            var topStudents = activeStudentIds
                .Select(studentId =>
                {
                    var hasObjective = studentObjectiveMap.ContainsKey(studentId);
                    var hasSpeaking = studentSpeakingMap.ContainsKey(studentId);

                    var objectiveCount = hasObjective ? studentObjectiveMap[studentId].Count : 0;
                    var speakingCount = hasSpeaking ? studentSpeakingMap[studentId].Count : 0;

                    var avgObjective = hasObjective ? studentObjectiveMap[studentId].AveragePercentage : 0;
                    var avgSpeaking = hasSpeaking ? studentSpeakingMap[studentId].AverageSpeakingOverallScore : 0;

                    return new AdminTopStudentViewModel
                    {
                        StudentId = studentId,
                        StudentName = studentNameMap.ContainsKey(studentId) ? studentNameMap[studentId] : "Student",
                        QuizSubmissionCount = objectiveCount,
                        SpeakingAttemptCount = speakingCount,
                        TotalLearningActivities = objectiveCount + speakingCount,
                        AverageQuizPercentage = avgObjective,
                        AverageSpeakingOverallScore = avgSpeaking,
                        OverallPerformanceScore = CombinePerformance(avgObjective, avgSpeaking, hasObjective, hasSpeaking),
                        CompletedCourses = completedCourseMap.ContainsKey(studentId) ? completedCourseMap[studentId] : 0
                    };
                })
                .OrderByDescending(x => x.OverallPerformanceScore)
                .ThenByDescending(x => x.TotalLearningActivities)
                .Take(10)
                .ToList();

            return new AdminReportsViewModel
            {
                TotalUsers = users.Count,
                TotalStudents = totalStudents,
                TotalEducators = totalEducators,
                PendingQuizReviewCount = pendingQuizReviewCount,

                TotalCourses = totalCourses,
                TotalLessons = totalLessons,
                TotalExercises = totalExercises,

                // keep quiz review metrics accurate
                TotalQuizSubmissions = totalQuizSubmissionsOnly,
                TotalGamePlaySessions = totalGamePlaySessions,
                TotalSpeakingAttempts = totalSpeakingAttempts,
                TotalLearningActivities = totalQuizSubmissionsOnly + totalGamePlaySessions + totalSpeakingAttempts,

                CoursePerformance = coursePerformance,
                LessonPerformance = lessonPerformance,
                RecentActivities = recentActivities,
                TopStudents = topStudents
            };
        }

        private static double CalculateSpeakingOverallScore(
            decimal? accuracyScore,
            decimal? fluencyScore,
            decimal? completenessScore,
            decimal? pronunciationScore)
        {
            var scoreParts = new List<double>();

            if (accuracyScore.HasValue)
                scoreParts.Add((double)accuracyScore.Value);

            if (fluencyScore.HasValue)
                scoreParts.Add((double)fluencyScore.Value);

            if (completenessScore.HasValue)
                scoreParts.Add((double)completenessScore.Value);

            if (pronunciationScore.HasValue)
                scoreParts.Add((double)pronunciationScore.Value);

            return scoreParts.Any() ? scoreParts.Average() : 0;
        }

        private static int CalculateSessionPercentage(int correctCount, int wrongCount)
        {
            var totalAnswered = correctCount + wrongCount;

            if (totalAnswered <= 0)
                return 0;

            return (int)Math.Round((correctCount * 100.0) / totalAnswered);
        }

        private static double CombinePerformance(
            double quizAverage,
            double speakingAverage,
            bool hasQuiz,
            bool hasSpeaking)
        {
            var parts = new List<double>();

            if (hasQuiz)
                parts.Add(quizAverage);

            if (hasSpeaking)
                parts.Add(speakingAverage);

            return parts.Any() ? parts.Average() : 0;
        }

        public async Task<ActionResult> Index()
        {
            var vm = await BuildReportsViewModelAsync();
            return View(vm);
        }

        public async Task<FileResult> ExportCoursePerformanceCsv()
        {
            var vm = await BuildReportsViewModelAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Course,ObjectiveActivities,SpeakingAttempts,TotalActivities,ObjectiveAverage,SpeakingAverage,OverallPerformance,EnrolledStudents,CompletedStudents");

            foreach (var item in vm.CoursePerformance
                .OrderByDescending(x => x.OverallPerformanceScore)
                .ThenByDescending(x => x.TotalLearningActivities))
            {
                sb.AppendLine(
                    Csv(item.CourseTitle) + "," +
                    item.QuizSubmissionCount + "," +
                    item.SpeakingAttemptCount + "," +
                    item.TotalLearningActivities + "," +
                    item.AverageQuizPercentage.ToString("0.0", CultureInfo.InvariantCulture) + "," +
                    item.AverageSpeakingOverallScore.ToString("0.0", CultureInfo.InvariantCulture) + "," +
                    item.OverallPerformanceScore.ToString("0.0", CultureInfo.InvariantCulture) + "," +
                    item.EnrolledStudents + "," +
                    item.CompletedStudents);
            }

            return File(
                new UTF8Encoding(true).GetBytes(sb.ToString()),
                "text/csv",
                "course-performance.csv");
        }

        public async Task<FileResult> ExportTopStudentsCsv()
        {
            var vm = await BuildReportsViewModelAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Student,ObjectiveActivities,SpeakingAttempts,TotalActivities,ObjectiveAverage,SpeakingAverage,OverallPerformance,CompletedCourses");

            foreach (var item in vm.TopStudents
                .OrderByDescending(x => x.OverallPerformanceScore)
                .ThenByDescending(x => x.TotalLearningActivities))
            {
                sb.AppendLine(
                    Csv(item.StudentName) + "," +
                    item.QuizSubmissionCount + "," +
                    item.SpeakingAttemptCount + "," +
                    item.TotalLearningActivities + "," +
                    item.AverageQuizPercentage.ToString("0.0", CultureInfo.InvariantCulture) + "," +
                    item.AverageSpeakingOverallScore.ToString("0.0", CultureInfo.InvariantCulture) + "," +
                    item.OverallPerformanceScore.ToString("0.0", CultureInfo.InvariantCulture) + "," +
                    item.CompletedCourses);
            }

            return File(
                new UTF8Encoding(true).GetBytes(sb.ToString()),
                "text/csv",
                "top-students.csv");
        }

        public async Task<FileResult> ExportRecentSubmissionsCsv()
        {
            var vm = await BuildReportsViewModelAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Type,Student,Course,Lesson,Result,Reviewed,AttemptedAt");

            foreach (var item in vm.RecentActivities.OrderByDescending(x => x.AttemptedAt))
            {
                var resultText = item.ActivityType == "Speaking"
                    ? "Overall " + ((item.SpeakingOverallScore ?? 0).ToString())
                    : ((item.Percentage ?? 0).ToString() + "%");

                var reviewedText = item.ActivityType == "Quiz"
                    ? (item.IsReviewed == true ? "Yes" : "No")
                    : "N/A";

                sb.AppendLine(
                    Csv(item.ActivityType) + "," +
                    Csv(item.StudentName) + "," +
                    Csv(item.CourseTitle) + "," +
                    Csv(item.LessonTitle) + "," +
                    Csv(resultText) + "," +
                    Csv(reviewedText) + "," +
                    item.AttemptedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            }

            return File(
                new UTF8Encoding(true).GetBytes(sb.ToString()),
                "text/csv",
                "recent-learning-activities.csv");
        }

        private static string Csv(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "\"\"";

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}