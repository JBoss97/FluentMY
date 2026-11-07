using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FluentMY.Models
{
    public class StudentQuizAttempt
    {
        public int StudentQuizAttemptId { get; set; }

        [Required]
        public string StudentId { get; set; }

        [Required]
        public int LessonId { get; set; }

        public int AttemptNumber { get; set; }

        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public int TotalScore { get; set; }
        public int Percentage { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("StudentId")]
        public virtual ApplicationUser Student { get; set; }

        public virtual Lesson Lesson { get; set; }

        public virtual ICollection<StudentExerciseAttempt> Answers { get; set; }
    }
}