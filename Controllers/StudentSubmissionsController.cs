using FluentMY.Models;
using FluentMY.ViewModels;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace FluentMY.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentSubmissionsController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();
        private readonly JavaScriptSerializer serializer = new JavaScriptSerializer();

        [HttpGet]
        public async Task<ActionResult> Start(int lessonId)
        {
            var userId = User.Identity.GetUserId();

            var lesson = await db.Lessons
                .Include(l => l.Course)
                .Include(l => l.Exercises)
                .FirstOrDefaultAsync(l => l.LessonId == lessonId);

            if (lesson == null)
                return HttpNotFound();

            bool isEnrolled = await IsStudentEnrolledInCourseAsync(userId, lesson.CourseId);
            if (!isEnrolled)
            {
                TempData["CourseMessage"] = "Please enroll in the course before taking the quiz.";
                return RedirectToAction("Details", "Courses", new { id = lesson.CourseId });
            }

            var playableExercises = lesson.Exercises
                .Where(e => NormalizeKey(e.ExerciseType) != "speaking")
                .OrderBy(e => e.DisplayOrder)
                .ThenBy(e => e.ExerciseId)
                .ToList();

            if (!playableExercises.Any())
            {
                TempData["CourseMessage"] = "This lesson does not have any playable quiz questions yet.";
                return RedirectToAction("Details", "Lessons", new { id = lesson.LessonId });
            }

            var vm = new LessonSubmissionViewModel
            {
                LessonId = lesson.LessonId,
                LessonTitle = lesson.Title,
                CourseTitle = lesson.Course != null ? lesson.Course.Title : "",
                Questions = playableExercises
                    .Select(e => new LessonSubmissionQuestionViewModel
                    {
                        ExerciseId = e.ExerciseId,
                        ExerciseType = string.IsNullOrWhiteSpace(e.ExerciseType) ? "MultipleChoice" : e.ExerciseType,
                        PracticeArea = e.PracticeArea,
                        QuestionText = e.QuestionText,
                        PromptText = string.IsNullOrWhiteSpace(e.PromptText) ? e.QuestionText : e.PromptText,
                        OptionA = e.OptionA,
                        OptionB = e.OptionB,
                        OptionC = e.OptionC,
                        OptionD = e.OptionD,
                        MatchingPairsJson = e.MatchingPairsJson,
                        SentenceTokensJson = e.SentenceTokensJson,
                        AudioPromptPath = e.AudioPromptPath,
                        ReferenceText = e.ReferenceText,
                        SyllableGuide = e.SyllableGuide,
                        PronunciationHint = e.PronunciationHint
                    })
                    .ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Submit(LessonSubmissionViewModel model)
        {
            var userId = User.Identity.GetUserId();

            var lesson = await db.Lessons
                .Include(l => l.Course)
                .Include(l => l.Exercises)
                .FirstOrDefaultAsync(l => l.LessonId == model.LessonId);

            if (lesson == null)
                return HttpNotFound();

            bool isEnrolled = await IsStudentEnrolledInCourseAsync(userId, lesson.CourseId);
            if (!isEnrolled)
            {
                TempData["CourseMessage"] = "Please enroll in the course before submitting the quiz.";
                return RedirectToAction("Details", "Courses", new { id = lesson.CourseId });
            }

            var playableExercises = lesson.Exercises
                .Where(e => NormalizeKey(e.ExerciseType) != "speaking")
                .ToList();

            if (!playableExercises.Any())
            {
                TempData["CourseMessage"] = "This lesson does not have any playable quiz questions yet.";
                return RedirectToAction("Details", "Lessons", new { id = lesson.LessonId });
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    int attemptNumber = await db.Submissions
                        .CountAsync(s => s.StudentId == userId && s.LessonId == lesson.LessonId) + 1;

                    var submission = new Submission
                    {
                        StudentId = userId,
                        LessonId = lesson.LessonId,
                        AttemptNumber = attemptNumber,
                        SubmittedAt = DateTime.UtcNow
                    };

                    db.Submissions.Add(submission);
                    await db.SaveChangesAsync();

                    int totalQuestions = playableExercises.Count;
                    int correctAnswers = 0;
                    int totalScore = 0;

                    foreach (var item in model.Questions ?? Enumerable.Empty<LessonSubmissionQuestionViewModel>())
                    {
                        var exercise = playableExercises.FirstOrDefault(e => e.ExerciseId == item.ExerciseId);
                        if (exercise == null)
                            continue;

                        var evaluation = EvaluateExercise(exercise, item);

                        if (evaluation.IsCorrect)
                            correctAnswers++;

                        totalScore += evaluation.Score;

                        db.SubmissionAnswers.Add(new SubmissionAnswer
                        {
                            SubmissionId = submission.SubmissionId,
                            ExerciseId = exercise.ExerciseId,
                            QuestionTextSnapshot = !string.IsNullOrWhiteSpace(exercise.PromptText) ? exercise.PromptText : exercise.QuestionText,
                            SelectedAnswer = evaluation.SelectedAnswerSnapshot,
                            CorrectAnswerSnapshot = evaluation.CorrectAnswerSnapshot,
                            IsCorrect = evaluation.IsCorrect,
                            Score = evaluation.Score,
                            SubmittedAt = DateTime.UtcNow
                        });
                    }

                    submission.TotalQuestions = totalQuestions;
                    submission.CorrectAnswers = correctAnswers;
                    submission.TotalScore = totalScore;
                    submission.Percentage = totalQuestions == 0
                        ? 0
                        : (int)Math.Floor(correctAnswers * 100.0 / totalQuestions);

                    db.Feedbacks.Add(new Feedback
                    {
                        SubmissionId = submission.SubmissionId,
                        FeedbackType = "AI",
                        CommentText = BuildAutoFeedback(submission.Percentage),
                        CreatedAt = DateTime.UtcNow
                    });

                    await SaveLessonCompletionAndProgress(userId, lesson);
                    await db.SaveChangesAsync();

                    transaction.Commit();

                    return RedirectToAction("Result", new { id = submission.SubmissionId });
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        [HttpGet]
        public async Task<ActionResult> Result(int id)
        {
            var userId = User.Identity.GetUserId();

            var submission = await db.Submissions
                .Include(s => s.Lesson)
                .Include(s => s.Lesson.Course)
                .Include(s => s.SubmissionAnswers)
                .Include(s => s.Feedbacks.Select(f => f.Educator))
                .FirstOrDefaultAsync(s => s.SubmissionId == id && s.StudentId == userId);

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
                                ? (!string.IsNullOrWhiteSpace(f.Educator.FullName) ? f.Educator.FullName : f.Educator.Email)
                                : "Educator")
                            : "AI Coach",
                        CommentText = f.CommentText,
                        CreatedAt = f.CreatedAt
                    }).ToList()
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<ActionResult> History(int page = 1, string type = "All")
        {
            var userId = User.Identity.GetUserId();

            var quizRawItems = await db.Submissions
                .AsNoTracking()
                .Include(s => s.Lesson)
                .Include(s => s.Lesson.Course)
                .Where(s => s.StudentId == userId)
                .Select(s => new
                {
                    s.SubmissionId,
                    s.LessonId,
                    CourseTitle = s.Lesson.Course.Title,
                    LessonTitle = s.Lesson.Title,
                    s.AttemptNumber,
                    s.SubmittedAt,
                    s.TotalQuestions,
                    s.CorrectAnswers,
                    s.TotalScore,
                    s.Percentage
                })
                .ToListAsync();

            var quizItems = quizRawItems
                .Select(s => new StudentLearningHistoryItemViewModel
                {
                    RecordId = s.SubmissionId,
                    LessonId = s.LessonId,
                    ExerciseId = null,
                    CourseTitle = s.CourseTitle,
                    LessonTitle = s.LessonTitle,
                    PromptText = null,
                    ExerciseType = "Quiz",
                    AttemptNumber = s.AttemptNumber,
                    AttemptedAt = s.SubmittedAt,
                    TotalQuestions = s.TotalQuestions,
                    CorrectAnswers = s.CorrectAnswers,
                    TotalScore = s.TotalScore,
                    Percentage = s.Percentage,
                    TotalXP = null,
                    RecognizedText = null,
                    AccuracyScore = null,
                    FluencyScore = null,
                    CompletenessScore = null,
                    PronunciationScore = null,
                    SummaryText = "Correct: " + s.CorrectAnswers + " / " + s.TotalQuestions +
                                  " | Score: " + s.TotalScore +
                                  " | " + s.Percentage + "%",
                    ResultUrl = Url.Action("Result", "StudentSubmissions", new { id = s.SubmissionId })
                })
                .ToList();

            var speakingRawItems = await db.SpeakingAttempts
                .AsNoTracking()
                .Include(x => x.Lesson)
                .Include(x => x.Lesson.Course)
                .Include(x => x.Exercise)
                .Where(x => x.StudentId == userId)
                .Select(x => new
                {
                    x.SpeakingAttemptId,
                    x.ExerciseId,
                    x.LessonId,
                    LessonTitle = x.Lesson.Title,
                    CourseTitle = x.Lesson.Course.Title,
                    PromptText = x.Exercise.PromptText,
                    QuestionText = x.Exercise.QuestionText,
                    x.AttemptNumber,
                    x.RecognizedText,
                    x.AccuracyScore,
                    x.FluencyScore,
                    x.CompletenessScore,
                    x.PronunciationScore,
                    x.CreatedAtUtc
                })
                .ToListAsync();

            var speakingItems = speakingRawItems
                .Select(x => new StudentLearningHistoryItemViewModel
                {
                    RecordId = x.SpeakingAttemptId,
                    LessonId = x.LessonId,
                    ExerciseId = x.ExerciseId,
                    CourseTitle = x.CourseTitle,
                    LessonTitle = x.LessonTitle,
                    PromptText = !string.IsNullOrWhiteSpace(x.PromptText) ? x.PromptText : x.QuestionText,
                    ExerciseType = "Speaking",
                    AttemptNumber = x.AttemptNumber,
                    AttemptedAt = x.CreatedAtUtc,
                    TotalQuestions = null,
                    CorrectAnswers = null,
                    TotalScore = null,
                    Percentage = null,
                    TotalXP = null,
                    RecognizedText = x.RecognizedText,
                    AccuracyScore = x.AccuracyScore,
                    FluencyScore = x.FluencyScore,
                    CompletenessScore = x.CompletenessScore,
                    PronunciationScore = x.PronunciationScore,
                    SummaryText = "Accuracy: " + (x.AccuracyScore ?? 0).ToString("0.#") +
                                  " | Fluency: " + (x.FluencyScore ?? 0).ToString("0.#") +
                                  " | Completeness: " + (x.CompletenessScore ?? 0).ToString("0.#") +
                                  " | Pronunciation: " + (x.PronunciationScore ?? 0).ToString("0.#"),
                    ResultUrl = Url.Action("Result", "Speaking", new { speakingAttemptId = x.SpeakingAttemptId })
                })
                .ToList();

            var gamePlayRawItems = await db.LessonSessions
                .AsNoTracking()
                .Include(x => x.Lesson)
                .Include(x => x.Lesson.Course)
                .Where(x => x.StudentId == userId && x.IsCompleted)
                .Select(x => new
                {
                    x.LessonSessionId,
                    x.LessonId,
                    LessonTitle = x.Lesson.Title,
                    CourseTitle = x.Lesson.Course.Title,
                    x.CorrectCount,
                    x.WrongCount,
                    x.TotalXP,
                    x.StartedAtUtc,
                    x.CompletedAtUtc
                })
                .ToListAsync();

            var gamePlayItems = gamePlayRawItems
                .GroupBy(x => x.LessonId)
                .SelectMany(g => g
                    .OrderBy(x => x.StartedAtUtc)
                    .ThenBy(x => x.LessonSessionId)
                    .Select((x, index) =>
                    {
                        var totalQuestions = x.CorrectCount + x.WrongCount;
                        var percentage = totalQuestions == 0
                            ? 0
                            : (int)Math.Round((x.CorrectCount * 100.0) / totalQuestions);

                        return new StudentLearningHistoryItemViewModel
                        {
                            RecordId = x.LessonSessionId,
                            LessonId = x.LessonId,
                            ExerciseId = null,
                            CourseTitle = x.CourseTitle,
                            LessonTitle = x.LessonTitle,
                            PromptText = null,
                            ExerciseType = "Game Play",
                            AttemptNumber = index + 1,
                            AttemptedAt = x.CompletedAtUtc ?? x.StartedAtUtc,
                            TotalQuestions = totalQuestions,
                            CorrectAnswers = x.CorrectCount,
                            TotalScore = x.TotalXP,
                            Percentage = percentage,
                            TotalXP = x.TotalXP,
                            RecognizedText = null,
                            AccuracyScore = null,
                            FluencyScore = null,
                            CompletenessScore = null,
                            PronunciationScore = null,
                            SummaryText = "Correct: " + x.CorrectCount + " / " + totalQuestions +
                                          " | XP: " + x.TotalXP +
                                          " | " + percentage + "%",
                            ResultUrl = Url.Action("Finish", "LessonPlay", new { sessionId = x.LessonSessionId })
                        };
                    }))
                .ToList();

            var allItems = quizItems
                .Concat(speakingItems)
                .Concat(gamePlayItems)
                .OrderByDescending(x => x.AttemptedAt)
                .ToList();

            if (!string.IsNullOrWhiteSpace(type) && !string.Equals(type, "All", System.StringComparison.OrdinalIgnoreCase))
            {
                allItems = allItems
                    .Where(x => string.Equals(x.ExerciseType, type, System.StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            const int pageSize = 10;
            var totalItems = allItems.Count;
            var totalPages = totalItems == 0
                ? 1
                : (int)Math.Ceiling(totalItems / (double)pageSize);

            if (page < 1)
                page = 1;

            if (page > totalPages)
                page = totalPages;

            var pagedItems = allItems
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var vm = new StudentLearningHistoryPageViewModel
            {
                Items = pagedItems,
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                SelectedType = string.IsNullOrWhiteSpace(type) ? "All" : type
            };

            return View(vm);
        }

        private ExerciseEvaluationResult EvaluateExercise(Exercise exercise, LessonSubmissionQuestionViewModel item)
        {
            var type = NormalizeKey(exercise.ExerciseType);
            if (string.IsNullOrWhiteSpace(type))
                type = "multiplechoice";

            switch (type)
            {
                case "multiplechoice":
                case "listeningchoice":
                    return EvaluateMultipleChoice(exercise, item);

                case "multipleselect":
                    return EvaluateMultipleSelect(exercise, item);

                case "fillblank":
                    return EvaluateFillBlank(exercise, item);

                case "matching":
                    return EvaluateMatching(exercise, item);

                case "reorder":
                    return EvaluateReorder(exercise, item);

                default:
                    return EvaluateMultipleChoice(exercise, item);
            }
        }

        private ExerciseEvaluationResult EvaluateMultipleChoice(Exercise exercise, LessonSubmissionQuestionViewModel item)
        {
            var selectedAnswer = (item.SelectedAnswer ?? "").Trim().ToUpperInvariant();
            var correctAnswer = (exercise.CorrectAnswer ?? "").Trim().ToUpperInvariant();

            bool isCorrect = selectedAnswer == correctAnswer;
            int score = isCorrect ? exercise.Marks : 0;

            return new ExerciseEvaluationResult
            {
                SelectedAnswerSnapshot = selectedAnswer,
                CorrectAnswerSnapshot = correctAnswer,
                IsCorrect = isCorrect,
                Score = score
            };
        }

        private ExerciseEvaluationResult EvaluateMultipleSelect(Exercise exercise, LessonSubmissionQuestionViewModel item)
        {
            var selectedAnswer = NormalizeMultiAnswer(item.SelectedAnswers);
            var correctAnswer = NormalizeMultiAnswer((exercise.CorrectAnswer ?? "").Split(','));

            bool isCorrect = selectedAnswer == correctAnswer;
            int score = isCorrect ? exercise.Marks : 0;

            return new ExerciseEvaluationResult
            {
                SelectedAnswerSnapshot = selectedAnswer,
                CorrectAnswerSnapshot = correctAnswer,
                IsCorrect = isCorrect,
                Score = score
            };
        }

        private ExerciseEvaluationResult EvaluateFillBlank(Exercise exercise, LessonSubmissionQuestionViewModel item)
        {
            var selectedAnswer = (item.SelectedAnswer ?? "").Trim();
            var correctAnswer = (exercise.CorrectAnswer ?? "").Trim();

            bool isCorrect = NormalizeAnswer(selectedAnswer) == NormalizeAnswer(correctAnswer);
            int score = isCorrect ? exercise.Marks : 0;

            return new ExerciseEvaluationResult
            {
                SelectedAnswerSnapshot = selectedAnswer,
                CorrectAnswerSnapshot = correctAnswer,
                IsCorrect = isCorrect,
                Score = score
            };
        }

        private ExerciseEvaluationResult EvaluateMatching(Exercise exercise, LessonSubmissionQuestionViewModel item)
        {
            bool isCorrect = false;

            try
            {
                var submitted = serializer.Deserialize<Dictionary<string, string>>(item.SelectedAnswerJson ?? "{}");
                var expected = serializer.Deserialize<Dictionary<string, string>>(exercise.MatchingPairsJson ?? "{}");

                isCorrect = submitted.Count == expected.Count &&
                            submitted.All(x => expected.ContainsKey(x.Key) &&
                                               NormalizeAnswer(expected[x.Key]) == NormalizeAnswer(x.Value));
            }
            catch
            {
                isCorrect = false;
            }

            return new ExerciseEvaluationResult
            {
                SelectedAnswerSnapshot = item.SelectedAnswerJson,
                CorrectAnswerSnapshot = exercise.MatchingPairsJson,
                IsCorrect = isCorrect,
                Score = isCorrect ? exercise.Marks : 0
            };
        }

        private ExerciseEvaluationResult EvaluateReorder(Exercise exercise, LessonSubmissionQuestionViewModel item)
        {
            bool isCorrect = false;
            string correctAnswerSnapshot = "";

            try
            {
                var submitted = serializer.Deserialize<List<string>>(item.SelectedAnswerJson ?? "[]");
                var expected = serializer.Deserialize<List<string>>(exercise.SentenceTokensJson ?? "[]");

                correctAnswerSnapshot = string.Join(" ", expected);

                isCorrect = submitted.Count == expected.Count &&
                            !submitted.Where((token, index) => NormalizeAnswer(token) != NormalizeAnswer(expected[index])).Any();
            }
            catch
            {
                isCorrect = false;
            }

            return new ExerciseEvaluationResult
            {
                SelectedAnswerSnapshot = item.SelectedAnswerJson,
                CorrectAnswerSnapshot = correctAnswerSnapshot,
                IsCorrect = isCorrect,
                Score = isCorrect ? exercise.Marks : 0
            };
        }

        private string NormalizeAnswer(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return string.Join(" ",
                value.Trim()
                     .ToLowerInvariant()
                     .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }

        private string NormalizeMultiAnswer(IEnumerable<string> values)
        {
            if (values == null)
                return string.Empty;

            return string.Join(",",
                values
                    .Select(x => (x ?? "").Trim().ToUpperInvariant())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .OrderBy(x => x));
        }

        private string NormalizeKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value.Trim().ToLowerInvariant();
        }

        private async Task<bool> IsStudentEnrolledInCourseAsync(string studentId, int courseId)
        {
            return await db.StudentCourses
                .AnyAsync(sc => sc.StudentId == studentId && sc.CourseId == courseId);
        }

        private async Task SaveLessonCompletionAndProgress(string studentId, Lesson lesson)
        {
            var lessonProgress = await db.StudentLessonProgresses
                .FirstOrDefaultAsync(p => p.StudentId == studentId && p.LessonId == lesson.LessonId);

            if (lessonProgress == null)
            {
                db.StudentLessonProgresses.Add(new StudentLessonProgress
                {
                    StudentId = studentId,
                    LessonId = lesson.LessonId,
                    IsCompleted = true,
                    CompletedAt = DateTime.UtcNow
                });
            }
            else
            {
                lessonProgress.IsCompleted = true;
                lessonProgress.CompletedAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync();

            var lessonIdsInCourse = await db.Lessons
                .Where(l => l.CourseId == lesson.CourseId)
                .Select(l => l.LessonId)
                .ToListAsync();

            int totalLessons = lessonIdsInCourse.Count;

            int completedLessons = await db.StudentLessonProgresses
                .CountAsync(p => p.StudentId == studentId && p.IsCompleted && lessonIdsInCourse.Contains(p.LessonId));

            int completionRate = totalLessons == 0
                ? 0
                : (int)Math.Floor(completedLessons * 100.0 / totalLessons);

            int? nextLessonId = await db.Lessons
                .Where(l => l.CourseId == lesson.CourseId)
                .OrderBy(l => l.LessonId)
                .Where(l => !db.StudentLessonProgresses.Any(p =>
                    p.StudentId == studentId &&
                    p.LessonId == l.LessonId &&
                    p.IsCompleted))
                .Select(l => (int?)l.LessonId)
                .FirstOrDefaultAsync();

            var studentCourse = await db.StudentCourses
                .FirstOrDefaultAsync(sc => sc.StudentId == studentId && sc.CourseId == lesson.CourseId);

            if (studentCourse != null)
            {
                studentCourse.CurrentLessonId = nextLessonId;
                studentCourse.IsCompleted = completionRate == 100;

                if (completionRate == 100)
                {
                    if (studentCourse.CompletedAt == null)
                        studentCourse.CompletedAt = DateTime.UtcNow;
                }
                else
                {
                    studentCourse.CompletedAt = null;
                }
            }

            var progress = await db.Progresses
                .FirstOrDefaultAsync(p => p.StudentId == studentId && p.CourseId == lesson.CourseId);

            if (progress == null)
            {
                db.Progresses.Add(new Progress
                {
                    StudentId = studentId,
                    CourseId = lesson.CourseId,
                    TotalLessons = totalLessons,
                    CompletedLessons = completedLessons,
                    CompletionRate = completionRate,
                    LastLessonId = lesson.LessonId,
                    IsCourseCompleted = completionRate == 100,
                    LastUpdated = DateTime.UtcNow
                });
            }
            else
            {
                progress.TotalLessons = totalLessons;
                progress.CompletedLessons = completedLessons;
                progress.CompletionRate = completionRate;
                progress.LastLessonId = lesson.LessonId;
                progress.IsCourseCompleted = completionRate == 100;
                progress.LastUpdated = DateTime.UtcNow;
            }
        }

        private string BuildAutoFeedback(int percentage)
        {
            if (percentage >= 90)
                return "Excellent work. You demonstrated strong understanding of this lesson. Continue to the next topic and maintain this level of accuracy.";

            if (percentage >= 70)
                return "Good job. You understood most of the lesson, but there are still a few areas to improve. Review the incorrect answers before moving on.";

            if (percentage >= 50)
                return "Fair attempt. You have some understanding, but you should revisit the lesson content and retry the quiz to strengthen your knowledge.";

            return "You need more practice on this lesson. Review the lesson carefully, focus on vocabulary and grammar points, and try again.";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }

        private class ExerciseEvaluationResult
        {
            public string SelectedAnswerSnapshot { get; set; }
            public string CorrectAnswerSnapshot { get; set; }
            public bool IsCorrect { get; set; }
            public int Score { get; set; }
        }
    }
}