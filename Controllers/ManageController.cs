using FluentMY.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace FluentMY.Controllers
{
    [Authorize]
    public class ManageController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        public ManageController()
        {

        }

        public ManageController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set 
            { 
                _signInManager = value; 
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        //
        // GET: /Manage/Index
        public async Task<ActionResult> Index(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
                : message == ManageMessageId.SetTwoFactorSuccess ? "Your two-factor authentication provider has been set."
                : message == ManageMessageId.Error ? "An error has occurred."
                : message == ManageMessageId.AddPhoneSuccess ? "Your phone number was added."
                : message == ManageMessageId.RemovePhoneSuccess ? "Your phone number was removed."
                : "";

            var userId = User.Identity.GetUserId();
            var model = new IndexViewModel
            {
                HasPassword = HasPassword(),
                PhoneNumber = await UserManager.GetPhoneNumberAsync(userId),
                TwoFactor = await UserManager.GetTwoFactorEnabledAsync(userId),
                Logins = await UserManager.GetLoginsAsync(userId),
                BrowserRemembered = await AuthenticationManager.TwoFactorBrowserRememberedAsync(userId)
            };
            return View(model);
        }

        //
        // POST: /Manage/RemoveLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemoveLogin(string loginProvider, string providerKey)
        {
            ManageMessageId? message;
            var result = await UserManager.RemoveLoginAsync(User.Identity.GetUserId(), new UserLoginInfo(loginProvider, providerKey));
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                message = ManageMessageId.RemoveLoginSuccess;
            }
            else
            {
                message = ManageMessageId.Error;
            }
            return RedirectToAction("ManageLogins", new { Message = message });
        }

        //
        // GET: /Manage/AddPhoneNumber
        public ActionResult AddPhoneNumber()
        {
            return View();
        }

        //
        // POST: /Manage/AddPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddPhoneNumber(AddPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            // Generate the token and send it
            var code = await UserManager.GenerateChangePhoneNumberTokenAsync(User.Identity.GetUserId(), model.Number);
            if (UserManager.SmsService != null)
            {
                var message = new IdentityMessage
                {
                    Destination = model.Number,
                    Body = "Your security code is: " + code
                };
                await UserManager.SmsService.SendAsync(message);
            }
            return RedirectToAction("VerifyPhoneNumber", new { PhoneNumber = model.Number });
        }

        //
        // POST: /Manage/EnableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EnableTwoFactorAuthentication()
        {
            await UserManager.SetTwoFactorEnabledAsync(User.Identity.GetUserId(), true);
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", "Manage");
        }

        //
        // POST: /Manage/DisableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DisableTwoFactorAuthentication()
        {
            await UserManager.SetTwoFactorEnabledAsync(User.Identity.GetUserId(), false);
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", "Manage");
        }

        //
        // GET: /Manage/VerifyPhoneNumber
        public async Task<ActionResult> VerifyPhoneNumber(string phoneNumber)
        {
            var code = await UserManager.GenerateChangePhoneNumberTokenAsync(User.Identity.GetUserId(), phoneNumber);
            // Send an SMS through the SMS provider to verify the phone number
            return phoneNumber == null ? View("Error") : View(new VerifyPhoneNumberViewModel { PhoneNumber = phoneNumber });
        }

        //
        // POST: /Manage/VerifyPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyPhoneNumber(VerifyPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await UserManager.ChangePhoneNumberAsync(User.Identity.GetUserId(), model.PhoneNumber, model.Code);
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                return RedirectToAction("Index", new { Message = ManageMessageId.AddPhoneSuccess });
            }
            // If we got this far, something failed, redisplay form
            ModelState.AddModelError("", "Failed to verify phone");
            return View(model);
        }

        //
        // POST: /Manage/RemovePhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemovePhoneNumber()
        {
            var result = await UserManager.SetPhoneNumberAsync(User.Identity.GetUserId(), null);
            if (!result.Succeeded)
            {
                return RedirectToAction("Index", new { Message = ManageMessageId.Error });
            }
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", new { Message = ManageMessageId.RemovePhoneSuccess });
        }

        //
        // GET: /Manage/ChangePassword
        public ActionResult ChangePassword()
        {
            return View();
        }

        //
        // POST: /Manage/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }

                TempData["ProfileMessage"] = "Your password has been changed successfully.";
                return RedirectToAction("EditProfile");
            }
            AddErrors(result);
            return View(model);
        }

        //
        // GET: /Manage/SetPassword
        public ActionResult SetPassword()
        {
            return View();
        }

        //
        // POST: /Manage/SetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SetPassword(SetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await UserManager.AddPasswordAsync(User.Identity.GetUserId(), model.NewPassword);
                if (result.Succeeded)
                {
                    var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                    if (user != null)
                    {
                        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                    }
                    return RedirectToAction("Index", new { Message = ManageMessageId.SetPasswordSuccess });
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Manage/ManageLogins
        public async Task<ActionResult> ManageLogins(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
                : message == ManageMessageId.Error ? "An error has occurred."
                : "";
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user == null)
            {
                return View("Error");
            }
            var userLogins = await UserManager.GetLoginsAsync(User.Identity.GetUserId());
            var otherLogins = AuthenticationManager.GetExternalAuthenticationTypes().Where(auth => userLogins.All(ul => auth.AuthenticationType != ul.LoginProvider)).ToList();
            ViewBag.ShowRemoveButton = user.PasswordHash != null || userLogins.Count > 1;
            return View(new ManageLoginsViewModel
            {
                CurrentLogins = userLogins,
                OtherLogins = otherLogins
            });
        }

        //
        // POST: /Manage/LinkLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LinkLogin(string provider)
        {
            // Request a redirect to the external login provider to link a login for the current user
            return new AccountController.ChallengeResult(provider, Url.Action("LinkLoginCallback", "Manage"), User.Identity.GetUserId());
        }

        //
        // GET: /Manage/LinkLoginCallback
        public async Task<ActionResult> LinkLoginCallback()
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync(XsrfKey, User.Identity.GetUserId());
            if (loginInfo == null)
            {
                return RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
            }
            var result = await UserManager.AddLoginAsync(User.Identity.GetUserId(), loginInfo.Login);
            return result.Succeeded ? RedirectToAction("ManageLogins") : RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _userManager != null)
            {
                _userManager.Dispose();
                _userManager = null;
            }

            base.Dispose(disposing);
        }

        [HttpGet]
        public async Task<ActionResult> EditProfile()
        {
            var userId = User.Identity.GetUserId();
            var user = await UserManager.FindByIdAsync(userId);

            if (user == null)
                return RedirectToAction("Index");

            var roles = await UserManager.GetRolesAsync(userId);
            var roleName = roles.FirstOrDefault() ?? "Member";

            int enrolledCourses = 0;
            int completedCourses = 0;
            int overallProgress = 0;

            if (roleName == "Student")
            {
                enrolledCourses = await db.StudentCourses.CountAsync(x => x.StudentId == userId);
                completedCourses = await db.StudentCourses.CountAsync(x => x.StudentId == userId && x.IsCompleted);

                var enrolledCourseIds = await db.StudentCourses
                    .Where(x => x.StudentId == userId)
                    .Select(x => x.CourseId)
                    .ToListAsync();

                var lessonIds = await db.Lessons
                    .Where(l => enrolledCourseIds.Contains(l.CourseId))
                    .Select(l => l.LessonId)
                    .ToListAsync();

                var totalLessons = lessonIds.Count;

                var completedLessons = totalLessons == 0
                    ? 0
                    : await db.StudentLessonProgresses.CountAsync(p =>
                        p.StudentId == userId &&
                        p.IsCompleted &&
                        lessonIds.Contains(p.LessonId));

                overallProgress = totalLessons == 0
                    ? 0
                    : (int)Math.Floor(completedLessons * 100.0 / totalLessons);
            }

            var vm = new EditProfileViewModel
            {
                UserName = user.UserName,
                Email = user.Email,
                FullName = user.FullName,
                Bio = user.Bio,
                LanguageLevel = user.LanguageLevel,
                ExistingProfileImagePath = user.ProfileImagePath,
                RoleName = roleName,
                JoinedAt = user.JoinedAt,
                IsEmailConfirmed = user.EmailConfirmed,
                EnrolledCoursesCount = enrolledCourses,
                CompletedCoursesCount = completedCourses,
                OverallProgressPercent = overallProgress
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditProfile(EditProfileViewModel model)
        {
            var userId = User.Identity.GetUserId();
            var user = await UserManager.FindByIdAsync(userId);

            if (user == null)
                return RedirectToAction("Index");

            if (!ModelState.IsValid)
            {
                await PopulateEditProfileViewModelAsync(model, user, userId);
                return View(model);
            }

            var usernameExists = await db.Users.AnyAsync(u => u.UserName == model.UserName && u.Id != userId);
            if (usernameExists)
                ModelState.AddModelError("UserName", "This username is already taken.");

            var emailExists = await db.Users.AnyAsync(u => u.Email == model.Email && u.Id != userId);
            if (emailExists)
                ModelState.AddModelError("Email", "This email is already in use.");

            if (!ModelState.IsValid)
            {
                await PopulateEditProfileViewModelAsync(model, user, userId);
                return View(model);
            }

            bool emailChanged = !string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase);

            if (model.ProfileImageFile != null && model.ProfileImageFile.ContentLength > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(model.ProfileImageFile.FileName)?.ToLower();

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("ProfileImageFile", "Only JPG, JPEG, PNG, and GIF files are allowed.");
                    await PopulateEditProfileViewModelAsync(model, user, userId);
                    return View(model);
                }

                if (model.ProfileImageFile.ContentLength > 2 * 1024 * 1024)
                {
                    ModelState.AddModelError("ProfileImageFile", "Image size must be less than 2MB.");
                    await PopulateEditProfileViewModelAsync(model, user, userId);
                    return View(model);
                }

                var fileName = Guid.NewGuid() + extension;
                var folderPath = Server.MapPath("~/Content/profile-images/");
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                var fullPath = Path.Combine(folderPath, fileName);
                model.ProfileImageFile.SaveAs(fullPath);

                if (!string.IsNullOrEmpty(user.ProfileImagePath))
                {
                    var oldPath = Server.MapPath(user.ProfileImagePath);
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                user.ProfileImagePath = "~/Content/profile-images/" + fileName;
            }

            user.UserName = model.UserName;
            user.Email = model.Email;
            user.FullName = model.FullName;
            user.Bio = model.Bio;
            user.LanguageLevel = model.LanguageLevel;

            if (emailChanged)
                user.EmailConfirmed = false;

            var result = await UserManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error);

                await PopulateEditProfileViewModelAsync(model, user, userId);
                return View(model);
            }

            await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);

            if (emailChanged)
            {
                try
                {
                    string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                    var callbackUrl = Url.Action(
                        "ConfirmEmail",
                        "Account",
                        new { userId = user.Id, code = code },
                        protocol: Request.Url.Scheme);

                    await UserManager.SendEmailAsync(
                        user.Id,
                        "Confirm your new FluentMY email",
                        "Please confirm your new email by clicking <a href=\"" + callbackUrl + "\">here</a>.");

                    TempData["ProfileMessage"] = "Profile updated successfully. Please check your new email address to confirm it.";
                }
                catch
                {
                    TempData["ProfileMessage"] = "Profile updated successfully, but the confirmation email could not be sent.";
                }
            }
            else
            {
                TempData["ProfileMessage"] = "Profile updated successfully.";
            }

            return RedirectToAction("EditProfile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemoveProfileImage()
        {
            var userId = User.Identity.GetUserId();
            var user = await UserManager.FindByIdAsync(userId);

            if (user == null)
                return RedirectToAction("EditProfile");

            if (!string.IsNullOrEmpty(user.ProfileImagePath))
            {
                var fullPath = Server.MapPath(user.ProfileImagePath);
                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);

                user.ProfileImagePath = null;
                await UserManager.UpdateAsync(user);
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }

            TempData["ProfileMessage"] = "Profile picture removed successfully.";
            return RedirectToAction("EditProfile");
        }

        private async Task PopulateEditProfileViewModelAsync(EditProfileViewModel model, ApplicationUser user, string userId)
        {
            var roles = await UserManager.GetRolesAsync(userId);
            var roleName = roles.FirstOrDefault() ?? "Member";

            int enrolledCourses = 0;
            int completedCourses = 0;
            int overallProgress = 0;

            if (roleName == "Student")
            {
                enrolledCourses = await db.StudentCourses.CountAsync(x => x.StudentId == userId);
                completedCourses = await db.StudentCourses.CountAsync(x => x.StudentId == userId && x.IsCompleted);

                var enrolledCourseIds = await db.StudentCourses
                    .Where(x => x.StudentId == userId)
                    .Select(x => x.CourseId)
                    .ToListAsync();

                var lessonIds = await db.Lessons
                    .Where(l => enrolledCourseIds.Contains(l.CourseId))
                    .Select(l => l.LessonId)
                    .ToListAsync();

                var totalLessons = lessonIds.Count;

                var completedLessons = totalLessons == 0
                    ? 0
                    : await db.StudentLessonProgresses.CountAsync(p =>
                        p.StudentId == userId &&
                        p.IsCompleted &&
                        lessonIds.Contains(p.LessonId));

                overallProgress = totalLessons == 0
                    ? 0
                    : (int)Math.Floor(completedLessons * 100.0 / totalLessons);
            }

            model.ExistingProfileImagePath = user.ProfileImagePath;
            model.RoleName = roleName;
            model.JoinedAt = user.JoinedAt;
            model.IsEmailConfirmed = user.EmailConfirmed;
            model.EnrolledCoursesCount = enrolledCourses;
            model.CompletedCoursesCount = completedCourses;
            model.OverallProgressPercent = overallProgress;
        }

        #region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private bool HasPassword()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PasswordHash != null;
            }
            return false;
        }

        private bool HasPhoneNumber()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PhoneNumber != null;
            }
            return false;
        }

        public enum ManageMessageId
        {
            AddPhoneSuccess,
            ChangePasswordSuccess,
            SetTwoFactorSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            RemovePhoneSuccess,
            Error
        }

#endregion
    }
}