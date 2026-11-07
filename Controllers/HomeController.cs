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
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        public async Task<ActionResult> Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin"))
                    return RedirectToAction("Index", "Admin");

                if (User.IsInRole("Educator"))
                    return RedirectToAction("Index", "Educator");

                if (User.IsInRole("Student"))
                    return RedirectToAction("Index", "Student");
            }

            var courseQuery = db.Courses
                .AsNoTracking()
                .Include(c => c.Lessons)
                .OrderBy(c => c.CourseId);

            var recommendedCourse = await courseQuery
                .Select(c => new HomeCourseCardViewModel
                {
                    CourseId = c.CourseId,
                    Title = c.Title,
                    Language = c.Language,
                    Level = c.Level,
                    EstimatedDuration = c.EstimatedDuration,
                    Description = c.Description,
                    LessonCount = c.Lessons.Count()
                })
                .FirstOrDefaultAsync();

            var popularCourses = await courseQuery
                .Take(4)
                .Select(c => new HomeCourseCardViewModel
                {
                    CourseId = c.CourseId,
                    Title = c.Title,
                    Language = c.Language,
                    Level = c.Level,
                    EstimatedDuration = c.EstimatedDuration,
                    Description = c.Description,
                    LessonCount = c.Lessons.Count()
                })
                .ToListAsync();

            var vm = new HomeIndexViewModel
            {
                RecommendedCourse = recommendedCourse,
                PopularCourses = popularCourses
            };

            return View(vm);
        }

        public ActionResult About()
        {
            ViewBag.Message = "FluentMY helps learners build practical language skills through structured lessons, speaking practice, game-based activities, and a more relevant Malaysian learning context.";
            return View();
        }

        [HttpGet]
        public ActionResult Contact()
        {
            var vm = new ContactEnquiryViewModel();

            if (User.Identity.IsAuthenticated)
            {
                var userId = User.Identity.GetUserId();
                var user = db.Users.FirstOrDefault(x => x.Id == userId);

                if (user != null)
                {
                    vm.FullName = !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : "";
                    vm.Email = !string.IsNullOrWhiteSpace(user.Email) ? user.Email : "";

                    if (User.IsInRole("Student"))
                        vm.Role = "Student";
                    else if (User.IsInRole("Educator"))
                        vm.Role = "Educator";
                    else if (User.IsInRole("Admin"))
                        vm.Role = "Admin";
                    else
                        vm.Role = "Visitor";
                }
            }

            ViewBag.Message = "Reach out for learner support, educator enquiries, platform guidance, or general collaboration.";
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Contact(ContactEnquiryViewModel model)
        {
            ViewBag.Message = "Reach out for learner support, educator enquiries, platform guidance, or general collaboration.";

            if (!ModelState.IsValid)
                return View(model);

            var enquiry = new ContactEnquiry
            {
                FullName = model.FullName,
                Email = model.Email,
                Subject = model.Subject,
                Role = model.Role,
                Message = model.Message,
                SubmittedAtUtc = DateTime.UtcNow,
                IsRead = false
            };

            db.ContactEnquiries.Add(enquiry);
            await db.SaveChangesAsync();

            TempData["ContactSuccess"] = "Your enquiry has been submitted successfully. Our admin team will review it soon.";

            return RedirectToAction("Contact");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}