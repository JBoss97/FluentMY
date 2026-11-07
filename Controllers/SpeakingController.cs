using FluentMY.Models;
using FluentMY.Services;
using FluentMY.ViewModels;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace FluentMY.Controllers
{
    [Authorize(Roles = "Student")]
    public class SpeakingController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();
        private readonly SpeechAssessmentService speechAssessmentService = new SpeechAssessmentService();

        [HttpGet]
        public async Task<ActionResult> Start(int exerciseId, int lessonId)
        {
            var userId = User.Identity.GetUserId();

            var exercise = await db.Exercises
                .Include(e => e.Lesson)
                .Include(e => e.Lesson.Course)
                .FirstOrDefaultAsync(e => e.ExerciseId == exerciseId && e.LessonId == lessonId);

            if (exercise == null)
                return HttpNotFound();

            bool isEnrolled = await db.StudentCourses
                .AnyAsync(sc => sc.StudentId == userId && sc.CourseId == exercise.Lesson.CourseId);

            if (!isEnrolled)
            {
                TempData["CourseMessage"] = "Please enroll in the course before starting speaking practice.";
                return RedirectToAction("Details", "Courses", new { id = exercise.Lesson.CourseId });
            }

            if (NormalizeKey(exercise.ExerciseType) != "speaking")
                return HttpNotFound();

            var vm = new SpeakingStartViewModel
            {
                ExerciseId = exercise.ExerciseId,
                LessonId = exercise.LessonId,
                LessonTitle = exercise.Lesson != null ? exercise.Lesson.Title : "",
                CourseTitle = exercise.Lesson != null && exercise.Lesson.Course != null ? exercise.Lesson.Course.Title : "",
                PromptText = !string.IsNullOrWhiteSpace(exercise.PromptText) ? exercise.PromptText : exercise.QuestionText,
                ReferenceText = exercise.ReferenceText,
                SyllableGuide = exercise.SyllableGuide,
                PronunciationHint = exercise.PronunciationHint,
                AudioPromptPath = exercise.AudioPromptPath,
                SpeechLocale = ResolveSpeechLocale(exercise)
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SaveAttempt(SpeakingAttemptSaveViewModel model)
        {
            var userId = User.Identity.GetUserId();

            if (model == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Invalid request."
                });
            }

            var exercise = await db.Exercises
                .Include(e => e.Lesson)
                .Include(e => e.Lesson.Course)
                .FirstOrDefaultAsync(e => e.ExerciseId == model.ExerciseId && e.LessonId == model.LessonId);

            if (exercise == null)
                return HttpNotFound();

            bool isEnrolled = await db.StudentCourses
                .AnyAsync(sc => sc.StudentId == userId && sc.CourseId == exercise.Lesson.CourseId);

            if (!isEnrolled)
            {
                return Json(new
                {
                    success = false,
                    message = "You are not enrolled in this course."
                });
            }

            var referenceText = !string.IsNullOrWhiteSpace(exercise.ReferenceText)
                ? exercise.ReferenceText
                : model.ReferenceText;

            if (string.IsNullOrWhiteSpace(referenceText))
            {
                return Json(new
                {
                    success = false,
                    message = "Reference text is missing for this speaking exercise."
                });
            }

            if (string.IsNullOrWhiteSpace(model.RecognizedText))
            {
                return Json(new
                {
                    success = false,
                    message = "No speech was recognized. Please try again."
                });
            }

            // Server-side scoring
            var assessment = await speechAssessmentService.AssessAsync(model.RecognizedText, referenceText);

            var attemptNumber = await db.SpeakingAttempts
                .CountAsync(x => x.StudentId == userId && x.ExerciseId == model.ExerciseId) + 1;

            var attempt = new SpeakingAttempt
            {
                ExerciseId = model.ExerciseId,
                LessonId = model.LessonId,
                StudentId = userId,
                AttemptNumber = attemptNumber,
                RecognizedText = assessment.RecognizedText,
                ReferenceText = assessment.ReferenceText,
                AccuracyScore = assessment.AccuracyScore,
                FluencyScore = assessment.FluencyScore,
                CompletenessScore = assessment.CompletenessScore,
                PronunciationScore = assessment.PronunciationScore,
                FeedbackJson = assessment.FeedbackJson,
                CreatedAtUtc = DateTime.UtcNow
            };

            db.SpeakingAttempts.Add(attempt);
            await db.SaveChangesAsync();

            return Json(new
            {
                success = true,
                redirectUrl = Url.Action("Result", new { speakingAttemptId = attempt.SpeakingAttemptId })
            });
        }

        [HttpGet]
        public async Task<ActionResult> Result(int speakingAttemptId)
        {
            var userId = User.Identity.GetUserId();

            var attempt = await db.SpeakingAttempts
                .Include(x => x.Lesson)
                .Include(x => x.Lesson.Course)
                .FirstOrDefaultAsync(x => x.SpeakingAttemptId == speakingAttemptId && x.StudentId == userId);

            if (attempt == null)
                return HttpNotFound();

            var vm = new SpeakingResultViewModel
            {
                SpeakingAttemptId = attempt.SpeakingAttemptId,
                ExerciseId = attempt.ExerciseId,
                LessonId = attempt.LessonId,
                AttemptNumber = attempt.AttemptNumber,
                LessonTitle = attempt.Lesson != null ? attempt.Lesson.Title : "",
                CourseTitle = attempt.Lesson != null && attempt.Lesson.Course != null ? attempt.Lesson.Course.Title : "",
                RecognizedText = attempt.RecognizedText,
                ReferenceText = attempt.ReferenceText,
                AccuracyScore = attempt.AccuracyScore,
                FluencyScore = attempt.FluencyScore,
                CompletenessScore = attempt.CompletenessScore,
                PronunciationScore = attempt.PronunciationScore,
                FeedbackJson = attempt.FeedbackJson,
                CreatedAtUtc = attempt.CreatedAtUtc
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<ActionResult> History(int? lessonId, int page = 1)
        {
            var userId = User.Identity.GetUserId();

            var lessonOptionRows = await db.SpeakingAttempts
                .AsNoTracking()
                .Where(x => x.StudentId == userId)
                .GroupBy(x => new
                {
                    x.LessonId,
                    LessonTitle = x.Lesson.Title,
                    CourseTitle = x.Lesson.Course.Title
                })
                .Select(g => new
                {
                    g.Key.LessonId,
                    g.Key.LessonTitle,
                    g.Key.CourseTitle
                })
                .OrderBy(x => x.CourseTitle)
                .ThenBy(x => x.LessonTitle)
                .ToListAsync();

            var lessonOptions = new List<SelectListItem>
    {
        new SelectListItem
        {
            Text = "All Lessons",
            Value = "",
            Selected = !lessonId.HasValue
        }
    };

            lessonOptions.AddRange(
                lessonOptionRows.Select(x => new SelectListItem
                {
                    Value = x.LessonId.ToString(),
                    Text = x.CourseTitle + " - " + x.LessonTitle,
                    Selected = lessonId.HasValue && lessonId.Value == x.LessonId
                })
            );

            var query = db.SpeakingAttempts
                .AsNoTracking()
                .Include(x => x.Lesson)
                .Include(x => x.Lesson.Course)
                .Include(x => x.Exercise)
                .Where(x => x.StudentId == userId);

            if (lessonId.HasValue)
            {
                query = query.Where(x => x.LessonId == lessonId.Value);
            }

            var rawItems = await query
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(x => new
                {
                    x.SpeakingAttemptId,
                    x.ExerciseId,
                    x.LessonId,
                    LessonTitle = x.Lesson.Title,
                    CourseTitle = x.Lesson.Course.Title,
                    PromptText = x.Exercise.PromptText,
                    QuestionText = x.Exercise.QuestionText,
                    x.RecognizedText,
                    x.AccuracyScore,
                    x.FluencyScore,
                    x.CompletenessScore,
                    x.PronunciationScore,
                    x.AttemptNumber,
                    x.CreatedAtUtc
                })
                .ToListAsync();

            var allItems = rawItems
                .Select(x => new SpeakingHistoryItemViewModel
                {
                    SpeakingAttemptId = x.SpeakingAttemptId,
                    ExerciseId = x.ExerciseId,
                    LessonId = x.LessonId,
                    LessonTitle = x.LessonTitle,
                    CourseTitle = x.CourseTitle,
                    PromptText = !string.IsNullOrWhiteSpace(x.PromptText)
                        ? x.PromptText
                        : x.QuestionText,
                    RecognizedText = x.RecognizedText,
                    AccuracyScore = x.AccuracyScore,
                    FluencyScore = x.FluencyScore,
                    CompletenessScore = x.CompletenessScore,
                    PronunciationScore = x.PronunciationScore,
                    AttemptNumber = x.AttemptNumber,
                    CreatedAtUtc = x.CreatedAtUtc
                })
                .ToList();

            var totalAttempts = allItems.Count;

            var avgAccuracy = totalAttempts > 0
                ? (int)System.Math.Round(allItems.Average(x => x.AccuracyScore ?? 0))
                : 0;

            var avgFluency = totalAttempts > 0
                ? (int)System.Math.Round(allItems.Average(x => x.FluencyScore ?? 0))
                : 0;

            var avgCompleteness = totalAttempts > 0
                ? (int)System.Math.Round(allItems.Average(x => x.CompletenessScore ?? 0))
                : 0;

            var avgPronunciation = totalAttempts > 0
                ? (int)System.Math.Round(allItems.Average(x => x.PronunciationScore ?? 0))
                : 0;

            var avgOverall = totalAttempts > 0
                ? (int)System.Math.Round(allItems.Average(x =>
                    ((x.AccuracyScore ?? 0) +
                     (x.FluencyScore ?? 0) +
                     (x.CompletenessScore ?? 0) +
                     (x.PronunciationScore ?? 0)) / 4m))
                : 0;

            const int pageSize = 10;
            var totalItems = allItems.Count;
            var totalPages = totalItems == 0
                ? 1
                : (int)System.Math.Ceiling(totalItems / (double)pageSize);

            if (page < 1)
                page = 1;

            if (page > totalPages)
                page = totalPages;

            var pagedItems = allItems
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var vm = new SpeakingHistoryPageViewModel
            {
                Items = pagedItems,
                LessonOptions = lessonOptions,
                SelectedLessonId = lessonId,
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                TotalAttempts = totalAttempts,
                AvgAccuracy = avgAccuracy,
                AvgFluency = avgFluency,
                AvgCompleteness = avgCompleteness,
                AvgPronunciation = avgPronunciation,
                AvgOverall = avgOverall
            };

            return View(vm);
        }

        private string ResolveSpeechLocale(Exercise exercise)
        {
            // Change this if your exercise language is English instead
            return "ms-MY";
        }

        private string NormalizeKey(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? ""
                : value.Trim().ToLowerInvariant();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}