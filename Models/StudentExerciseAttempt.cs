using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FluentMY.Models
{
    public class StudentExerciseAttempt
    {
        public int StudentExerciseAttemptId { get; set; }

        [Required]
        public string StudentId { get; set; }

        [ForeignKey("StudentId")]
        public virtual ApplicationUser Student { get; set; }

        [Required]
        public int ExerciseId { get; set; }
        public virtual Exercise Exercise { get; set; }

        [Required]
        public int StudentQuizAttemptId { get; set; }
        public virtual StudentQuizAttempt StudentQuizAttempt { get; set; }

        [StringLength(500)]
        public string QuestionTextSnapshot { get; set; }

        [StringLength(1)]
        public string SelectedAnswer { get; set; }

        [StringLength(1)]
        public string CorrectAnswerSnapshot { get; set; }

        public bool IsCorrect { get; set; }

        public int Score { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }
}