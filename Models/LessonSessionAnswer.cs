using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FluentMY.Models
{
    public class LessonSessionAnswer
    {
        public int LessonSessionAnswerId { get; set; }

        [Required]
        public int LessonSessionId { get; set; }

        [ForeignKey("LessonSessionId")]
        public virtual LessonSession LessonSession { get; set; }

        [Required]
        public int ExerciseId { get; set; }

        [ForeignKey("ExerciseId")]
        public virtual Exercise Exercise { get; set; }

        public string SubmittedAnswer { get; set; }
        public string SubmittedAnswerJson { get; set; }

        public bool IsCorrect { get; set; }
        public int XPGranted { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}