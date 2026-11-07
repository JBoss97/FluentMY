using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using FluentMY.Models;
using FluentMY.ViewModels;
using Microsoft.AspNet.Identity;

namespace FluentMY.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        private class LessonLite
        {
            public int LessonId { get; set; }
            public int CourseId { get; set; }
            public string Title { get; set; }
        }

        public async Task<ActionResult> Index()
        {
            var userId = User.Identity.GetUserId();

            var user = await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            var enrollments = await db.StudentCourses
                .Include(sc => sc.Course)
                .Where(sc => sc.StudentId == userId)
                .OrderBy(sc => sc.IsCompleted)
                .ThenByDescending(sc => sc.EnrolledAt)
                .ToListAsync();

            var enrolledCourseIds = enrollments
                .Select(e => e.CourseId)
                .Distinct()
                .ToList();

            var lessons = await db.Lessons
                .Where(l => enrolledCourseIds.Contains(l.CourseId))
                .OrderBy(l => l.CourseId)
                .ThenBy(l => l.LessonId)
                .Select(l => new LessonLite
                {
                    LessonId = l.LessonId,
                    CourseId = l.CourseId,
                    Title = l.Title
                })
                .ToListAsync();

            var lessonIds = lessons.Select(x => x.LessonId).ToList();

            var completedLessonIds = await db.StudentLessonProgresses
                .Where(p => p.StudentId == userId && p.IsCompleted && lessonIds.Contains(p.LessonId))
                .Select(p => p.LessonId)
                .ToListAsync();

            var completedLessonSet = new HashSet<int>(completedLessonIds);

            var progressMap = await db.Progresses
                .Where(p => p.StudentId == userId && enrolledCourseIds.Contains(p.CourseId))
                .ToDictionaryAsync(p => p.CourseId);

            int totalLessons = lessons.Count;
            int completedLessons = completedLessonIds.Count;

            int overallPercent = totalLessons == 0
                ? 0
                : (int)Math.Floor(completedLessons * 100.0 / totalLessons);

            var courseLessonGroups = lessons
                .GroupBy(l => l.CourseId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(x => x.LessonId).ToList()
                );

            int? continueLessonId = null;
            string continueLessonTitle = null;
            string continueCourseTitle = null;

            var bestEnrollment = enrollments
                .Where(e => !e.IsCompleted)
                .OrderByDescending(e => e.EnrolledAt)
                .FirstOrDefault();

            if (bestEnrollment != null)
            {
                if (bestEnrollment.CurrentLessonId.HasValue)
                {
                    continueLessonId = bestEnrollment.CurrentLessonId.Value;
                }
                else if (courseLessonGroups.ContainsKey(bestEnrollment.CourseId))
                {
                    continueLessonId = courseLessonGroups[bestEnrollment.CourseId]
                        .Where(l => !completedLessonSet.Contains(l.LessonId))
                        .Select(l => (int?)l.LessonId)
                        .FirstOrDefault();
                }

                if (continueLessonId.HasValue)
                {
                    continueCourseTitle = bestEnrollment.Course != null ? bestEnrollment.Course.Title : null;

                    var continueLesson = lessons.FirstOrDefault(x => x.LessonId == continueLessonId.Value);
                    continueLessonTitle = continueLesson != null ? continueLesson.Title : null;
                }
            }

            var vm = new StudentDashboardViewModel
            {
                FullNameOrEmail = string.IsNullOrWhiteSpace(user?.FullName) ? user?.Email : user.FullName,

                ContinueLessonId = continueLessonId,
                ContinueLessonTitle = continueLessonTitle,
                ContinueCourseTitle = continueCourseTitle,

                EnrolledCoursesCount = enrollments.Count,
                CompletedCoursesCount = enrollments.Count(e => e.IsCompleted),

                TotalLessonsInEnrolledCourses = totalLessons,
                CompletedLessonsInEnrolledCourses = completedLessons,
                OverallProgressPercent = overallPercent
            };

            foreach (var enrollment in enrollments)
            {
                var lessonList = courseLessonGroups.ContainsKey(enrollment.CourseId)
                    ? courseLessonGroups[enrollment.CourseId]
                    : new List<LessonLite>();

                int total = lessonList.Count;

                int done;
                int percent;

                if (progressMap.ContainsKey(enrollment.CourseId))
                {
                    done = progressMap[enrollment.CourseId].CompletedLessons;
                    percent = progressMap[enrollment.CourseId].CompletionRate;
                }
                else
                {
                    done = lessonList.Count(x => completedLessonSet.Contains(x.LessonId));
                    percent = total == 0 ? 0 : (int)Math.Floor(done * 100.0 / total);
                }

                int? courseContinueLessonId = null;

                if (!enrollment.IsCompleted)
                {
                    if (enrollment.CurrentLessonId.HasValue)
                    {
                        courseContinueLessonId = enrollment.CurrentLessonId.Value;
                    }
                    else
                    {
                        courseContinueLessonId = lessonList
                            .Where(x => !completedLessonSet.Contains(x.LessonId))
                            .Select(x => (int?)x.LessonId)
                            .FirstOrDefault();
                    }
                }

                vm.MyCourses.Add(new MyCourseCard
                {
                    CourseId = enrollment.CourseId,
                    Title = enrollment.Course != null ? enrollment.Course.Title : "",
                    Language = enrollment.Course != null ? enrollment.Course.Language : "",
                    Level = enrollment.Course != null ? enrollment.Course.Level : "",
                    ProgressPercent = percent,
                    ContinueLessonId = courseContinueLessonId
                });
            }

            vm.MyCourses = vm.MyCourses
                .OrderBy(c => c.ProgressPercent >= 100)
                .ThenByDescending(c => c.ContinueLessonId.HasValue)
                .ThenBy(c => c.Title)
                .ToList();

            return View(vm);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}
