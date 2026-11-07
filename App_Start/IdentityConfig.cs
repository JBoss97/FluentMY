using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using FluentMY.Models;
using System.Configuration;
using System.Net;
using System.Net.Mail;

namespace FluentMY
{
    public class EmailService : IIdentityMessageService
    {
        public Task SendAsync(IdentityMessage message)
        {
            var fromEmail = ConfigurationManager.AppSettings["SmtpFromEmail"];
            var fromName = ConfigurationManager.AppSettings["SmtpFromName"];

            var smtpHost = ConfigurationManager.AppSettings["SmtpHost"];
            var smtpPort = int.Parse(ConfigurationManager.AppSettings["SmtpPort"]);
            var smtpUser = ConfigurationManager.AppSettings["SmtpUser"];
            var smtpPass = ConfigurationManager.AppSettings["SmtpPass"];
            var enableSsl = bool.Parse(ConfigurationManager.AppSettings["SmtpEnableSsl"]);

            var mail = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = message.Subject,
                Body = message.Body,
                IsBodyHtml = true
            };
            mail.To.Add(message.Destination);

            var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl = enableSsl
            };

            return client.SendMailAsync(mail);
        }
    }

    public class SmsService : IIdentityMessageService
    {
        public Task SendAsync(IdentityMessage message)
        {
            // Plug in your SMS service here to send a text message.
            return Task.FromResult(0);
        }
    }

    // Configure the application user manager used in this application. UserManager is defined in ASP.NET Identity and is used by the application.
    public class ApplicationUserManager : UserManager<ApplicationUser>
    {
        public ApplicationUserManager(IUserStore<ApplicationUser> store)
            : base(store)
        {
        }

        public static ApplicationUserManager Create(IdentityFactoryOptions<ApplicationUserManager> options, IOwinContext context) 
        {
            var manager = new ApplicationUserManager(new UserStore<ApplicationUser>(context.Get<ApplicationDbContext>()));
            // Configure validation logic for usernames
            manager.UserValidator = new UserValidator<ApplicationUser>(manager)
            {
                AllowOnlyAlphanumericUserNames = false,
                RequireUniqueEmail = true
            };

            // Configure validation logic for passwords
            manager.PasswordValidator = new PasswordValidator
            {
                RequiredLength = 6,
                RequireNonLetterOrDigit = true,
                RequireDigit = true,
                RequireLowercase = true,
                RequireUppercase = true,
            };

            // Configure user lockout defaults
            manager.UserLockoutEnabledByDefault = true;
            manager.DefaultAccountLockoutTimeSpan = TimeSpan.FromMinutes(5);
            manager.MaxFailedAccessAttemptsBeforeLockout = 5;

            // Register two factor authentication providers. This application uses Phone and Emails as a step of receiving a code for verifying the user
            // You can write your own provider and plug it in here.
            manager.RegisterTwoFactorProvider("Phone Code", new PhoneNumberTokenProvider<ApplicationUser>
            {
                MessageFormat = "Your security code is {0}"
            });
            manager.RegisterTwoFactorProvider("Email Code", new EmailTokenProvider<ApplicationUser>
            {
                Subject = "Security Code",
                BodyFormat = "Your security code is {0}"
            });
            manager.EmailService = new EmailService();
            manager.SmsService = new SmsService();
            var dataProtectionProvider = options.DataProtectionProvider;
            if (dataProtectionProvider != null)
            {
                manager.UserTokenProvider = 
                    new DataProtectorTokenProvider<ApplicationUser>(dataProtectionProvider.Create("ASP.NET Identity"));
            }
            return manager;
        }
    }

    // Configure the application sign-in manager which is used in this application.
    public class ApplicationSignInManager : SignInManager<ApplicationUser, string>
    {
        public ApplicationSignInManager(ApplicationUserManager userManager, IAuthenticationManager authenticationManager)
            : base(userManager, authenticationManager)
        {
        }

        public override Task<ClaimsIdentity> CreateUserIdentityAsync(ApplicationUser user)
        {
            return user.GenerateUserIdentityAsync((ApplicationUserManager)UserManager);
        }

        public static ApplicationSignInManager Create(IdentityFactoryOptions<ApplicationSignInManager> options, IOwinContext context)
        {
            return new ApplicationSignInManager(context.GetUserManager<ApplicationUserManager>(), context.Authentication);
        }
    }

    /*public class ApplicationDbInitializer : CreateDatabaseIfNotExists<ApplicationDbContext>
    {
        protected override void Seed(ApplicationDbContext context)
        {
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));

            string[] roles = { "Student", "Educator", "Admin" };
            foreach (var r in roles)
            {
                if (!roleManager.RoleExists(r))
                    roleManager.Create(new IdentityRole(r));
            }

            if (!context.Courses.Any())
            {
                context.Courses.Add(new Models.Course
                {
                    Title = "Bahasa Malaysia Starter",
                    Language = "Bahasa Malaysia",
                    Level = "Beginner",
                    EstimatedDuration = "1–2 weeks",
                    Description = "Learn essential BM phrases and everyday conversations in Malaysian context."
                });

                context.Courses.Add(new Models.Course
                {
                    Title = "Daily English",
                    Language = "English",
                    Level = "Beginner/Intermediate",
                    EstimatedDuration = "2–3 weeks",
                    Description = "Improve everyday English for study, work, and communication."
                });

                context.Courses.Add(new Models.Course
                {
                    Title = "Mandarin Basics",
                    Language = "Mandarin",
                    Level = "Beginner",
                    EstimatedDuration = "2 weeks",
                    Description = "Start with pronunciation, simple dialogues, and basic characters."
                });

                context.Courses.Add(new Models.Course
                {
                    Title = "Tamil Starter",
                    Language = "Tamil",
                    Level = "Beginner",
                    EstimatedDuration = "2 weeks",
                    Description = "Learn common words and phrases used in daily Malaysian life."
                });

                context.SaveChanges();
            }

            base.Seed(context);
        }
    }*/
}
