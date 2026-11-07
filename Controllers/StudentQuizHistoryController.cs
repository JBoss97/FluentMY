using FluentMY.Models;
using FluentMY.ViewModels;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace FluentMY.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentQuizHistoryController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        public async Task<ActionResult> Index()
        {
            var userId = User.Identity.GetUserId();

            var items = await db.StudentQuizAttempts
                .Include(a => a.Lesson)
                .Include(a => a.Lesson.Course)
                .Where(a => a.StudentId == userId)
                .OrderByDescending(a => a.SubmittedAt)
                .Select(a => new StudentQuizHistoryItemViewModel
                {
                    AttemptId = a.StudentQuizAttemptId,
                    LessonId = a.LessonId,
                    LessonTitle = a.Lesson.Title,
                    CourseTitle = a.Lesson.Course.Title,
                    AttemptNumber = a.AttemptNumber,
                    TotalQuestions = a.TotalQuestions,
                    CorrectAnswers = a.CorrectAnswers,
                    TotalScore = a.TotalScore,
                    Percentage = a.Percentage,
                    SubmittedAt = a.SubmittedAt
                })
                .ToListAsync();

            return View(items);
        }
    }
}