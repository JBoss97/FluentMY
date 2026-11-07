using FluentMY.Models;
using FluentMY.ViewModels;
using Microsoft.AspNet.Identity;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace FluentMY.Controllers
{
    [Authorize(Roles = "Educator")]
    public class EducatorReviewController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        [HttpGet]
        public async Task<ActionResult> Index(int lessonId)
        {
            var educatorId = User.Identity.GetUserId();

            var lesson = await db.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.LessonId == lessonId && l.EducatorId == educatorId);

            if (lesson == null)
                return HttpNotFound();

            var quizItems = await db.Submissions
                .Include(s => s.Student)
                .Where(s => s.LessonId == lessonId)
                .Select(s => new EducatorSubmissionListItemViewModel
                {
                    SubmissionId = s.SubmissionId,
                    LessonSessionId = null,
                    ActivityType = "Quiz",
                    StudentName = s.Student.FullName != null && s.Student.FullName != ""
                        ? s.Student.FullName
                        : s.Student.Email,
                    AttemptNumber = s.AttemptNumber,
                    CorrectAnswers = s.CorrectAnswers,
                    TotalQuestions = s.TotalQuestions,
                    TotalScore = s.TotalScore,
                    Percentage = s.Percentage,
                    IsReviewed = s.IsReviewed,
                    IsReviewable = true,
                    SubmittedAt = s.SubmittedAt
                })
                .ToListAsync();

            var gamePlayRawItems = await db.LessonSessions
                .Include(x => x.Student)
                .Where(x => x.LessonId == lessonId && x.IsCompleted)
                .Select(x => new
                {
                    x.LessonSessionId,
                    StudentName = x.Student.FullName != null && x.Student.FullName != ""
                        ? x.Student.FullName
                        : x.Student.Email,
                    x.CorrectCount,
                    x.WrongCount,
                    x.TotalXP,
                    x.StartedAtUtc,
                    x.CompletedAtUtc
                })
                .ToListAsync();

            var gamePlayItems = gamePlayRawItems
                .GroupBy(x => x.StudentName)
                .SelectMany(g => g
                    .OrderBy(x => x.StartedAtUtc)
                    .ThenBy(x => x.LessonSessionId)
                    .Select((x, index) =>
                    {
                        var totalQuestions = x.CorrectCount + x.WrongCount;
                        var percentage = totalQuestions == 0
                            ? 0
                            : (int)Math.Round((x.CorrectCount * 100.0) / totalQuestions);

                        return new EducatorSubmissionListItemViewModel
                        {
                            SubmissionId = 0,
                            LessonSessionId = x.LessonSessionId,
                            ActivityType = "Game Play",
                            StudentName = x.StudentName,
                            AttemptNumber = index + 1,
                            CorrectAnswers = x.CorrectCount,
                            TotalQuestions = totalQuestions,
                            TotalScore = x.TotalXP,
                            Percentage = percentage,
                            IsReviewed = false,
                            IsReviewable = false,
                            SubmittedAt = x.CompletedAtUtc ?? x.StartedAtUtc
                        };
                    }))
                .ToList();

            var items = quizItems
                .Concat(gamePlayItems)
                .OrderByDescending(x => x.SubmittedAt)
                .ToList();

            ViewBag.LessonId = lesson.LessonId;
            ViewBag.LessonTitle = lesson.Title;
            ViewBag.CourseTitle = lesson.Course != null ? lesson.Course.Title : "";
            ViewBag.CourseId = lesson.CourseId;

            return View(items);
        }

        [HttpGet]
        public async Task<ActionResult> Details(int submissionId)
        {
            var submission = await GetOwnedSubmissionAsync(submissionId);
            if (submission == null)
                return HttpNotFound();

            var vm = new SubmissionResultViewModel
            {
                SubmissionId = submission.SubmissionId,
                LessonId = submission.LessonId,
                LessonTitle = submission.Lesson != null ? submission.Lesson.Title : "",
                CourseTitle = submission.Lesson != null && submission.Lesson.Course != null
                    ? submission.Lesson.Course.Title
                    : "",
                StudentName = submission.Student != null
                    ? (!string.IsNullOrWhiteSpace(submission.Student.FullName)
                        ? submission.Student.FullName
                        : submission.Student.Email)
                    : "Unknown",
                AttemptNumber = submission.AttemptNumber,
                TotalQuestions = submission.TotalQuestions,
                CorrectAnswers = submission.CorrectAnswers,
                TotalScore = submission.TotalScore,
                Percentage = submission.Percentage,
                SubmittedAt = submission.SubmittedAt,
                IsReviewed = submission.IsReviewed,
                ReviewedAt = submission.ReviewedAt,

                Answers = submission.SubmissionAnswers
                    .OrderBy(a => a.SubmissionAnswerId)
                    .Select(a => new SubmissionAnswerResultViewModel
                    {
                        QuestionText = a.QuestionTextSnapshot,
                        SelectedAnswer = a.SelectedAnswer,
                        CorrectAnswer = a.CorrectAnswerSnapshot,
                        IsCorrect = a.IsCorrect,
                        Score = a.Score
                    }).ToList(),

                Feedbacks = submission.Feedbacks
                    .OrderBy(f => f.CreatedAt)
                    .Select(f => new FeedbackItemViewModel
                    {
                        FeedbackType = f.FeedbackType,
                        AuthorName = f.FeedbackType == "Educator"
                            ? (f.Educator != null
                                ? (!string.IsNullOrWhiteSpace(f.Educator.FullName)
                                    ? f.Educator.FullName
                                    : f.Educator.Email)
                                : "Educator")
                            : "AI Coach",
                        CommentText = f.CommentText,
                        CreatedAt = f.CreatedAt
                    }).ToList()
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<ActionResult> SessionDetails(int sessionId)
        {
            var educatorId = User.Identity.GetUserId();

            var session = await db.LessonSessions
                .Include(x => x.Student)
                .Include(x => x.Lesson)
                .Include(x => x.Lesson.Course)
                .Include(x => x.Answers.Select(a => a.Exercise))
                .FirstOrDefaultAsync(x =>
                    x.LessonSessionId == sessionId &&
                    x.Lesson.EducatorId == educatorId);

            if (session == null)
                return HttpNotFound();

            var vm = new EducatorGamePlaySessionDetailViewModel
            {
                LessonSessionId = session.LessonSessionId,
                StudentName = session.Student != null
                    ? (!string.IsNullOrWhiteSpace(session.Student.FullName)
                        ? session.Student.FullName
                        : session.Student.Email)
                    : "Unknown",
                CourseTitle = session.Lesson != null && session.Lesson.Course != null
                    ? session.Lesson.Course.Title
                    : "",
                LessonTitle = session.Lesson != null ? session.Lesson.Title : "",
                CorrectAnswers = session.CorrectCount,
                WrongAnswers = session.WrongCount,
                TotalQuestions = session.CorrectCount + session.WrongCount,
                TotalXP = session.TotalXP,
                Percentage = (session.CorrectCount + session.WrongCount) == 0
                    ? 0
                    : (int)Math.Round((session.CorrectCount * 100.0) / (session.CorrectCount + session.WrongCount)),
                SubmittedAt = session.CompletedAtUtc ?? session.StartedAtUtc,
                Answers = session.Answers
                    .OrderBy(a => a.LessonSessionAnswerId)
                    .Select(a => new EducatorGamePlaySessionAnswerItemViewModel
                    {
                        PromptText = a.Exercise != null
                            ? (!string.IsNullOrWhiteSpace(a.Exercise.PromptText)
                                ? a.Exercise.PromptText
                                : a.Exercise.QuestionText)
                            : "",
                        SubmittedAnswer = a.SubmittedAnswer,
                        SubmittedAnswerJson = a.SubmittedAnswerJson,
                        IsCorrect = a.IsCorrect,
                        XPGranted = a.XPGranted
                    })
                    .ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddFeedback(int submissionId, string feedbackText)
        {
            if (string.IsNullOrWhiteSpace(feedbackText))
            {
                TempData["ReviewMessage"] = "Feedback text cannot be empty.";
                return RedirectToAction("Details", new { submissionId });
            }

            var submission = await GetOwnedSubmissionAsync(submissionId);
            if (submission == null)
                return HttpNotFound();

            var educatorId = User.Identity.GetUserId();

            db.Feedbacks.Add(new Feedback
            {
                SubmissionId = submissionId,
                EducatorId = educatorId,
                FeedbackType = "Educator",
                CommentText = feedbackText.Trim(),
                CreatedAt = DateTime.UtcNow
            });

            submission.IsReviewed = true;
            submission.ReviewedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            TempData["ReviewMessage"] = "Feedback added successfully.";
            return RedirectToAction("Details", new { submissionId });
        }

        private async Task<Submission> GetOwnedSubmissionAsync(int submissionId)
        {
            var educatorId = User.Identity.GetUserId();

            return await db.Submissions
                .Include(s => s.Student)
                .Include(s => s.Lesson)
                .Include(s => s.Lesson.Course)
                .Include(s => s.SubmissionAnswers)
                .Include(s => s.Feedbacks.Select(f => f.Educator))
                .FirstOrDefaultAsync(s =>
                    s.SubmissionId == submissionId &&
                    s.Lesson.EducatorId == educatorId);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}