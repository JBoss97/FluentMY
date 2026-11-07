using FluentMY.Models;
using Microsoft.AspNet.Identity;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace FluentMY.Controllers
{
    [Authorize(Roles = "Educator")]
    public class EducatorLessonsController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        public async Task<ActionResult> Index(int? courseId)
        {
            var educatorId = User.Identity.GetUserId();

            var lessons = db.Lessons
                .Include(l => l.Course)
                .Where(l => l.EducatorId == educatorId)
                .AsQueryable();

            if (courseId.HasValue)
            {
                bool ownsCourse = await db.Courses
                    .AnyAsync(c => c.CourseId == courseId.Value && c.EducatorId == educatorId);

                if (!ownsCourse)
                    return HttpNotFound();

                lessons = lessons.Where(l => l.CourseId == courseId.Value);
            }

            ViewBag.SelectedCourseId = courseId;

            return View(await lessons
                .OrderByDescending(l => l.UpdatedAt ?? l.CreatedAt)
                .ThenBy(l => l.Course.Title)
                .ThenBy(l => l.Title)
                .ToListAsync());
        }

        [HttpGet]
        public async Task<ActionResult> Create(int? courseId)
        {
            var educatorId = User.Identity.GetUserId();

            var myCourses = await GetEducatorCoursesAsync(educatorId);

            if (!myCourses.Any())
            {
                TempData["EducatorLessonMessage"] = "Please create a course first before adding lessons.";
                return RedirectToAction("Create", "EducatorCourses");
            }

            ViewBag.CourseId = new SelectList(myCourses, "CourseId", "Title", courseId);

            return View(new Lesson
            {
                EducatorId = educatorId,
                CourseId = courseId ?? 0
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(Lesson lesson)
        {
            var educatorId = User.Identity.GetUserId();

            lesson.EducatorId = educatorId;
            ModelState.Remove("EducatorId");

            bool ownsCourse = await db.Courses
                .AnyAsync(c => c.CourseId == lesson.CourseId && c.EducatorId == educatorId);

            if (!ownsCourse)
            {
                ModelState.AddModelError("CourseId", "Please select one of your own courses.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.CourseId = new SelectList(
                    await GetEducatorCoursesAsync(educatorId),
                    "CourseId",
                    "Title",
                    lesson.CourseId);

                return View(lesson);
            }

            lesson.CreatedAt = System.DateTime.UtcNow;
            lesson.UpdatedAt = null;

            db.Lessons.Add(lesson);
            await db.SaveChangesAsync();

            TempData["EducatorLessonMessage"] = "Lesson created successfully.";
            return RedirectToAction("Index", new { courseId = lesson.CourseId });
        }

        [HttpGet]
        public async Task<ActionResult> Edit(int id)
        {
            var educatorId = User.Identity.GetUserId();

            var lesson = await db.Lessons
                .FirstOrDefaultAsync(l => l.LessonId == id && l.EducatorId == educatorId);

            if (lesson == null)
                return HttpNotFound();

            ViewBag.CourseId = new SelectList(
                await GetEducatorCoursesAsync(educatorId),
                "CourseId",
                "Title",
                lesson.CourseId);

            return View(lesson);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Lesson lesson)
        {
            var educatorId = User.Identity.GetUserId();

            lesson.EducatorId = educatorId;
            ModelState.Remove("EducatorId");

            var existingLesson = await db.Lessons
                .FirstOrDefaultAsync(l => l.LessonId == lesson.LessonId && l.EducatorId == educatorId);

            if (existingLesson == null)
                return HttpNotFound();

            bool ownsCourse = await db.Courses
                .AnyAsync(c => c.CourseId == lesson.CourseId && c.EducatorId == educatorId);

            if (!ownsCourse)
            {
                ModelState.AddModelError("CourseId", "Please select one of your own courses.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.CourseId = new SelectList(
                    await GetEducatorCoursesAsync(educatorId),
                    "CourseId",
                    "Title",
                    lesson.CourseId);

                return View(lesson);
            }

            existingLesson.Title = lesson.Title;
            existingLesson.DifficultyLevel = lesson.DifficultyLevel;
            existingLesson.Content = lesson.Content;
            existingLesson.CourseId = lesson.CourseId;
            existingLesson.EducatorId = educatorId;
            existingLesson.UpdatedAt = System.DateTime.UtcNow;

            await db.SaveChangesAsync();

            TempData["EducatorLessonMessage"] = "Lesson updated successfully.";
            return RedirectToAction("Index", new { courseId = existingLesson.CourseId });
        }

        [HttpGet]
        public async Task<ActionResult> Delete(int id)
        {
            var educatorId = User.Identity.GetUserId();

            var lesson = await db.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.LessonId == id && l.EducatorId == educatorId);

            if (lesson == null)
                return HttpNotFound();

            return View(lesson);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            var educatorId = User.Identity.GetUserId();

            var lesson = await db.Lessons
                .FirstOrDefaultAsync(l => l.LessonId == id && l.EducatorId == educatorId);

            if (lesson == null)
                return HttpNotFound();

            int courseId = lesson.CourseId;

            bool hasSubmissions = await db.Submissions.AnyAsync(s => s.LessonId == id);
            if (hasSubmissions)
            {
                TempData["EducatorLessonMessage"] = "You cannot delete a lesson that already has student submissions.";
                return RedirectToAction("Index", new { courseId });
            }

            var exercises = await db.Exercises
                .Where(e => e.LessonId == id)
                .ToListAsync();

            if (exercises.Any())
            {
                db.Exercises.RemoveRange(exercises);
            }

            db.Lessons.Remove(lesson);
            await db.SaveChangesAsync();

            TempData["EducatorLessonMessage"] = "Lesson deleted successfully.";
            return RedirectToAction("Index", new { courseId });
        }

        private async Task<List<Course>> GetEducatorCoursesAsync(string educatorId)
        {
            return await db.Courses
                .Where(c => c.EducatorId == educatorId)
                .OrderBy(c => c.Title)
                .ToListAsync();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}