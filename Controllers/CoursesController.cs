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
    public class CoursesController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> Index()
        {
            var courses = await db.Courses
                .AsNoTracking()
                .Include(c => c.Lessons)
                .OrderBy(c => c.Title)
                .ToListAsync();

            var vm = new List<CourseCatalogItemViewModel>();

            string userId = User.Identity.GetUserId();
            bool isStudent = Request.IsAuthenticated && User.IsInRole("Student");

            Dictionary<int, StudentCourse> studentCourseMap = new Dictionary<int, StudentCourse>();
            Dictionary<int, Progress> progressMap = new Dictionary<int, Progress>();
            HashSet<int> completedLessonSet = new HashSet<int>();

            if (isStudent)
            {
                var studentCourses = await db.StudentCourses
                    .Where(sc => sc.StudentId == userId)
                    .ToListAsync();

                studentCourseMap = studentCourses.ToDictionary(x => x.CourseId, x => x);

                var progresses = await db.Progresses
                    .Where(p => p.StudentId == userId)
                    .ToListAsync();

                progressMap = progresses.ToDictionary(x => x.CourseId, x => x);

                var completedLessonIds = await db.StudentLessonProgresses
                    .Where(p => p.StudentId == userId && p.IsCompleted)
                    .Select(p => p.LessonId)
                    .ToListAsync();

                completedLessonSet = new HashSet<int>(completedLessonIds);
            }

            foreach (var course in courses)
            {
                var orderedLessons = course.Lessons
                    .OrderBy(l => l.LessonId)
                    .ToList();

                var lessonIds = orderedLessons.Select(l => l.LessonId).ToList();
                int totalLessons = lessonIds.Count;

                bool isEnrolled = studentCourseMap.ContainsKey(course.CourseId);
                int completedLessons = 0;
                int progressPercent = 0;
                bool isCompleted = false;
                int? continueLessonId = null;

                if (isStudent && isEnrolled)
                {
                    var enrollment = studentCourseMap[course.CourseId];
                    isCompleted = enrollment.IsCompleted;
                    continueLessonId = enrollment.CurrentLessonId;

                    if (progressMap.ContainsKey(course.CourseId))
                    {
                        completedLessons = progressMap[course.CourseId].CompletedLessons;
                        progressPercent = progressMap[course.CourseId].CompletionRate;
                    }
                    else
                    {
                        completedLessons = lessonIds.Count(id => completedLessonSet.Contains(id));
                        progressPercent = totalLessons == 0
                            ? 0
                            : (int)Math.Floor(completedLessons * 100.0 / totalLessons);
                    }

                    if (!continueLessonId.HasValue && totalLessons > 0 && !isCompleted)
                    {
                        continueLessonId = orderedLessons
                            .Where(l => !completedLessonSet.Contains(l.LessonId))
                            .Select(l => (int?)l.LessonId)
                            .FirstOrDefault();
                    }
                }

                vm.Add(new CourseCatalogItemViewModel
                {
                    CourseId = course.CourseId,
                    Title = course.Title,
                    Language = course.Language,
                    Level = course.Level,
                    EstimatedDuration = course.EstimatedDuration,
                    Description = course.Description,
                    LessonCount = totalLessons,
                    IsEnrolled = isEnrolled,
                    IsCompleted = isCompleted,
                    ProgressPercent = progressPercent,
                    ContinueLessonId = continueLessonId
                });
            }

            return View(vm);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> Details(int id)
        {
            var course = await db.Courses
                .AsNoTracking()
                .Include(c => c.Lessons)
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (course == null)
                return HttpNotFound();

            var vm = new CourseDetailsViewModel
            {
                CourseId = course.CourseId,
                Title = course.Title,
                Language = course.Language,
                Level = course.Level,
                EstimatedDuration = course.EstimatedDuration,
                Description = course.Description
            };

            string userId = User.Identity.GetUserId();
            bool isStudent = Request.IsAuthenticated && User.IsInRole("Student");

            HashSet<int> completedLessonSet = new HashSet<int>();

            if (isStudent)
            {
                var enrollment = await db.StudentCourses
                    .FirstOrDefaultAsync(sc => sc.StudentId == userId && sc.CourseId == id);

                if (enrollment != null)
                {
                    vm.IsEnrolled = true;
                    vm.IsCompleted = enrollment.IsCompleted;
                    vm.ContinueLessonId = enrollment.CurrentLessonId;
                }

                var completedLessonIds = await db.StudentLessonProgresses
                    .Where(p => p.StudentId == userId && p.IsCompleted && p.Lesson.CourseId == id)
                    .Select(p => p.LessonId)
                    .ToListAsync();

                completedLessonSet = new HashSet<int>(completedLessonIds);
            }

            var orderedLessons = course.Lessons
                .OrderBy(l => l.LessonId)
                .ToList();

            vm.Lessons = orderedLessons
                .Select(l => new CourseLessonItemViewModel
                {
                    LessonId = l.LessonId,
                    Title = l.Title,
                    DifficultyLevel = l.DifficultyLevel,
                    IsCompleted = completedLessonSet.Contains(l.LessonId)
                })
                .ToList();

            vm.ProgressPercent = vm.Lessons.Count == 0
                ? 0
                : (int)Math.Floor(vm.Lessons.Count(x => x.IsCompleted) * 100.0 / vm.Lessons.Count);

            if (vm.IsEnrolled && !vm.ContinueLessonId.HasValue && !vm.IsCompleted)
            {
                vm.ContinueLessonId = vm.Lessons
                    .Where(l => !l.IsCompleted)
                    .OrderBy(l => l.LessonId)
                    .Select(l => (int?)l.LessonId)
                    .FirstOrDefault();
            }

            return View(vm);
        }

        [HttpPost]
        [Authorize(Roles = "Student")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Enroll(int id)
        {
            var userId = User.Identity.GetUserId();

            var course = await db.Courses
                .Include(c => c.Lessons)
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (course == null)
                return HttpNotFound();

            var existing = await db.StudentCourses
                .FirstOrDefaultAsync(sc => sc.StudentId == userId && sc.CourseId == id);

            if (existing == null)
            {
                var firstLessonId = course.Lessons
                    .OrderBy(l => l.LessonId)
                    .Select(l => (int?)l.LessonId)
                    .FirstOrDefault();

                db.StudentCourses.Add(new StudentCourse
                {
                    StudentId = userId,
                    CourseId = id,
                    EnrolledAt = DateTime.UtcNow,
                    CurrentLessonId = firstLessonId,
                    IsCompleted = false
                });

                await db.SaveChangesAsync();
                TempData["CourseMessage"] = "You have enrolled successfully.";
            }
            else
            {
                TempData["CourseMessage"] = "You are already enrolled in this course.";
            }

            return RedirectToAction("Details", new { id });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}