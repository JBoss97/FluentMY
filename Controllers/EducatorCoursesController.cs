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
    public class EducatorCoursesController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        public async Task<ActionResult> Index()
        {
            var educatorId = User.Identity.GetUserId();

            var courses = await db.Courses
                .AsNoTracking()
                .Where(c => c.EducatorId == educatorId)
                .Select(c => new EducatorCourseViewModel
                {
                    CourseId = c.CourseId,
                    Title = c.Title,
                    Language = c.Language,
                    Level = c.Level,
                    EstimatedDuration = c.EstimatedDuration,
                    Description = c.Description,
                    LessonCount = c.Lessons.Count(),
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .ToListAsync();

            // LastActivityAt is assumed to be a computed ViewModel property,
            // so sorting must happen in memory after materialization.
            courses = courses
                .OrderByDescending(c => c.LastActivityAt)
                .ThenBy(c => c.Title)
                .ToList();

            return View(courses);
        }

        [HttpGet]
        public ActionResult Create()
        {
            return View(new EducatorCourseViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(EducatorCourseViewModel model)
        {
            var educatorId = User.Identity.GetUserId();

            if (string.IsNullOrWhiteSpace(educatorId))
            {
                ModelState.AddModelError("", "Unable to identify the current educator account. Please sign in again.");
                return View(model);
            }

            NormalizeCourseModel(model);

            if (await HasDuplicateTitleAsync(model.Title, educatorId))
            {
                ModelState.AddModelError("Title", "You already have a course with this title.");
            }

            if (!ModelState.IsValid)
                return View(model);

            var now = DateTime.UtcNow;

            var course = new Course
            {
                Title = model.Title,
                Language = model.Language,
                Level = model.Level,
                EstimatedDuration = model.EstimatedDuration,
                Description = model.Description,
                EducatorId = educatorId,
                CreatedAt = now,
                UpdatedAt = null
            };

            db.Courses.Add(course);

            try
            {
                await db.SaveChangesAsync();
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                foreach (var entityValidationErrors in ex.EntityValidationErrors)
                {
                    foreach (var validationError in entityValidationErrors.ValidationErrors)
                    {
                        ModelState.AddModelError(validationError.PropertyName, validationError.ErrorMessage);
                    }
                }

                return View(model);
            }

            TempData["EducatorCourseMessage"] = "Course created successfully.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<ActionResult> Edit(int id)
        {
            var educatorId = User.Identity.GetUserId();

            if (string.IsNullOrWhiteSpace(educatorId))
            {
                return new HttpStatusCodeResult(401);
            }

            var course = await db.Courses
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CourseId == id && c.EducatorId == educatorId);

            if (course == null)
                return HttpNotFound();

            var vm = new EducatorCourseViewModel
            {
                CourseId = course.CourseId,
                Title = course.Title,
                Language = course.Language,
                Level = course.Level,
                EstimatedDuration = course.EstimatedDuration,
                Description = course.Description,
                CreatedAt = course.CreatedAt,
                UpdatedAt = course.UpdatedAt
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(EducatorCourseViewModel model)
        {
            var educatorId = User.Identity.GetUserId();

            var course = await db.Courses
                .FirstOrDefaultAsync(c => c.CourseId == model.CourseId && c.EducatorId == educatorId);

            if (course == null)
                return HttpNotFound();

            NormalizeCourseModel(model);

            if (await HasDuplicateTitleAsync(model.Title, educatorId, model.CourseId))
            {
                ModelState.AddModelError("Title", "You already have another course with this title.");
            }

            if (!ModelState.IsValid)
            {
                model.CreatedAt = course.CreatedAt;
                model.UpdatedAt = course.UpdatedAt;
                return View(model);
            }

            course.Title = model.Title;
            course.Language = model.Language;
            course.Level = model.Level;
            course.EstimatedDuration = model.EstimatedDuration;
            course.Description = model.Description;
            course.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            TempData["EducatorCourseMessage"] = "Course updated successfully.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<ActionResult> Delete(int id)
        {
            var educatorId = User.Identity.GetUserId();

            var course = await db.Courses
                .AsNoTracking()
                .Include(c => c.Lessons)
                .FirstOrDefaultAsync(c => c.CourseId == id && c.EducatorId == educatorId);

            if (course == null)
                return HttpNotFound();

            var vm = new EducatorCourseViewModel
            {
                CourseId = course.CourseId,
                Title = course.Title,
                Language = course.Language,
                Level = course.Level,
                EstimatedDuration = course.EstimatedDuration,
                Description = course.Description,
                LessonCount = course.Lessons.Count,
                CreatedAt = course.CreatedAt,
                UpdatedAt = course.UpdatedAt
            };

            return View(vm);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int courseId)
        {
            var educatorId = User.Identity.GetUserId();

            var course = await db.Courses
                .Include(c => c.Lessons)
                .FirstOrDefaultAsync(c => c.CourseId == courseId && c.EducatorId == educatorId);

            if (course == null)
                return HttpNotFound();

            if (course.Lessons.Any())
            {
                TempData["EducatorCourseMessage"] = "You cannot delete a course that still contains lessons. Please delete or move the lessons first.";
                return RedirectToAction("Index");
            }

            var enrollmentsExist = await db.StudentCourses.AnyAsync(sc => sc.CourseId == courseId);
            if (enrollmentsExist)
            {
                TempData["EducatorCourseMessage"] = "You cannot delete a course that already has student enrollments.";
                return RedirectToAction("Index");
            }

            db.Courses.Remove(course);
            await db.SaveChangesAsync();

            TempData["EducatorCourseMessage"] = "Course deleted successfully.";
            return RedirectToAction("Index");
        }

        private void NormalizeCourseModel(EducatorCourseViewModel model)
        {
            if (model == null) return;

            model.Title = model.Title?.Trim();
            model.Language = model.Language?.Trim();
            model.Level = model.Level?.Trim();
            model.EstimatedDuration = model.EstimatedDuration?.Trim();
            model.Description = model.Description?.Trim();
        }

        private async Task<bool> HasDuplicateTitleAsync(string title, string educatorId, int? excludeCourseId = null)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(educatorId))
                return false;

            var normalizedTitle = title.Trim().ToLower();

            var query = db.Courses.Where(c =>
                c.EducatorId == educatorId &&
                c.Title != null &&
                c.Title.ToLower() == normalizedTitle);

            if (excludeCourseId.HasValue)
            {
                query = query.Where(c => c.CourseId != excludeCourseId.Value);
            }

            return await query.AnyAsync();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}