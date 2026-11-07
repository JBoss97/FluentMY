using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FluentMY.Models
{
    public class StudentLessonProgress
    {
        public int StudentLessonProgressId { get; set; }

        [Required]
        public string StudentId { get; set; }

        [Required]
        public int LessonId { get; set; }

        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int AttemptsCount { get; set; }
        public int? LastScorePercent { get; set; }
        public int? BestScorePercent { get; set; }
        public DateTime? LastAttemptAt { get; set; }

        [ForeignKey("StudentId")]
        public virtual ApplicationUser Student { get; set; }

        public virtual Lesson Lesson { get; set; }
    }
}