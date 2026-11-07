using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using System.Threading.Tasks;
using System.Web.Mvc;
using FluentMY.Models;
using Microsoft.AspNet.Identity;

namespace FluentMY.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentCoursesController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        public async Task<ActionResult> MyCourses()
        {
            var userId = User.Identity.GetUserId();

            var my = await db.StudentCourses
                .Include(sc => sc.Course)
                .Where(sc => sc.StudentId == userId)
                .OrderByDescending(sc => sc.EnrolledAt)
                .ToListAsync();

            return View(my);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Enroll(int courseId)
        {
            var userId = User.Identity.GetUserId();

            var sc = await db.StudentCourses
                .FirstOrDefaultAsync(x => x.StudentId == userId && x.CourseId == courseId);

            if (sc == null)
            {
                var firstLesson = await db.Lessons
                    .Where(l => l.CourseId == courseId)
                    .OrderBy(l => l.LessonId)
                    .FirstOrDefaultAsync();

                sc = new StudentCourse
                {
                    StudentId = userId,
                    CourseId = courseId,
                    CurrentLessonId = firstLesson?.LessonId,
                    IsCompleted = false
                };

                db.StudentCourses.Add(sc);
                await db.SaveChangesAsync();
            }

            return RedirectToAction("Details", "Courses", new { id = courseId });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}