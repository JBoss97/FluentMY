using FluentMY.Models;
using FluentMY.ViewModels;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace FluentMY.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminCoursesController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();
        private const int PageSize = 10;

        public async Task<ActionResult> Index(string search, string educatorId, string sort = "title_asc", int page = 1)
        {
            search = (search ?? string.Empty).Trim();
            educatorId = (educatorId ?? string.Empty).Trim();
            sort = string.IsNullOrWhiteSpace(sort) ? "title_asc" : sort;
            page = page < 1 ? 1 : page;

            var query = db.Courses
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c =>
                    c.Title.Contains(search) ||
                    c.Description.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(educatorId))
            {
                query = query.Where(c => c.EducatorId == educatorId);
            }

            var totalCourses = await query.CountAsync();

            var courseIdsQuery = query.Select(c => c.CourseId);

            var totalLessons = await db.Lessons
                .CountAsync(l => courseIdsQuery.Contains(l.CourseId));

            var totalEnrollments = await db.StudentCourses
                .CountAsync(sc => courseIdsQuery.Contains(sc.CourseId));

            var totalSubmissions = await db.Submissions
                .CountAsync(s => courseIdsQuery.Contains(s.Lesson.CourseId));

            var projected = query.Select(c => new AdminCourseListItemViewModel
            {
                CourseId = c.CourseId,
                Title = c.Title,
                Description = c.Description,
                EducatorId = c.EducatorId,
                EducatorName = c.EducatorId == null
                    ? "Unassigned"
                    : (c.Educator.FullName ?? c.Educator.UserName),

                LessonCount = c.Lessons.Count(),

                ExerciseCount = c.Lessons
                    .SelectMany(l => l.Exercises)
                    .Count(),

                EnrollmentCount = db.StudentCourses
                    .Count(sc => sc.CourseId == c.CourseId),

                SubmissionCount = db.Submissions
                    .Count(s => s.Lesson.CourseId == c.CourseId),

                AveragePercentage = db.Submissions
                    .Where(s => s.Lesson.CourseId == c.CourseId)
                    .Select(s => (decimal?)s.Percentage)
                    .Average() ?? 0m
            });

            IOrderedQueryable<AdminCourseListItemViewModel> ordered;

            switch (sort)
            {
                case "title_desc":
                    ordered = projected.OrderByDescending(c => c.Title);
                    break;

                case "educator_asc":
                    ordered = projected.OrderBy(c => c.EducatorName).ThenBy(c => c.Title);
                    break;

                case "educator_desc":
                    ordered = projected.OrderByDescending(c => c.EducatorName).ThenBy(c => c.Title);
                    break;

                case "lessons_desc":
                    ordered = projected.OrderByDescending(c => c.LessonCount).ThenBy(c => c.Title);
                    break;

                case "enrollments_desc":
                    ordered = projected.OrderByDescending(c => c.EnrollmentCount).ThenBy(c => c.Title);
                    break;

                case "submissions_desc":
                    ordered = projected.OrderByDescending(c => c.SubmissionCount).ThenBy(c => c.Title);
                    break;

                case "score_desc":
                    ordered = projected.OrderByDescending(c => c.AveragePercentage).ThenBy(c => c.Title);
                    break;

                default:
                    ordered = projected.OrderBy(c => c.Title);
                    break;
            }

            var totalPages = totalCourses == 0
                ? 1
                : (int)Math.Ceiling(totalCourses / (double)PageSize);

            if (page > totalPages)
            {
                page = totalPages;
            }

            var items = await ordered
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            var educatorOptions = await db.Courses
                .AsNoTracking()
                .Where(c => c.EducatorId != null)
                .Select(c => new
                {
                    c.EducatorId,
                    Name = c.Educator.FullName ?? c.Educator.UserName
                })
                .GroupBy(x => new { x.EducatorId, x.Name })
                .Select(g => g.Key)
                .OrderBy(x => x.Name)
                .ToListAsync();

            var model = new AdminCourseIndexViewModel
            {
                Search = search,
                EducatorId = educatorId,
                Sort = sort,
                Page = page,
                TotalPages = totalPages,
                TotalCourses = totalCourses,
                TotalLessons = totalLessons,
                TotalEnrollments = totalEnrollments,
                TotalSubmissions = totalSubmissions,
                Courses = items,
                Educators = educatorOptions.Select(x => new SelectListItem
                {
                    Text = x.Name,
                    Value = x.EducatorId,
                    Selected = x.EducatorId == educatorId
                }).ToList()
            };

            return View(model);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}