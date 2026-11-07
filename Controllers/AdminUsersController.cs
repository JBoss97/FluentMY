using FluentMY.Models;
using FluentMY.ViewModels;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace FluentMY.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminUsersController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        private RoleManager<IdentityRole> _roleManager;
        public RoleManager<IdentityRole> RoleManager
        {
            get
            {
                return _roleManager ?? new RoleManager<IdentityRole>(
                    new RoleStore<IdentityRole>(db));
            }
            private set { _roleManager = value; }
        }

        private ApplicationUserManager _userManager;
        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set { _userManager = value; }
        }

        public async Task<ActionResult> Index(
            string searchTerm,
            string selectedRole,
            bool? emailConfirmed,
            string sortBy = "email",
            string sortDirection = "asc",
            int page = 1,
            int pageSize = 10)
        {
            if (page < 1) page = 1;

            int[] allowedPageSizes = { 5, 10, 20, 50 };
            if (!allowedPageSizes.Contains(pageSize))
                pageSize = 10;

            var items = await BuildAdminUserListAsync();
            items = ApplyAdminUserFiltersAndSort(items, searchTerm, selectedRole, emailConfirmed, sortBy, sortDirection);

            int totalCount = items.Count;
            int totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);

            if (page > totalPages)
                page = totalPages;

            var pagedUsers = items
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var vm = new AdminUserIndexViewModel
            {
                SearchTerm = searchTerm,
                SelectedRole = selectedRole,
                EmailConfirmed = emailConfirmed,
                SortBy = sortBy,
                SortDirection = sortDirection,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                AvailableRoles = await GetRoleSelectList(),
                Users = pagedUsers
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<ActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return HttpNotFound();

            var user = await UserManager.FindByIdAsync(id);
            if (user == null)
                return HttpNotFound();

            var currentAdminId = User.Identity.GetUserId();
            var roles = await UserManager.GetRolesAsync(user.Id);
            var currentRole = roles.FirstOrDefault();

            var vm = new AdminEditUserViewModel
            {
                UserId = user.Id,
                FullName = user.FullName,
                UserName = user.UserName,
                Email = user.Email,
                SelectedRole = currentRole,
                CurrentRoleName = currentRole,
                EmailConfirmed = user.EmailConfirmed,
                IsSelf = user.Id == currentAdminId,
                AvailableRoles = await GetRoleSelectList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(AdminEditUserViewModel model)
        {
            var user = await UserManager.FindByIdAsync(model.UserId);
            if (user == null)
                return HttpNotFound();

            var currentAdminId = User.Identity.GetUserId();
            bool isSelf = user.Id == currentAdminId;

            var currentRoles = await UserManager.GetRolesAsync(user.Id);
            var currentRole = currentRoles.FirstOrDefault();

            var originalFullName = user.FullName;
            var originalUserName = user.UserName;
            var originalEmail = user.Email;
            var originalEmailConfirmed = user.EmailConfirmed;

            if (!ModelState.IsValid)
            {
                model.AvailableRoles = await GetRoleSelectList();
                model.IsSelf = isSelf;
                model.CurrentRoleName = currentRole;
                return View(model);
            }

            var userNameExists = await db.Users.AnyAsync(u => u.UserName == model.UserName && u.Id != model.UserId);
            if (userNameExists)
                ModelState.AddModelError("UserName", "Username is already taken.");

            var emailExists = await db.Users.AnyAsync(u => u.Email == model.Email && u.Id != model.UserId);
            if (emailExists)
                ModelState.AddModelError("Email", "Email is already in use.");

            if (!isSelf && string.IsNullOrWhiteSpace(model.SelectedRole))
                ModelState.AddModelError("SelectedRole", "Please select a role.");

            if (isSelf && model.SelectedRole != currentRole)
                ModelState.AddModelError("SelectedRole", "You cannot change your own role from Admin User Management.");

            bool userIsCurrentlyAdmin = currentRoles.Contains("Admin");
            bool changingAwayFromAdmin = userIsCurrentlyAdmin && model.SelectedRole != "Admin";

            if (changingAwayFromAdmin)
            {
                int adminCount = await GetAdminCountAsync();
                if (adminCount <= 1)
                {
                    ModelState.AddModelError("SelectedRole", "You cannot remove the Admin role from the last remaining admin account.");
                }
            }

            bool userIsCurrentlyEducator = currentRoles.Contains("Educator");
            bool changingAwayFromEducator = userIsCurrentlyEducator && model.SelectedRole != "Educator";

            if (changingAwayFromEducator)
            {
                int ownedCourseCount = await db.Courses.CountAsync(c => c.EducatorId == user.Id);
                int ownedLessonCount = await db.Lessons.CountAsync(l => l.EducatorId == user.Id);

                if (ownedCourseCount > 0 || ownedLessonCount > 0)
                {
                    ModelState.AddModelError("SelectedRole", "This educator still owns courses or lessons. Reassign ownership before changing the role.");
                }
            }

            if (!ModelState.IsValid)
            {
                model.AvailableRoles = await GetRoleSelectList();
                model.IsSelf = isSelf;
                model.CurrentRoleName = currentRole;
                return View(model);
            }

            user.FullName = model.FullName;
            user.UserName = model.UserName;
            user.Email = model.Email;
            user.EmailConfirmed = model.EmailConfirmed;

            var updateResult = await UserManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError("", error);
                }

                model.AvailableRoles = await GetRoleSelectList();
                model.IsSelf = isSelf;
                model.CurrentRoleName = currentRole;
                return View(model);
            }

            var finalRole = currentRole;

            if (!isSelf && !string.IsNullOrWhiteSpace(model.SelectedRole) && model.SelectedRole != currentRole)
            {
                if (!await RoleManager.RoleExistsAsync(model.SelectedRole))
                    await RoleManager.CreateAsync(new IdentityRole(model.SelectedRole));

                var addRoleResult = await UserManager.AddToRoleAsync(user.Id, model.SelectedRole);
                if (!addRoleResult.Succeeded)
                {
                    foreach (var error in addRoleResult.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }

                    model.AvailableRoles = await GetRoleSelectList();
                    model.IsSelf = isSelf;
                    model.CurrentRoleName = currentRole;
                    return View(model);
                }

                foreach (var role in currentRoles.Where(r => r != model.SelectedRole))
                {
                    var removeRoleResult = await UserManager.RemoveFromRoleAsync(user.Id, role);
                    if (!removeRoleResult.Succeeded)
                    {
                        foreach (var error in removeRoleResult.Errors)
                        {
                            ModelState.AddModelError("", error);
                        }

                        model.AvailableRoles = await GetRoleSelectList();
                        model.IsSelf = isSelf;
                        model.CurrentRoleName = model.SelectedRole;
                        return View(model);
                    }
                }

                finalRole = model.SelectedRole;
            }

            await AddAuditLogAsync(
                "Edit User",
                "User",
                user.Id,
                GetDisplayName(user),
                "Updated user profile. " +
                "FullName: '" + (originalFullName ?? "") + "' -> '" + (user.FullName ?? "") + "'. " +
                "UserName: '" + (originalUserName ?? "") + "' -> '" + (user.UserName ?? "") + "'. " +
                "Email: '" + (originalEmail ?? "") + "' -> '" + (user.Email ?? "") + "'. " +
                "EmailConfirmed: " + (originalEmailConfirmed ? "Yes" : "No") + " -> " + (user.EmailConfirmed ? "Yes" : "No") + ". " +
                "Role: '" + (currentRole ?? "No Role") + "' -> '" + (finalRole ?? "No Role") + "'.",
                saveNow: true);

            TempData["AdminUserMessage"] = "User updated successfully.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<ActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return HttpNotFound();

            var user = await UserManager.FindByIdAsync(id);
            if (user == null)
                return HttpNotFound();

            var currentAdminId = User.Identity.GetUserId();
            var roles = await UserManager.GetRolesAsync(user.Id);

            var vm = new AdminUserListItemViewModel
            {
                UserId = user.Id,
                FullName = user.FullName,
                UserName = user.UserName,
                Email = user.Email,
                RoleName = roles.FirstOrDefault() ?? "No Role",
                EmailConfirmed = user.EmailConfirmed,
                JoinedAt = user.JoinedAt,
                IsSelf = user.Id == currentAdminId,
                OwnedCourseCount = await db.Courses.CountAsync(c => c.EducatorId == user.Id),
                OwnedLessonCount = await db.Lessons.CountAsync(l => l.EducatorId == user.Id)
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<ActionResult> ReassignOwnership(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return HttpNotFound();

            var sourceEducator = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (sourceEducator == null)
                return HttpNotFound();

            var roles = await UserManager.GetRolesAsync(sourceEducator.Id);
            if (!roles.Contains("Educator"))
                return HttpNotFound();

            int ownedCourseCount = await db.Courses.CountAsync(c => c.EducatorId == id);
            int ownedLessonCount = await db.Lessons.CountAsync(l => l.EducatorId == id);

            var educatorRoleId = await db.Roles
                .Where(r => r.Name == "Educator")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            var availableEducators = await db.Users
                .Where(u => u.Id != id && u.Roles.Any(r => r.RoleId == educatorRoleId))
                .OrderBy(u => u.FullName)
                .ThenBy(u => u.Email)
                .ToListAsync();

            var vm = new AdminReassignEducatorViewModel
            {
                SourceEducatorId = sourceEducator.Id,
                SourceEducatorName = !string.IsNullOrWhiteSpace(sourceEducator.FullName) ? sourceEducator.FullName : sourceEducator.Email,
                OwnedCourseCount = ownedCourseCount,
                OwnedLessonCount = ownedLessonCount,
                AvailableEducators = availableEducators.Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName + " (" + u.Email + ")" : u.Email
                })
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ReassignOwnership(AdminReassignEducatorViewModel model)
        {
            var sourceEducator = await db.Users.FirstOrDefaultAsync(u => u.Id == model.SourceEducatorId);
            if (sourceEducator == null)
                return HttpNotFound();

            var sourceRoles = await UserManager.GetRolesAsync(sourceEducator.Id);
            if (!sourceRoles.Contains("Educator"))
                return HttpNotFound();

            if (string.IsNullOrWhiteSpace(model.TargetEducatorId))
                ModelState.AddModelError("TargetEducatorId", "Please select a target educator.");

            if (model.TargetEducatorId == model.SourceEducatorId)
                ModelState.AddModelError("TargetEducatorId", "Please select a different educator.");

            var targetEducator = await db.Users.FirstOrDefaultAsync(u => u.Id == model.TargetEducatorId);
            if (targetEducator == null)
                ModelState.AddModelError("TargetEducatorId", "Selected educator does not exist.");

            if (targetEducator != null)
            {
                var targetRoles = await UserManager.GetRolesAsync(targetEducator.Id);
                if (!targetRoles.Contains("Educator"))
                    ModelState.AddModelError("TargetEducatorId", "Selected user is not an educator.");
            }

            model.SourceEducatorName = !string.IsNullOrWhiteSpace(sourceEducator.FullName) ? sourceEducator.FullName : sourceEducator.Email;
            model.OwnedCourseCount = await db.Courses.CountAsync(c => c.EducatorId == model.SourceEducatorId);
            model.OwnedLessonCount = await db.Lessons.CountAsync(l => l.EducatorId == model.SourceEducatorId);
            model.AvailableEducators = await GetEducatorSelectList(model.SourceEducatorId);

            if (!ModelState.IsValid)
                return View(model);

            using (var transaction = db.Database.BeginTransaction())
            {
                var courses = await db.Courses
                    .Where(c => c.EducatorId == model.SourceEducatorId)
                    .ToListAsync();

                foreach (var course in courses)
                {
                    course.EducatorId = model.TargetEducatorId;
                }

                var lessons = await db.Lessons
                    .Where(l => l.EducatorId == model.SourceEducatorId)
                    .ToListAsync();

                foreach (var lesson in lessons)
                {
                    lesson.EducatorId = model.TargetEducatorId;
                }

                await db.SaveChangesAsync();

                var sourceName = !string.IsNullOrWhiteSpace(sourceEducator.FullName) ? sourceEducator.FullName : sourceEducator.Email;
                var targetName = !string.IsNullOrWhiteSpace(targetEducator.FullName) ? targetEducator.FullName : targetEducator.Email;

                db.AdminAuditLogs.Add(await BuildAuditLogAsync(
                    "Reassign Educator Ownership",
                    "EducatorOwnership",
                    sourceEducator.Id,
                    sourceName,
                    "Reassigned ownership from " + sourceName +
                    " to " + targetName +
                    ". Courses moved: " + courses.Count +
                    ". Lessons moved: " + lessons.Count + "."));

                await db.SaveChangesAsync();
                transaction.Commit();
            }

            TempData["AdminUserMessage"] = "Ownership reassigned successfully. You may now delete or update the educator account.";
            return RedirectToAction("Delete", new { id = model.SourceEducatorId });
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(string id)
        {
            var user = await UserManager.FindByIdAsync(id);
            if (user == null)
                return HttpNotFound();

            var currentAdminId = User.Identity.GetUserId();
            if (user.Id == currentAdminId)
            {
                TempData["AdminUserMessage"] = "You cannot delete your own admin account.";
                return RedirectToAction("Index");
            }

            var roles = await UserManager.GetRolesAsync(user.Id);
            bool userIsAdmin = roles.Contains("Admin");

            if (userIsAdmin)
            {
                int adminCount = await GetAdminCountAsync();
                if (adminCount <= 1)
                {
                    TempData["AdminUserMessage"] = "You cannot delete the last remaining admin account.";
                    return RedirectToAction("Index");
                }
            }

            bool ownsCourses = await db.Courses.AnyAsync(c => c.EducatorId == id);
            bool ownsLessons = await db.Lessons.AnyAsync(l => l.EducatorId == id);

            if (ownsCourses || ownsLessons)
            {
                TempData["AdminUserMessage"] = "This educator still owns courses or lessons. Reassign or delete that academic content first.";
                return RedirectToAction("Delete", new { id });
            }

            var deletedUserName = GetDisplayName(user);
            var deletedRoleSummary = roles.Any() ? string.Join(", ", roles) : "No Role";

            var submissionIds = await db.Submissions
                .Where(x => x.StudentId == id)
                .Select(x => x.SubmissionId)
                .ToListAsync();

            if (submissionIds.Any())
            {
                db.SubmissionAnswers.RemoveRange(
                    db.SubmissionAnswers.Where(x => submissionIds.Contains(x.SubmissionId)));

                db.Feedbacks.RemoveRange(
                    db.Feedbacks.Where(x => submissionIds.Contains(x.SubmissionId)));
            }

            db.Feedbacks.RemoveRange(db.Feedbacks.Where(x => x.EducatorId == id));
            db.StudentCourses.RemoveRange(db.StudentCourses.Where(x => x.StudentId == id));
            db.StudentLessonProgresses.RemoveRange(db.StudentLessonProgresses.Where(x => x.StudentId == id));
            db.Progresses.RemoveRange(db.Progresses.Where(x => x.StudentId == id));
            db.Submissions.RemoveRange(db.Submissions.Where(x => x.StudentId == id));

            await db.SaveChangesAsync();

            foreach (var role in roles)
            {
                await UserManager.RemoveFromRoleAsync(user.Id, role);
            }

            var result = await UserManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                TempData["AdminUserMessage"] = string.Join(" ", result.Errors);
                return RedirectToAction("Index");
            }

            await AddAuditLogAsync(
                "Delete User",
                "User",
                id,
                deletedUserName,
                "Deleted user. Roles: " + deletedRoleSummary + ".",
                saveNow: true);

            TempData["AdminUserMessage"] = "User deleted successfully.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<ActionResult> Create()
        {
            var vm = new AdminCreateUserViewModel
            {
                AvailableRoles = await GetRoleSelectList(),
                EmailConfirmed = true
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(AdminCreateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableRoles = await GetRoleSelectList();
                return View(model);
            }

            var userNameExists = await db.Users.AnyAsync(u => u.UserName == model.UserName);
            if (userNameExists)
                ModelState.AddModelError("UserName", "Username is already taken.");

            var emailExists = await db.Users.AnyAsync(u => u.Email == model.Email);
            if (emailExists)
                ModelState.AddModelError("Email", "Email is already in use.");

            if (string.IsNullOrWhiteSpace(model.SelectedRole))
                ModelState.AddModelError("SelectedRole", "Please select a role.");

            if (!ModelState.IsValid)
            {
                model.AvailableRoles = await GetRoleSelectList();
                return View(model);
            }

            if (!await RoleManager.RoleExistsAsync(model.SelectedRole))
            {
                await RoleManager.CreateAsync(new IdentityRole(model.SelectedRole));
            }

            var user = new ApplicationUser
            {
                FullName = model.FullName,
                UserName = model.UserName,
                Email = model.Email,
                EmailConfirmed = model.EmailConfirmed,
                JoinedAt = DateTime.UtcNow
            };

            var createResult = await UserManager.CreateAsync(user, model.Password);
            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError("", error);
                }

                model.AvailableRoles = await GetRoleSelectList();
                return View(model);
            }

            var addRoleResult = await UserManager.AddToRoleAsync(user.Id, model.SelectedRole);
            if (!addRoleResult.Succeeded)
            {
                foreach (var error in addRoleResult.Errors)
                {
                    ModelState.AddModelError("", error);
                }

                await UserManager.DeleteAsync(user);
                model.AvailableRoles = await GetRoleSelectList();
                return View(model);
            }

            await AddAuditLogAsync(
                "Create User",
                "User",
                user.Id,
                GetDisplayName(user),
                "Created user with role '" + model.SelectedRole + "'.",
                saveNow: true);

            TempData["AdminUserMessage"] = "User created successfully.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<ActionResult> ResetPassword(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return HttpNotFound();

            var user = await UserManager.FindByIdAsync(id);
            if (user == null)
                return HttpNotFound();

            var currentAdminId = User.Identity.GetUserId();

            var vm = new AdminResetPasswordViewModel
            {
                UserId = user.Id,
                FullName = user.FullName,
                UserName = user.UserName,
                Email = user.Email,
                IsSelf = user.Id == currentAdminId
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(AdminResetPasswordViewModel model)
        {
            var user = await UserManager.FindByIdAsync(model.UserId);
            if (user == null)
                return HttpNotFound();

            var currentAdminId = User.Identity.GetUserId();
            model.IsSelf = user.Id == currentAdminId;
            model.FullName = user.FullName;
            model.UserName = user.UserName;
            model.Email = user.Email;

            if (!ModelState.IsValid)
                return View(model);

            if (model.IsSelf)
            {
                ModelState.AddModelError("", "Please use Change Password for your own account.");
                return View(model);
            }

            var resetToken = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
            var result = await UserManager.ResetPasswordAsync(user.Id, resetToken, model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error);
                }

                return View(model);
            }

            await AddAuditLogAsync(
                "Reset Password",
                "User",
                user.Id,
                GetDisplayName(user),
                "Administrator reset this user's password.",
                saveNow: true);

            TempData["AdminUserMessage"] = "Password reset successfully.";
            return RedirectToAction("Index");
        }

        public async Task<FileResult> ExportCsv(
            string searchTerm,
            string selectedRole,
            bool? emailConfirmed,
            string sortBy = "email",
            string sortDirection = "asc")
        {
            var items = await BuildAdminUserListAsync();
            items = ApplyAdminUserFiltersAndSort(items, searchTerm, selectedRole, emailConfirmed, sortBy, sortDirection);

            var sb = new StringBuilder();
            sb.AppendLine("FullName,UserName,Email,Role,EmailConfirmed,JoinedAt,OwnedCourses,OwnedLessons,IsSelf");

            foreach (var item in items)
            {
                sb.AppendLine(
                    Csv(item.FullName) + "," +
                    Csv(item.UserName) + "," +
                    Csv(item.Email) + "," +
                    Csv(item.RoleName) + "," +
                    (item.EmailConfirmed ? "Yes" : "No") + "," +
                    Csv(item.JoinedAt.HasValue
                        ? item.JoinedAt.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                        : "") + "," +
                    item.OwnedCourseCount + "," +
                    item.OwnedLessonCount + "," +
                    (item.IsSelf ? "Yes" : "No"));
            }

            return File(
                new UTF8Encoding(true).GetBytes(sb.ToString()),
                "text/csv",
                "admin-users.csv");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> BulkAction(AdminBulkActionViewModel model)
        {
            if (model.SelectedUserIds == null || !model.SelectedUserIds.Any())
            {
                TempData["AdminUserMessage"] = "Please select at least one user.";
                return RedirectToAction("Index", new
                {
                    searchTerm = model.SearchTerm,
                    selectedRole = model.SelectedRole,
                    emailConfirmed = model.EmailConfirmed,
                    sortBy = model.SortBy,
                    sortDirection = model.SortDirection,
                    page = model.Page,
                    pageSize = model.PageSize
                });
            }

            var currentAdminId = User.Identity.GetUserId();
            int affectedCount = 0;
            var messages = new List<string>();

            if (model.ActionName == "confirm-email")
            {
                var confirmedUsers = new List<string>();

                var users = await db.Users
                    .Where(u => model.SelectedUserIds.Contains(u.Id))
                    .ToListAsync();

                foreach (var user in users)
                {
                    if (!user.EmailConfirmed)
                    {
                        user.EmailConfirmed = true;
                        affectedCount++;
                        confirmedUsers.Add(user.Email);
                    }
                }

                await db.SaveChangesAsync();

                if (affectedCount > 0)
                {
                    await AddAuditLogAsync(
                        "Bulk Confirm Email",
                        "User",
                        null,
                        "Bulk User Action",
                        "Confirmed email for " + affectedCount + " user(s): " + string.Join(", ", confirmedUsers),
                        saveNow: true);
                }

                TempData["AdminUserMessage"] = affectedCount + " user(s) email confirmed successfully.";
            }
            else if (model.ActionName == "delete")
            {
                var deletedUsers = new List<string>();

                foreach (var userId in model.SelectedUserIds.Distinct())
                {
                    var user = await UserManager.FindByIdAsync(userId);
                    if (user == null)
                        continue;

                    if (user.Id == currentAdminId)
                    {
                        messages.Add("Skipped your own admin account.");
                        continue;
                    }

                    var roles = await UserManager.GetRolesAsync(user.Id);

                    if (roles.Contains("Admin"))
                    {
                        int adminCount = await GetAdminCountAsync();
                        if (adminCount <= 1)
                        {
                            messages.Add("Skipped " + user.Email + " because it is the last remaining admin account.");
                            continue;
                        }
                    }

                    bool ownsCourses = await db.Courses.AnyAsync(c => c.EducatorId == user.Id);
                    bool ownsLessons = await db.Lessons.AnyAsync(l => l.EducatorId == user.Id);

                    if (ownsCourses || ownsLessons)
                    {
                        messages.Add("Skipped " + user.Email + " because the educator still owns courses or lessons.");
                        continue;
                    }

                    var submissionIds = await db.Submissions
                        .Where(x => x.StudentId == user.Id)
                        .Select(x => x.SubmissionId)
                        .ToListAsync();

                    if (submissionIds.Any())
                    {
                        db.SubmissionAnswers.RemoveRange(
                            db.SubmissionAnswers.Where(x => submissionIds.Contains(x.SubmissionId)));

                        db.Feedbacks.RemoveRange(
                            db.Feedbacks.Where(x => submissionIds.Contains(x.SubmissionId)));
                    }

                    db.Feedbacks.RemoveRange(db.Feedbacks.Where(x => x.EducatorId == user.Id));
                    db.StudentCourses.RemoveRange(db.StudentCourses.Where(x => x.StudentId == user.Id));
                    db.StudentLessonProgresses.RemoveRange(db.StudentLessonProgresses.Where(x => x.StudentId == user.Id));
                    db.Progresses.RemoveRange(db.Progresses.Where(x => x.StudentId == user.Id));
                    db.Submissions.RemoveRange(db.Submissions.Where(x => x.StudentId == user.Id));

                    await db.SaveChangesAsync();

                    foreach (var role in roles)
                    {
                        await UserManager.RemoveFromRoleAsync(user.Id, role);
                    }

                    var result = await UserManager.DeleteAsync(user);
                    if (result.Succeeded)
                    {
                        affectedCount++;
                        deletedUsers.Add(user.Email);
                    }
                    else
                    {
                        messages.Add("Failed to delete " + user.Email + ".");
                    }
                }

                await AddAuditLogAsync(
                    "Bulk Delete Users",
                    "User",
                    null,
                    "Bulk User Action",
                    "Deleted " + affectedCount + " user(s)." +
                    (deletedUsers.Any() ? " Deleted users: " + string.Join(", ", deletedUsers) + "." : "") +
                    (messages.Any() ? " Notes: " + string.Join(" ", messages) : ""),
                    saveNow: true);

                var finalMessage = affectedCount + " user(s) deleted successfully.";
                if (messages.Any())
                {
                    finalMessage += " " + string.Join(" ", messages);
                }

                TempData["AdminUserMessage"] = finalMessage;
            }
            else if (model.ActionName == "reassign-ownership")
            {
                var sourceItems = await BuildBulkReassignSourceItemsAsync(model.SelectedUserIds);

                if (!sourceItems.Any())
                {
                    TempData["AdminUserMessage"] = "No selected educator with owned courses or lessons was found.";
                    return RedirectToAction("Index", new
                    {
                        searchTerm = model.SearchTerm,
                        selectedRole = model.SelectedRole,
                        emailConfirmed = model.EmailConfirmed,
                        sortBy = model.SortBy,
                        sortDirection = model.SortDirection,
                        page = model.Page,
                        pageSize = model.PageSize
                    });
                }

                var reassignVm = new AdminBulkReassignOwnershipViewModel
                {
                    SourceEducatorIds = sourceItems.Select(x => x.EducatorId).ToList(),
                    SourceEducators = sourceItems,
                    AvailableEducators = await GetEducatorSelectListExceptAsync(sourceItems.Select(x => x.EducatorId)),
                    SearchTerm = model.SearchTerm,
                    SelectedRole = model.SelectedRole,
                    EmailConfirmed = model.EmailConfirmed,
                    SortBy = model.SortBy,
                    SortDirection = model.SortDirection,
                    Page = model.Page,
                    PageSize = model.PageSize
                };

                return View("BulkReassignOwnership", reassignVm);
            }
            else
            {
                TempData["AdminUserMessage"] = "Invalid bulk action.";
            }

            return RedirectToAction("Index", new
            {
                searchTerm = model.SearchTerm,
                selectedRole = model.SelectedRole,
                emailConfirmed = model.EmailConfirmed,
                sortBy = model.SortBy,
                sortDirection = model.SortDirection,
                page = model.Page,
                pageSize = model.PageSize
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> BulkReassignOwnership(AdminBulkReassignOwnershipViewModel model)
        {
            model.SourceEducatorIds = model.SourceEducatorIds ?? new List<string>();
            model.SourceEducators = await BuildBulkReassignSourceItemsAsync(model.SourceEducatorIds);
            model.AvailableEducators = await GetEducatorSelectListExceptAsync(model.SourceEducatorIds);

            if (!model.SourceEducatorIds.Any() || !model.SourceEducators.Any())
            {
                TempData["AdminUserMessage"] = "No valid educator ownership records were found for bulk reassignment.";
                return RedirectToAction("Index", new
                {
                    searchTerm = model.SearchTerm,
                    selectedRole = model.SelectedRole,
                    emailConfirmed = model.EmailConfirmed,
                    sortBy = model.SortBy,
                    sortDirection = model.SortDirection,
                    page = model.Page,
                    pageSize = model.PageSize
                });
            }

            if (string.IsNullOrWhiteSpace(model.TargetEducatorId))
                ModelState.AddModelError("TargetEducatorId", "Please select a target educator.");

            if (model.SourceEducatorIds.Contains(model.TargetEducatorId))
                ModelState.AddModelError("TargetEducatorId", "Target educator cannot also be one of the selected source educators.");

            var targetEducator = await db.Users.FirstOrDefaultAsync(u => u.Id == model.TargetEducatorId);
            if (targetEducator == null)
            {
                ModelState.AddModelError("TargetEducatorId", "Selected educator does not exist.");
            }
            else
            {
                var targetRoles = await UserManager.GetRolesAsync(targetEducator.Id);
                if (!targetRoles.Contains("Educator"))
                    ModelState.AddModelError("TargetEducatorId", "Selected user is not an educator.");
            }

            if (!ModelState.IsValid)
                return View(model);

            var sourceIds = model.SourceEducators
                .Select(x => x.EducatorId)
                .Distinct()
                .ToList();

            using (var transaction = db.Database.BeginTransaction())
            {
                var courses = await db.Courses
                    .Where(c => c.EducatorId != null && sourceIds.Contains(c.EducatorId))
                    .ToListAsync();

                var lessons = await db.Lessons
                    .Where(l => l.EducatorId != null && sourceIds.Contains(l.EducatorId))
                    .ToListAsync();

                foreach (var course in courses)
                {
                    course.EducatorId = model.TargetEducatorId;
                }

                foreach (var lesson in lessons)
                {
                    lesson.EducatorId = model.TargetEducatorId;
                }

                await db.SaveChangesAsync();

                var targetName = !string.IsNullOrWhiteSpace(targetEducator.FullName) ? targetEducator.FullName : targetEducator.Email;
                var sourceSummary = string.Join("; ", model.SourceEducators.Select(x =>
                    x.EducatorName + " (" + x.OwnedCourseCount + " courses, " + x.OwnedLessonCount + " lessons)"));

                db.AdminAuditLogs.Add(await BuildAuditLogAsync(
                    "Bulk Reassign Educator Ownership",
                    "EducatorOwnership",
                    model.TargetEducatorId,
                    targetName,
                    "Bulk reassignment to " + targetName +
                    ". Sources: " + sourceSummary +
                    ". Total courses moved: " + courses.Count +
                    ". Total lessons moved: " + lessons.Count + "."));

                await db.SaveChangesAsync();
                transaction.Commit();

                TempData["AdminUserMessage"] =
                    "Bulk ownership reassignment completed successfully. " +
                    courses.Count + " course(s) and " +
                    lessons.Count + " lesson(s) were reassigned.";
            }

            return RedirectToAction("Index", new
            {
                searchTerm = model.SearchTerm,
                selectedRole = model.SelectedRole,
                emailConfirmed = model.EmailConfirmed,
                sortBy = model.SortBy,
                sortDirection = model.SortDirection,
                page = model.Page,
                pageSize = model.PageSize
            });
        }

        private async Task<int> GetAdminCountAsync()
        {
            var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
            if (adminRole == null)
                return 0;

            return await db.Users.CountAsync(u => u.Roles.Any(r => r.RoleId == adminRole.Id));
        }

        private async Task<IEnumerable<SelectListItem>> GetRoleSelectList()
        {
            var roles = await db.Roles
                .OrderBy(r => r.Name)
                .ToListAsync();

            return roles.Select(r => new SelectListItem
            {
                Value = r.Name,
                Text = r.Name
            });
        }

        private async Task<IEnumerable<SelectListItem>> GetEducatorSelectList(string excludeUserId)
        {
            var educatorRoleId = await db.Roles
                .Where(r => r.Name == "Educator")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            var educators = await db.Users
                .Where(u => u.Id != excludeUserId && u.Roles.Any(r => r.RoleId == educatorRoleId))
                .OrderBy(u => u.FullName)
                .ThenBy(u => u.Email)
                .ToListAsync();

            return educators.Select(u => new SelectListItem
            {
                Value = u.Id,
                Text = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName + " (" + u.Email + ")" : u.Email
            });
        }

        private async Task<List<AdminUserListItemViewModel>> BuildAdminUserListAsync()
        {
            var currentAdminId = User.Identity.GetUserId();

            var users = await db.Users
                .OrderBy(u => u.Email)
                .ToListAsync();

            var courseCountMap = await db.Courses
                .Where(c => c.EducatorId != null)
                .GroupBy(c => c.EducatorId)
                .Select(g => new { EducatorId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.EducatorId, x => x.Count);

            var lessonCountMap = await db.Lessons
                .Where(l => l.EducatorId != null)
                .GroupBy(l => l.EducatorId)
                .Select(g => new { EducatorId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.EducatorId, x => x.Count);

            var items = new List<AdminUserListItemViewModel>();

            foreach (var user in users)
            {
                var roles = await UserManager.GetRolesAsync(user.Id);

                items.Add(new AdminUserListItemViewModel
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    UserName = user.UserName,
                    Email = user.Email,
                    RoleName = roles.FirstOrDefault() ?? "No Role",
                    EmailConfirmed = user.EmailConfirmed,
                    JoinedAt = user.JoinedAt,
                    IsSelf = user.Id == currentAdminId,
                    OwnedCourseCount = courseCountMap.ContainsKey(user.Id) ? courseCountMap[user.Id] : 0,
                    OwnedLessonCount = lessonCountMap.ContainsKey(user.Id) ? lessonCountMap[user.Id] : 0
                });
            }

            return items;
        }

        private List<AdminUserListItemViewModel> ApplyAdminUserFiltersAndSort(
            List<AdminUserListItemViewModel> items,
            string searchTerm,
            string selectedRole,
            bool? emailConfirmed,
            string sortBy,
            string sortDirection)
        {
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var keyword = searchTerm.Trim().ToLower();

                items = items.Where(x =>
                    (!string.IsNullOrWhiteSpace(x.FullName) && x.FullName.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrWhiteSpace(x.UserName) && x.UserName.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrWhiteSpace(x.Email) && x.Email.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrWhiteSpace(x.RoleName) && x.RoleName.ToLower().Contains(keyword))
                ).ToList();
            }

            if (!string.IsNullOrWhiteSpace(selectedRole))
            {
                items = items.Where(x => x.RoleName == selectedRole).ToList();
            }

            if (emailConfirmed.HasValue)
            {
                items = items.Where(x => x.EmailConfirmed == emailConfirmed.Value).ToList();
            }

            bool asc = (sortDirection ?? "asc").ToLower() != "desc";

            switch ((sortBy ?? "email").ToLower())
            {
                case "fullname":
                    items = asc
                        ? items.OrderBy(x => x.FullName).ThenBy(x => x.Email).ToList()
                        : items.OrderByDescending(x => x.FullName).ThenBy(x => x.Email).ToList();
                    break;

                case "username":
                    items = asc
                        ? items.OrderBy(x => x.UserName).ThenBy(x => x.Email).ToList()
                        : items.OrderByDescending(x => x.UserName).ThenBy(x => x.Email).ToList();
                    break;

                case "role":
                    items = asc
                        ? items.OrderBy(x => x.RoleName).ThenBy(x => x.Email).ToList()
                        : items.OrderByDescending(x => x.RoleName).ThenBy(x => x.Email).ToList();
                    break;

                case "joined":
                    items = asc
                        ? items.OrderBy(x => x.JoinedAt).ThenBy(x => x.Email).ToList()
                        : items.OrderByDescending(x => x.JoinedAt).ThenBy(x => x.Email).ToList();
                    break;

                case "emailconfirmed":
                    items = asc
                        ? items.OrderBy(x => x.EmailConfirmed).ThenBy(x => x.Email).ToList()
                        : items.OrderByDescending(x => x.EmailConfirmed).ThenBy(x => x.Email).ToList();
                    break;

                default:
                    items = asc
                        ? items.OrderBy(x => x.Email).ToList()
                        : items.OrderByDescending(x => x.Email).ToList();
                    break;
            }

            return items;
        }

        private static string Csv(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "\"\"";

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string GetDisplayName(ApplicationUser user)
        {
            if (user == null) return "Unknown User";

            if (!string.IsNullOrWhiteSpace(user.FullName))
                return user.FullName;

            if (!string.IsNullOrWhiteSpace(user.Email))
                return user.Email;

            return user.UserName;
        }

        private async Task<AdminAuditLog> BuildAuditLogAsync(
            string actionType,
            string entityType,
            string entityId,
            string entityDisplayName,
            string details)
        {
            var adminUserId = User.Identity.GetUserId();

            var adminInfo = await db.Users
                .AsNoTracking()
                .Where(u => u.Id == adminUserId)
                .Select(u => new
                {
                    u.FullName,
                    u.Email
                })
                .FirstOrDefaultAsync();

            var adminUserName = adminInfo == null
                ? "Unknown Admin"
                : (!string.IsNullOrWhiteSpace(adminInfo.FullName) ? adminInfo.FullName : adminInfo.Email);

            return new AdminAuditLog
            {
                AdminUserId = adminUserId,
                AdminUserName = adminUserName,
                ActionType = actionType,
                EntityType = entityType,
                EntityId = entityId,
                EntityDisplayName = entityDisplayName,
                Details = details,
                CreatedAtUtc = DateTime.UtcNow
            };
        }

        private async Task AddAuditLogAsync(
            string actionType,
            string entityType,
            string entityId,
            string entityDisplayName,
            string details,
            bool saveNow = false)
        {
            db.AdminAuditLogs.Add(await BuildAuditLogAsync(
                actionType,
                entityType,
                entityId,
                entityDisplayName,
                details));

            if (saveNow)
            {
                await db.SaveChangesAsync();
            }
        }

        private async Task<List<AdminBulkReassignOwnershipItemViewModel>> BuildBulkReassignSourceItemsAsync(IEnumerable<string> selectedUserIds)
        {
            var sourceIds = (selectedUserIds ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            if (!sourceIds.Any())
                return new List<AdminBulkReassignOwnershipItemViewModel>();

            var educatorRoleId = await db.Roles
                .Where(r => r.Name == "Educator")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(educatorRoleId))
                return new List<AdminBulkReassignOwnershipItemViewModel>();

            var educators = await db.Users
                .Where(u => sourceIds.Contains(u.Id) && u.Roles.Any(r => r.RoleId == educatorRoleId))
                .OrderBy(u => u.FullName)
                .ThenBy(u => u.Email)
                .ToListAsync();

            var courseCountMap = await db.Courses
                .Where(c => c.EducatorId != null && sourceIds.Contains(c.EducatorId))
                .GroupBy(c => c.EducatorId)
                .Select(g => new { EducatorId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.EducatorId, x => x.Count);

            var lessonCountMap = await db.Lessons
                .Where(l => l.EducatorId != null && sourceIds.Contains(l.EducatorId))
                .GroupBy(l => l.EducatorId)
                .Select(g => new { EducatorId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.EducatorId, x => x.Count);

            return educators
                .Select(u => new AdminBulkReassignOwnershipItemViewModel
                {
                    EducatorId = u.Id,
                    EducatorName = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.Email,
                    Email = u.Email,
                    OwnedCourseCount = courseCountMap.ContainsKey(u.Id) ? courseCountMap[u.Id] : 0,
                    OwnedLessonCount = lessonCountMap.ContainsKey(u.Id) ? lessonCountMap[u.Id] : 0
                })
                .Where(x => x.OwnedCourseCount > 0 || x.OwnedLessonCount > 0)
                .ToList();
        }

        private async Task<IEnumerable<SelectListItem>> GetEducatorSelectListExceptAsync(IEnumerable<string> excludeUserIds)
        {
            var excludedIds = (excludeUserIds ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            var educatorRoleId = await db.Roles
                .Where(r => r.Name == "Educator")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            var educators = await db.Users
                .Where(u => !excludedIds.Contains(u.Id) && u.Roles.Any(r => r.RoleId == educatorRoleId))
                .OrderBy(u => u.FullName)
                .ThenBy(u => u.Email)
                .ToListAsync();

            return educators.Select(u => new SelectListItem
            {
                Value = u.Id,
                Text = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName + " (" + u.Email + ")" : u.Email
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();

                if (_roleManager != null)
                    _roleManager.Dispose();

                if (_userManager != null)
                    _userManager.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}