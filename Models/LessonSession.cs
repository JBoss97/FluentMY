using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FluentMY.Models
{
    public class LessonSession
    {
        public int LessonSessionId { get; set; }

        [Required]
        public int LessonId { get; set; }

        [ForeignKey("LessonId")]
        public virtual Lesson Lesson { get; set; }

        [Required]
        [StringLength(128)]
        public string StudentId { get; set; }

        [ForeignKey("StudentId")]
        public virtual ApplicationUser Student { get; set; }

        public int CurrentIndex { get; set; }
        public int HeartsRemaining { get; set; }
        public int CurrentStreak { get; set; }
        public int TotalXP { get; set; }
        public int CorrectCount { get; set; }
        public int WrongCount { get; set; }

        public bool IsCompleted { get; set; }
        public bool RewardsApplied { get; set; }

        public DateTime StartedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }

        public virtual ICollection<LessonSessionAnswer> Answers { get; set; }
    }
}