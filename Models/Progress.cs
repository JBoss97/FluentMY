using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FluentMY.Models
{
    public class Progress
    {
        public int ProgressId { get; set; }

        [Required]
        public string StudentId { get; set; }

        [Required]
        public int CourseId { get; set; }

        public int TotalLessons { get; set; }
        public int CompletedLessons { get; set; }
        public int CompletionRate { get; set; }   // 0 - 100

        public int? LastLessonId { get; set; }
        public bool IsCourseCompleted { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        [ForeignKey("StudentId")]
        public virtual ApplicationUser Student { get; set; }

        public virtual Course Course { get; set; }
    }
}