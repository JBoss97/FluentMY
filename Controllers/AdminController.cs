using FluentMY.Models;
using FluentMY.ViewModels;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace FluentMY.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        public async Task<ActionResult> Index()
        {
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
            var users = await db.Users.ToListAsync();

            int totalStudents = 0;
            int totalEducators = 0;
            int totalAdmins = 0;

            foreach (var user in users)
            {
                var roles = await userManager.GetRolesAsync(user.Id);

                if (roles.Contains("Student")) totalStudents++;
                if (roles.Contains("Educator")) totalEducators++;
                if (roles.Contains("Admin")) totalAdmins++;
            }

            var totalCourses = await db.Courses.CountAsync();
            var totalLessons = await db.Lessons.CountAsync();
            var totalExercises = await db.Exercises.CountAsync();
            var totalEnrollments = await db.StudentCourses.CountAsync();
            var totalSubmissions = await db.Submissions.CountAsync();
            var pendingReviewCount = await db.Submissions.CountAsync(s => !s.IsReviewed);

            var totalEnquiries = await db.ContactEnquiries.CountAsync();
            var unreadEnquiriesCount = await db.ContactEnquiries.CountAsync(x => !x.IsRead);
            var totalAuditLogs = await db.AdminAuditLogs.CountAsync();

            var unassignedCoursesCount = await db.Courses.CountAsync(c => c.EducatorId == null);
            var coursesWithoutLessonsCount = await db.Courses.CountAsync(c => !c.Lessons.Any());
            var lessonsWithoutExercisesCount = await db.Lessons.CountAsync(l => !l.Exercises.Any());

            var vm = new AdminDashboardViewModel
            {
                TotalUsers = users.Count,
                TotalStudents = totalStudents,
                TotalEducators = totalEducators,
                TotalAdmins = totalAdmins,

                TotalCourses = totalCourses,
                TotalLessons = totalLessons,
                TotalExercises = totalExercises,

                TotalEnrollments = totalEnrollments,
                TotalSubmissions = totalSubmissions,
                PendingReviewCount = pendingReviewCount,

                TotalEnquiries = totalEnquiries,
                UnreadEnquiriesCount = unreadEnquiriesCount,
                TotalAuditLogs = totalAuditLogs,

                RecentSubmissions = await db.Submissions
                    .AsNoTracking()
                    .Include(s => s.Student)
                    .Include(s => s.Lesson)
                    .Include(s => s.Lesson.Course)
                    .OrderByDescending(s => s.SubmittedAt)
                    .Take(8)
                    .Select(s => new AdminRecentSubmissionViewModel
                    {
                        SubmissionId = s.SubmissionId,
                        StudentName = s.Student.FullName != null && s.Student.FullName != ""
                            ? s.Student.FullName
                            : s.Student.Email,
                        CourseTitle = s.Lesson.Course.Title,
                        LessonTitle = s.Lesson.Title,
                        Percentage = s.Percentage,
                        IsReviewed = s.IsReviewed,
                        SubmittedAt = s.SubmittedAt
                    })
                    .ToListAsync(),

                RecentEnquiries = await db.ContactEnquiries
                    .AsNoTracking()
                    .OrderBy(x => x.IsRead)
                    .ThenByDescending(x => x.SubmittedAtUtc)
                    .Take(6)
                    .Select(x => new AdminRecentEnquiryItemViewModel
                    {
                        ContactEnquiryId = x.ContactEnquiryId,
                        FullName = x.FullName,
                        Email = x.Email,
                        Role = x.Role,
                        Subject = x.Subject,
                        IsRead = x.IsRead,
                        SubmittedAtUtc = x.SubmittedAtUtc
                    })
                    .ToListAsync(),

                RecentAuditLogs = await db.AdminAuditLogs
                    .AsNoTracking()
                    .OrderByDescending(x => x.CreatedAtUtc)
                    .Take(8)
                    .Select(x => new AdminAuditLogItemViewModel
                    {
                        AdminAuditLogId = x.AdminAuditLogId,
                        AdminUserName = x.AdminUserName,
                        ActionType = x.ActionType,
                        EntityType = x.EntityType,
                        EntityDisplayName = x.EntityDisplayName,
                        Details = x.Details,
                        CreatedAtUtc = x.CreatedAtUtc
                    })
                    .ToListAsync()
            };

            vm.QuickLinks = new List<AdminQuickLinkViewModel>
            {
                new AdminQuickLinkViewModel
                {
                    Title = "Manage Users",
                    Description = "Create, edit, reset passwords, manage roles, and apply bulk actions safely.",
                    ActionText = "Open User Management",
                    ActionUrl = Url.Action("Index", "AdminUsers"),
                    BadgeText = vm.TotalUsers + " users",
                    ThemeClass = "theme-indigo"
                },
                new AdminQuickLinkViewModel
                {
                    Title = "Manage Courses",
                    Description = "Review course ownership, structure, student engagement, and learning activity.",
                    ActionText = "Open Course Oversight",
                    ActionUrl = Url.Action("Index", "AdminCourses"),
                    BadgeText = vm.TotalCourses + " courses",
                    ThemeClass = "theme-blue"
                },
                new AdminQuickLinkViewModel
                {
                    Title = "View Reports",
                    Description = "Monitor KPIs, rankings, submissions, and export admin CSV reports.",
                    ActionText = "Open Reports",
                    ActionUrl = Url.Action("Index", "AdminReports"),
                    BadgeText = "Analytics",
                    ThemeClass = "theme-green"
                },
                new AdminQuickLinkViewModel
                {
                    Title = "Pending Reviews",
                    Description = "Track submissions that are still waiting for educator feedback and review.",
                    ActionText = "View Reports",
                    ActionUrl = Url.Action("Index", "AdminReports"),
                    BadgeText = vm.PendingReviewCount + " pending",
                    ThemeClass = "theme-amber"
                },
                new AdminQuickLinkViewModel
                {
                    Title = "Manage Enquiries",
                    Description = "Review contact form messages submitted by learners, educators, and visitors.",
                    ActionText = "Open Enquiries",
                    ActionUrl = Url.Action("Index", "AdminEnquiries"),
                    BadgeText = vm.UnreadEnquiriesCount > 0 ? vm.UnreadEnquiriesCount + " unread" : "Up to date",
                    ThemeClass = "theme-amber"
                },
                new AdminQuickLinkViewModel
                {
                    Title = "Audit Logs",
                    Description = "View recent system actions and administrative activity records.",
                    ActionText = "Open Audit Logs",
                    ActionUrl = Url.Action("Index", "AdminAuditLogs"),
                    BadgeText = vm.TotalAuditLogs + " logs",
                    ThemeClass = "theme-slate"
                }
            };

            if (vm.PendingReviewCount > 0)
            {
                vm.Alerts.Add(new AdminDashboardAlertViewModel
                {
                    Title = "Submission review backlog detected",
                    Message = "There are student submissions still waiting for educator review.",
                    Severity = "warning",
                    CountText = vm.PendingReviewCount + " pending reviews",
                    ActionText = "Open Reports",
                    ActionUrl = Url.Action("Index", "AdminReports")
                });
            }

            if (vm.UnreadEnquiriesCount > 0)
            {
                vm.Alerts.Add(new AdminDashboardAlertViewModel
                {
                    Title = "Unread enquiries waiting",
                    Message = "There are new enquiries submitted through the Contact Us page that need admin attention.",
                    Severity = vm.UnreadEnquiriesCount >= 5 ? "warning" : "info",
                    CountText = vm.UnreadEnquiriesCount + " unread enquiries",
                    ActionText = "Open Enquiries",
                    ActionUrl = Url.Action("Index", "AdminEnquiries")
                });
            }

            if (unassignedCoursesCount > 0)
            {
                vm.Alerts.Add(new AdminDashboardAlertViewModel
                {
                    Title = "Unassigned courses found",
                    Message = "Some courses do not currently have an educator owner assigned.",
                    Severity = "danger",
                    CountText = unassignedCoursesCount + " unassigned courses",
                    ActionText = "Open Course Oversight",
                    ActionUrl = Url.Action("Index", "AdminCourses")
                });
            }

            if (coursesWithoutLessonsCount > 0)
            {
                vm.Alerts.Add(new AdminDashboardAlertViewModel
                {
                    Title = "Courses without lessons",
                    Message = "Some courses exist but do not yet contain lesson content.",
                    Severity = "warning",
                    CountText = coursesWithoutLessonsCount + " courses need lessons",
                    ActionText = "Check Courses",
                    ActionUrl = Url.Action("Index", "AdminCourses")
                });
            }

            if (lessonsWithoutExercisesCount > 0)
            {
                vm.Alerts.Add(new AdminDashboardAlertViewModel
                {
                    Title = "Lessons without exercises",
                    Message = "Some lessons do not yet have assessment or exercise content attached.",
                    Severity = "info",
                    CountText = lessonsWithoutExercisesCount + " lessons need exercises",
                    ActionText = "Review Content",
                    ActionUrl = Url.Action("Index", "AdminCourses")
                });
            }

            if (vm.TotalAuditLogs == 0)
            {
                vm.Alerts.Add(new AdminDashboardAlertViewModel
                {
                    Title = "Audit log not active",
                    Message = "No audit log entries have been recorded yet. Verify logging is enabled for important admin actions.",
                    Severity = "warning",
                    CountText = "0 audit entries",
                    ActionText = "Open Audit Logs",
                    ActionUrl = Url.Action("Index", "AdminAuditLogs")
                });
            }

            return View(vm);
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