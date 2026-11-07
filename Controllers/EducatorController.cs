using FluentMY.Models;
using FluentMY.ViewModels;
using Microsoft.AspNet.Identity;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace FluentMY.Controllers
{
    [Authorize(Roles = "Educator")]
    public class EducatorController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        public async Task<ActionResult> Index()
        {
            var educatorId = User.Identity.GetUserId();

            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == educatorId);

            var myCourseIds = await db.Courses
                .Where(c => c.EducatorId == educatorId)
                .Select(c => c.CourseId)
                .ToListAsync();

            var myLessonIds = await db.Lessons
                .Where(l => l.EducatorId == educatorId)
                .Select(l => l.LessonId)
                .ToListAsync();

            var vm = new EducatorDashboardViewModel
            {
                EducatorDisplayName = user != null
                    ? (!string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : user.Email)
                    : User.Identity.Name,

                TotalCourses = myCourseIds.Count,

                TotalLessons = myLessonIds.Count,

                TotalExercises = await db.Exercises
                    .CountAsync(e => myLessonIds.Contains(e.LessonId)),

                TotalSubmissions = await db.Submissions
                    .CountAsync(s => myLessonIds.Contains(s.LessonId)),

                PendingReviewCount = await db.Submissions
                    .CountAsync(s => myLessonIds.Contains(s.LessonId) && !s.IsReviewed)
            };

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