using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace FluentMY.Models
{
    public class SubmissionAnswer
    {
        public int SubmissionAnswerId { get; set; }

        [Required]
        public int SubmissionId { get; set; }
        public virtual Submission Submission { get; set; }

        [Required]
        public int ExerciseId { get; set; }
        public virtual Exercise Exercise { get; set; }

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