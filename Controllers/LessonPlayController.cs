using FluentMY.Models;
using FluentMY.Services;
using FluentMY.ViewModels;
using Microsoft.AspNet.Identity;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace FluentMY.Controllers
{
    [Authorize(Roles = "Student")]
    public class LessonPlayController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();
        private readonly ExerciseEvaluationService evaluationService = new ExerciseEvaluationService();

        [HttpGet]
        public async Task<ActionResult> Start(int lessonId)
        {
            var studentId = User.Identity.GetUserId();

            var lesson = await db.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.LessonId == lessonId);

            if (lesson == null)
                return HttpNotFound();

            bool isEnrolled = await db.StudentCourses
                .AnyAsync(x => x.StudentId == studentId && x.CourseId == lesson.CourseId);

            if (!isEnrolled)
            {
                TempData["CourseMessage"] = "Please enroll in the course first.";
                return RedirectToAction("Details", "Courses", new { id = lesson.CourseId });
            }

            var totalExercises = await GetPlayableExercisesQuery(lessonId).CountAsync();
            if (totalExercises == 0)
            {
                TempData["CourseMessage"] = "This lesson does not have any game-style exercises yet.";
                return RedirectToAction("Details", "Lessons", new { id = lessonId });
            }

            var session = new LessonSession
            {
                LessonId = lessonId,
                StudentId = studentId,
                CurrentIndex = 0,
                HeartsRemaining = 3,
                CurrentStreak = 0,
                TotalXP = 0,
                CorrectCount = 0,
                WrongCount = 0,
                IsCompleted = false,
                RewardsApplied = false,
                StartedAtUtc = DateTime.UtcNow
            };

            db.LessonSessions.Add(session);
            await db.SaveChangesAsync();

            return RedirectToAction("Play", new { sessionId = session.LessonSessionId });
        }

        [HttpGet]
        public async Task<ActionResult> Play(int sessionId)
        {
            var studentId = User.Identity.GetUserId();

            var session = await db.LessonSessions
                .Include(x => x.Lesson)
                .Include(x => x.Lesson.Course)
                .FirstOrDefaultAsync(x => x.LessonSessionId == sessionId && x.StudentId == studentId);

            if (session == null)
                return HttpNotFound();

            var exercises = await GetPlayableExercisesQuery(session.LessonId)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.ExerciseId)
                .ToListAsync();

            if (!exercises.Any())
            {
                TempData["CourseMessage"] = "This lesson does not have any game-style exercises yet.";
                return RedirectToAction("Details", "Lessons", new { id = session.LessonId });
            }

            if (session.IsCompleted || session.CurrentIndex >= exercises.Count)
            {
                if (!session.IsCompleted)
                {
                    session.IsCompleted = true;
                    session.CompletedAtUtc = DateTime.UtcNow;

                    if (!session.RewardsApplied)
                    {
                        await ApplyCompletionRewardsAsync(session);
                    }

                    await db.SaveChangesAsync();
                }

                return RedirectToAction("Finish", new { sessionId = session.LessonSessionId });
            }

            var exercise = exercises[session.CurrentIndex];

            var vm = new LessonPlayViewModel
            {
                LessonId = session.LessonId,
                LessonTitle = session.Lesson.Title,
                CourseTitle = session.Lesson.Course != null ? session.Lesson.Course.Title : "",
                SessionId = session.LessonSessionId,
                CurrentIndex = session.CurrentIndex + 1,
                TotalQuestions = exercises.Count,
                HeartsRemaining = session.HeartsRemaining,
                CurrentStreak = session.CurrentStreak,
                EarnedXP = session.TotalXP,
                CurrentExercise = MapExercise(exercise)
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SubmitAnswer(LessonAnswerPostViewModel model)
        {
            var studentId = User.Identity.GetUserId();

            var session = await db.LessonSessions
                .Include(x => x.Lesson)
                .Include(x => x.Lesson.Course)
                .FirstOrDefaultAsync(x => x.LessonSessionId == model.SessionId && x.StudentId == studentId);

            if (session == null || session.IsCompleted)
                return HttpNotFound();

            var orderedExercises = await GetPlayableExercisesQuery(session.LessonId)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.ExerciseId)
                .ToListAsync();

            if (!orderedExercises.Any())
                return HttpNotFound();

            if (session.CurrentIndex < 0 || session.CurrentIndex >= orderedExercises.Count)
                return HttpNotFound();

            var currentExercise = orderedExercises[session.CurrentIndex];

            if (currentExercise.ExerciseId != model.ExerciseId)
            {
                return Json(new
                {
                    isCorrect = false,
                    feedbackText = "The submitted question does not match the current session question. Please try again.",
                    correctAnswer = "",
                    heartsRemaining = session.HeartsRemaining,
                    currentStreak = session.CurrentStreak,
                    totalXP = session.TotalXP,
                    isFinished = false,
                    redirectUrl = Url.Action("Play", new { sessionId = session.LessonSessionId })
                });
            }

            var result = evaluationService.Evaluate(currentExercise, model.Answer, model.AnswerJson, model.SelectedAnswers);

            db.LessonSessionAnswers.Add(new LessonSessionAnswer
            {
                LessonSessionId = session.LessonSessionId,
                ExerciseId = currentExercise.ExerciseId,
                SubmittedAnswer = model.Answer,
                SubmittedAnswerJson = !string.IsNullOrWhiteSpace(model.AnswerJson)
                    ? model.AnswerJson
                    : (model.SelectedAnswers != null && model.SelectedAnswers.Any()
                        ? string.Join(",", model.SelectedAnswers.OrderBy(x => x))
                        : null),
                IsCorrect = result.IsCorrect,
                XPGranted = result.XPGranted,
                CreatedAtUtc = DateTime.UtcNow
            });

            if (result.IsCorrect)
            {
                session.CorrectCount++;
                session.CurrentStreak++;
                session.TotalXP += result.XPGranted;
            }
            else
            {
                session.WrongCount++;
                session.CurrentStreak = 0;
                session.HeartsRemaining = Math.Max(0, session.HeartsRemaining - 1);
            }

            session.CurrentIndex++;

            if (session.HeartsRemaining <= 0 || session.CurrentIndex >= orderedExercises.Count)
            {
                session.IsCompleted = true;
                session.CompletedAtUtc = DateTime.UtcNow;

                if (!session.RewardsApplied)
                {
                    await ApplyCompletionRewardsAsync(session);
                }
            }

            await db.SaveChangesAsync();

            return Json(new
            {
                isCorrect = result.IsCorrect,
                feedbackText = result.FeedbackText,
                correctAnswer = result.CorrectAnswer,
                heartsRemaining = session.HeartsRemaining,
                currentStreak = session.CurrentStreak,
                totalXP = session.TotalXP,
                isFinished = session.IsCompleted,
                redirectUrl = session.IsCompleted
                    ? Url.Action("Finish", new { sessionId = session.LessonSessionId })
                    : Url.Action("Play", new { sessionId = session.LessonSessionId })
            });
        }

        [HttpGet]
        public async Task<ActionResult> Finish(int sessionId)
        {
            var studentId = User.Identity.GetUserId();

            var session = await db.LessonSessions
                .Include(x => x.Lesson)
                .Include(x => x.Lesson.Course)
                .FirstOrDefaultAsync(x => x.LessonSessionId == sessionId && x.StudentId == studentId);

            if (session == null)
                return HttpNotFound();

            var totalAnswered = session.CorrectCount + session.WrongCount;
            var percentage = totalAnswered == 0
                ? 0
                : (int)Math.Round((session.CorrectCount * 100.0) / totalAnswered);

            var stars = percentage >= 90 ? 3
                : percentage >= 70 ? 2
                : percentage >= 50 ? 1
                : 0;

            var nextLessonId = await db.Lessons
                .Where(l => l.CourseId == session.Lesson.CourseId && l.LessonId > session.LessonId)
                .OrderBy(l => l.LessonId)
                .Select(l => (int?)l.LessonId)
                .FirstOrDefaultAsync();

            var vm = new LessonFinishViewModel
            {
                LessonId = session.LessonId,
                LessonTitle = session.Lesson.Title,
                CourseTitle = session.Lesson.Course != null ? session.Lesson.Course.Title : "",
                CorrectCount = session.CorrectCount,
                WrongCount = session.WrongCount,
                TotalXP = session.TotalXP,
                StarsEarned = stars,
                PerfectRun = session.WrongCount == 0,
                NextLessonId = nextLessonId,
                CourseId = session.Lesson.CourseId
            };

            return View(vm);
        }

        private IQueryable<Exercise> GetPlayableExercisesQuery(int lessonId)
        {
            return db.Exercises.Where(x =>
                x.LessonId == lessonId &&
                x.ExerciseType != null &&
                x.ExerciseType != "Speaking");
        }

        private ExercisePlayItemViewModel MapExercise(Exercise exercise)
        {
            var vm = new ExercisePlayItemViewModel
            {
                ExerciseId = exercise.ExerciseId,
                PracticeArea = exercise.PracticeArea,
                ExerciseType = string.IsNullOrWhiteSpace(exercise.ExerciseType) ? "MultipleChoice" : exercise.ExerciseType,
                PromptText = !string.IsNullOrWhiteSpace(exercise.PromptText) ? exercise.PromptText : exercise.QuestionText,
                MatchingPairsJson = exercise.MatchingPairsJson,
                SentenceTokensJson = exercise.SentenceTokensJson,
                AudioPromptPath = exercise.AudioPromptPath
            };

            if (!string.IsNullOrWhiteSpace(exercise.OptionA))
                vm.Options.Add(new ExerciseOptionViewModel { Value = "A", Text = exercise.OptionA });

            if (!string.IsNullOrWhiteSpace(exercise.OptionB))
                vm.Options.Add(new ExerciseOptionViewModel { Value = "B", Text = exercise.OptionB });

            if (!string.IsNullOrWhiteSpace(exercise.OptionC))
                vm.Options.Add(new ExerciseOptionViewModel { Value = "C", Text = exercise.OptionC });

            if (!string.IsNullOrWhiteSpace(exercise.OptionD))
                vm.Options.Add(new ExerciseOptionViewModel { Value = "D", Text = exercise.OptionD });

            return vm;
        }

        private async Task ApplyCompletionRewardsAsync(LessonSession session)
        {
            var totalAnswered = session.CorrectCount + session.WrongCount;
            var percentage = totalAnswered == 0
                ? 0
                : (int)Math.Round((session.CorrectCount * 100.0) / totalAnswered);

            var passed = percentage >= 60;

            var gameProfile = await db.StudentGameProfiles
                .FirstOrDefaultAsync(x => x.StudentId == session.StudentId);

            if (gameProfile == null)
            {
                gameProfile = new StudentGameProfile
                {
                    StudentId = session.StudentId,
                    TotalXP = 0,
                    CurrentStreakDays = 0,
                    LongestStreakDays = 0,
                    Hearts = 5
                };

                db.StudentGameProfiles.Add(gameProfile);
            }

            gameProfile.TotalXP += session.TotalXP;

            var todayUtc = DateTime.UtcNow.Date;
            var lastPracticeDate = gameProfile.LastPracticeDateUtc.HasValue
                ? gameProfile.LastPracticeDateUtc.Value.Date
                : (DateTime?)null;

            if (!lastPracticeDate.HasValue)
            {
                gameProfile.CurrentStreakDays = 1;
            }
            else if (lastPracticeDate.Value == todayUtc)
            {
                if (gameProfile.CurrentStreakDays <= 0)
                    gameProfile.CurrentStreakDays = 1;
            }
            else if (lastPracticeDate.Value == todayUtc.AddDays(-1))
            {
                gameProfile.CurrentStreakDays = Math.Max(1, gameProfile.CurrentStreakDays + 1);
            }
            else
            {
                gameProfile.CurrentStreakDays = 1;
            }

            if (gameProfile.CurrentStreakDays > gameProfile.LongestStreakDays)
            {
                gameProfile.LongestStreakDays = gameProfile.CurrentStreakDays;
            }

            gameProfile.LastPracticeDateUtc = todayUtc;
            gameProfile.Hearts = 5;

            if (passed)
            {
                await SaveLessonCompletionAndProgress(session.StudentId, session.LessonId);
            }

            session.RewardsApplied = true;
        }

        private async Task SaveLessonCompletionAndProgress(string studentId, int lessonId)
        {
            var lesson = await db.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.LessonId == lessonId);

            if (lesson == null)
                return;

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

            await db.SaveChangesAsync();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}