using FluentMY.Models;
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
    public class LessonsController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        [HttpGet]
        [Authorize]
        public async Task<ActionResult> Details(int id)
        {
            var lesson = await db.Lessons
                .AsNoTracking()
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.LessonId == id);

            if (lesson == null)
                return HttpNotFound();

            var speakingExerciseRows = await db.Exercises
                .AsNoTracking()
                .Where(e => e.LessonId == lesson.LessonId && e.ExerciseType == "Speaking")
                .OrderBy(e => e.DisplayOrder)
                .ThenBy(e => e.ExerciseId)
                .Select(e => new
                {
                    e.ExerciseId,
                    e.PromptText,
                    e.QuestionText,
                    e.ReferenceText,
                    e.SyllableGuide,
                    e.PronunciationHint,
                    e.AudioPromptPath
                })
                .ToListAsync();

            var speakingExercises = speakingExerciseRows
                .Select(e => new LessonSpeakingExerciseItemViewModel
                {
                    ExerciseId = e.ExerciseId,
                    PromptText = !string.IsNullOrWhiteSpace(e.PromptText) ? e.PromptText : e.QuestionText,
                    ReferenceText = e.ReferenceText,
                    SyllableGuide = e.SyllableGuide,
                    PronunciationHint = e.PronunciationHint,
                    AudioPromptPath = e.AudioPromptPath
                })
                .ToList();

            var userId = User.Identity.GetUserId();

            if (User.IsInRole("Admin"))
            {
                return View(BuildLessonVm(lesson, speakingExercises, true));
            }

            if (User.IsInRole("Educator"))
            {
                if (lesson.EducatorId != userId)
                    return HttpNotFound();

                return View(BuildLessonVm(lesson, speakingExercises, true));
            }

            if (User.IsInRole("Student"))
            {
                var enrollment = await db.StudentCourses
                    .FirstOrDefaultAsync(sc => sc.StudentId == userId && sc.CourseId == lesson.CourseId);

                if (enrollment == null)
                {
                    TempData["CourseMessage"] = "Please enroll in the course before accessing the lesson.";
                    return RedirectToAction("Details", "Courses", new { id = lesson.CourseId });
                }

                var lessonProgress = await db.StudentLessonProgresses
                    .FirstOrDefaultAsync(p => p.StudentId == userId && p.LessonId == id);

                var nextLessonId = await db.Lessons
                    .Where(l => l.CourseId == lesson.CourseId)
                    .OrderBy(l => l.LessonId)
                    .Where(l => !db.StudentLessonProgresses.Any(p =>
                        p.StudentId == userId &&
                        p.LessonId == l.LessonId &&
                        p.IsCompleted))
                    .Select(l => (int?)l.LessonId)
                    .FirstOrDefaultAsync();

                var vm = BuildLessonVm(lesson, speakingExercises);
                vm.IsStudent = true;
                vm.IsEnrolled = true;
                vm.IsCompleted = lessonProgress != null && lessonProgress.IsCompleted;
                vm.CanStartQuiz = true;
                vm.NextLessonId = nextLessonId;

                return View(vm);
            }

            return RedirectToAction("Login", "Account");
        }

        [HttpPost]
        [Authorize(Roles = "Student")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Complete(int id)
        {
            var userId = User.Identity.GetUserId();

            var lesson = await db.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.LessonId == id);

            if (lesson == null)
                return HttpNotFound();

            var studentCourse = await db.StudentCourses
                .FirstOrDefaultAsync(sc => sc.StudentId == userId && sc.CourseId == lesson.CourseId);

            if (studentCourse == null)
            {
                TempData["CourseMessage"] = "Please enroll in the course before marking lessons as completed.";
                return RedirectToAction("Details", "Courses", new { id = lesson.CourseId });
            }

            var lessonProgress = await db.StudentLessonProgresses
                .FirstOrDefaultAsync(p => p.StudentId == userId && p.LessonId == id);

            if (lessonProgress == null)
            {
                lessonProgress = new StudentLessonProgress
                {
                    StudentId = userId,
                    LessonId = id,
                    IsCompleted = true,
                    CompletedAt = DateTime.UtcNow
                };
                db.StudentLessonProgresses.Add(lessonProgress);
            }
            else
            {
                lessonProgress.IsCompleted = true;
                lessonProgress.CompletedAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync();
            await UpdateCourseProgressAsync(userId, lesson.CourseId, lesson.LessonId);

            TempData["CourseMessage"] = "Lesson marked as completed.";
            return RedirectToAction("Details", "Courses", new { id = lesson.CourseId });
        }

        private LessonDetailsViewModel BuildLessonVm(
            Lesson lesson,
            List<LessonSpeakingExerciseItemViewModel> speakingExercises,
            bool isEducatorPreview = false)
        {
            return new LessonDetailsViewModel
            {
                LessonId = lesson.LessonId,
                CourseId = lesson.CourseId,
                LessonTitle = lesson.Title,
                CourseTitle = lesson.Course != null ? lesson.Course.Title : "",
                DifficultyLevel = lesson.DifficultyLevel,
                Content = lesson.Content,
                IsEducatorPreview = isEducatorPreview,
                CanStartQuiz = false,
                SpeakingExercises = speakingExercises ?? new List<LessonSpeakingExerciseItemViewModel>()
            };
        }

        private async Task UpdateCourseProgressAsync(string studentId, int courseId, int lastLessonId)
        {
            var lessonIds = await db.Lessons
                .Where(l => l.CourseId == courseId)
                .Select(l => l.LessonId)
                .ToListAsync();

            int totalLessons = lessonIds.Count;

            int completedLessons = await db.StudentLessonProgresses
                .CountAsync(p => p.StudentId == studentId && p.IsCompleted && lessonIds.Contains(p.LessonId));

            int completionRate = totalLessons == 0
                ? 0
                : (int)Math.Floor(completedLessons * 100.0 / totalLessons);

            int? nextLessonId = await db.Lessons
                .Where(l => l.CourseId == courseId)
                .OrderBy(l => l.LessonId)
                .Where(l => !db.StudentLessonProgresses.Any(p =>
                    p.StudentId == studentId &&
                    p.LessonId == l.LessonId &&
                    p.IsCompleted))
                .Select(l => (int?)l.LessonId)
                .FirstOrDefaultAsync();

            var studentCourse = await db.StudentCourses
                .FirstOrDefaultAsync(sc => sc.StudentId == studentId && sc.CourseId == courseId);

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
                .FirstOrDefaultAsync(p => p.StudentId == studentId && p.CourseId == courseId);

            if (progress == null)
            {
                db.Progresses.Add(new Progress
                {
                    StudentId = studentId,
                    CourseId = courseId,
                    TotalLessons = totalLessons,
                    CompletedLessons = completedLessons,
                    CompletionRate = completionRate,
                    LastLessonId = lastLessonId,
                    IsCourseCompleted = completionRate == 100,
                    LastUpdated = DateTime.UtcNow
                });
            }
            else
            {
                progress.TotalLessons = totalLessons;
                progress.CompletedLessons = completedLessons;
                progress.CompletionRate = completionRate;
                progress.LastLessonId = lastLessonId;
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