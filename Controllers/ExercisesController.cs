using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentMY.Models;
using FluentMY.ViewModels;
using Microsoft.AspNet.Identity;
using System.Data.Entity;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace FluentMY.Controllers
{
    [Authorize(Roles = "Student")]
    public class ExercisesController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        [HttpGet]
        public async Task<ActionResult> Start(int lessonId)
        {
            var lesson = await db.Lessons
                .Include(l => l.Course)
                .Include(l => l.Exercises)
                .FirstOrDefaultAsync(l => l.LessonId == lessonId);

            if (lesson == null)
                return HttpNotFound();

            var vm = new LessonExerciseViewModel
            {
                LessonId = lesson.LessonId,
                LessonTitle = lesson.Title,
                CourseTitle = lesson.Course != null ? lesson.Course.Title : "",
                Exercises = lesson.Exercises.Select(e => new ExerciseItemViewModel
                {
                    ExerciseId = e.ExerciseId,
                    QuestionText = e.QuestionText,
                    OptionA = e.OptionA,
                    OptionB = e.OptionB,
                    OptionC = e.OptionC,
                    OptionD = e.OptionD
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Submit(LessonExerciseViewModel model)
        {
            var userId = User.Identity.GetUserId();

            var lesson = await db.Lessons
                .Include(l => l.Course)
                .Include(l => l.Exercises)
                .FirstOrDefaultAsync(l => l.LessonId == model.LessonId);

            if (lesson == null)
                return HttpNotFound();

            int nextAttemptNumber = await db.Submissions
                .CountAsync(s => s.StudentId == userId && s.LessonId == lesson.LessonId) + 1;

            var submission = new Submission
            {
                StudentId = userId,
                LessonId = lesson.LessonId,
                AttemptNumber = nextAttemptNumber,
                SubmittedAt = DateTime.UtcNow
            };

            db.Submissions.Add(submission);
            await db.SaveChangesAsync();

            int totalQuestions = lesson.Exercises.Count;
            int correctAnswers = 0;
            int totalScore = 0;

            foreach (var submitted in model.Exercises)
            {
                var exercise = lesson.Exercises.FirstOrDefault(e => e.ExerciseId == submitted.ExerciseId);
                if (exercise == null) continue;

                var selected = (submitted.SelectedAnswer ?? "").ToUpper();
                bool isCorrect = selected == exercise.CorrectAnswer.ToUpper();
                int score = isCorrect ? exercise.Marks : 0;

                if (isCorrect) correctAnswers++;
                totalScore += score;

                db.SubmissionAnswers.Add(new SubmissionAnswer
                {
                    SubmissionId = submission.SubmissionId,
                    ExerciseId = exercise.ExerciseId,
                    QuestionTextSnapshot = exercise.QuestionText,
                    SelectedAnswer = selected,
                    CorrectAnswerSnapshot = exercise.CorrectAnswer,
                    IsCorrect = isCorrect,
                    Score = score,
                    SubmittedAt = DateTime.UtcNow
                });
            }

            submission.TotalQuestions = totalQuestions;
            submission.CorrectAnswers = correctAnswers;
            submission.TotalScore = totalScore;
            submission.Percentage = totalQuestions == 0
                ? 0
                : (int)Math.Floor(correctAnswers * 100.0 / totalQuestions);

            db.Entry(submission).State = EntityState.Modified;

            db.Feedbacks.Add(new Feedback
            {
                SubmissionId = submission.SubmissionId,
                FeedbackType = "AI",
                FeedbackText = BuildAiFeedback(submission.Percentage, submission.CorrectAnswers, submission.TotalQuestions),
                CreatedAt = DateTime.UtcNow
            });

            var progress = await db.StudentLessonProgresses
                .FirstOrDefaultAsync(p => p.StudentId == userId && p.LessonId == lesson.LessonId);

            if (progress == null)
            {
                progress = new StudentLessonProgress
                {
                    StudentId = userId,
                    LessonId = lesson.LessonId,
                    IsCompleted = false,
                    AttemptsCount = 0
                };
                db.StudentLessonProgresses.Add(progress);
            }

            progress.AttemptsCount += 1;
            progress.LastAttemptAt = DateTime.UtcNow;
            progress.LastScorePercent = submission.Percentage;
            progress.BestScorePercent = progress.BestScorePercent.HasValue
                ? Math.Max(progress.BestScorePercent.Value, submission.Percentage)
                : submission.Percentage;

            await db.SaveChangesAsync();

            return RedirectToAction("Result", new { submissionId = submission.SubmissionId });
        }

        [HttpGet]
        public async Task<ActionResult> Result(int submissionId)
        {
            var userId = User.Identity.GetUserId();

            var submission = await db.Submissions
                .Include(s => s.Lesson)
                .Include(s => s.Lesson.Course)
                .Include(s => s.Answers)
                .Include("Feedbacks.GivenByUser")
                .FirstOrDefaultAsync(s => s.SubmissionId == submissionId && s.StudentId == userId);

            if (submission == null)
                return HttpNotFound();

            var vm = new SubmissionResultViewModel
            {
                SubmissionId = submission.SubmissionId,
                LessonId = submission.LessonId,
                LessonTitle = submission.Lesson != null ? submission.Lesson.Title : "",
                CourseTitle = submission.Lesson != null && submission.Lesson.Course != null ? submission.Lesson.Course.Title : "",
                AttemptNumber = submission.AttemptNumber,
                TotalQuestions = submission.TotalQuestions,
                CorrectAnswers = submission.CorrectAnswers,
                TotalScore = submission.TotalScore,
                Percentage = submission.Percentage,
                SubmittedAt = submission.SubmittedAt,
                Answers = submission.Answers
                    .OrderBy(a => a.SubmissionAnswerId)
                    .Select(a => new SubmissionAnswerResultItemViewModel
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
                        FeedbackText = f.FeedbackText,
                        GivenByName = f.FeedbackType == "AI"
                            ? "FluentMY AI"
                            : (f.GivenByUser != null
                                ? (!string.IsNullOrWhiteSpace(f.GivenByUser.FullName) ? f.GivenByUser.FullName : f.GivenByUser.Email)
                                : "Educator"),
                        CreatedAt = f.CreatedAt
                    }).ToList()
            };

            return View(vm);
        }

        private string BuildAiFeedback(int percentage, int correctAnswers, int totalQuestions)
        {
            if (totalQuestions == 0)
                return "No questions were available in this quiz.";

            if (percentage >= 80)
                return $"Great job. You answered {correctAnswers} out of {totalQuestions} correctly. Your understanding is strong. Review the few mistakes and move on to the next lesson.";

            if (percentage >= 50)
                return $"Good effort. You answered {correctAnswers} out of {totalQuestions} correctly. You understand the basics, but you should review the lesson and retry to strengthen your accuracy.";

            return $"You answered {correctAnswers} out of {totalQuestions} correctly. It is recommended that you revisit the lesson content, focus on the incorrect items, and try the quiz again.";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}