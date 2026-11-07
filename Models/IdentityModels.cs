using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FluentMY.Models
{
    public class ApplicationUser : IdentityUser
    {
        [StringLength(80)]
        public string FullName { get; set; }

        [StringLength(500)]
        public string Bio { get; set; }

        [StringLength(30)]
        public string LanguageLevel { get; set; }

        [StringLength(255)]
        public string ProfileImagePath { get; set; }

        public DateTime? JoinedAt { get; set; }

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            return userIdentity;
        }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<Course> Courses { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<StudentCourse> StudentCourses { get; set; }
        public DbSet<StudentLessonProgress> StudentLessonProgresses { get; set; }
        public DbSet<Exercise> Exercises { get; set; }
        public DbSet<Submission> Submissions { get; set; }
        public DbSet<SubmissionAnswer> SubmissionAnswers { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<Progress> Progresses { get; set; }
        public DbSet<AdminAuditLog> AdminAuditLogs { get; set; }
        public DbSet<LessonSession> LessonSessions { get; set; }
        public DbSet<LessonSessionAnswer> LessonSessionAnswers { get; set; }
        public DbSet<StudentGameProfile> StudentGameProfiles { get; set; }
        public DbSet<SpeakingAttempt> SpeakingAttempts { get; set; }
        public DbSet<ContactEnquiry> ContactEnquiries { get; set; }

        public ApplicationDbContext()
            : base("DefaultConnection", throwIfV1Schema: false)
        {
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Submission>()
                .HasRequired(x => x.Lesson)
                .WithMany()
                .HasForeignKey(x => x.LessonId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Submission>()
                .HasRequired(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<SubmissionAnswer>()
                .HasRequired(x => x.Submission)
                .WithMany(x => x.SubmissionAnswers)
                .HasForeignKey(x => x.SubmissionId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<SubmissionAnswer>()
                .HasRequired(x => x.Exercise)
                .WithMany()
                .HasForeignKey(x => x.ExerciseId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Feedback>()
                .HasRequired(x => x.Submission)
                .WithMany(x => x.Feedbacks)
                .HasForeignKey(x => x.SubmissionId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<Feedback>()
                .HasOptional(x => x.Educator)
                .WithMany()
                .HasForeignKey(x => x.EducatorId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Lesson>()
                .HasRequired(x => x.Educator)
                .WithMany()
                .HasForeignKey(x => x.EducatorId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Course>()
                .HasOptional(x => x.Educator)
                .WithMany()
                .HasForeignKey(x => x.EducatorId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<LessonSession>()
                .HasRequired(x => x.Lesson)
                .WithMany()
                .HasForeignKey(x => x.LessonId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<LessonSession>()
                .HasRequired(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<LessonSessionAnswer>()
                .HasRequired(x => x.LessonSession)
                .WithMany()
                .HasForeignKey(x => x.LessonSessionId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<LessonSessionAnswer>()
                .HasRequired(x => x.Exercise)
                .WithMany()
                .HasForeignKey(x => x.ExerciseId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<StudentGameProfile>()
                .HasRequired(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<SpeakingAttempt>()
                .HasRequired(x => x.Exercise)
                .WithMany()
                .HasForeignKey(x => x.ExerciseId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<SpeakingAttempt>()
                .HasRequired(x => x.Lesson)
                .WithMany()
                .HasForeignKey(x => x.LessonId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<SpeakingAttempt>()
                .HasRequired(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .WillCascadeOnDelete(false);
        }
    }
}