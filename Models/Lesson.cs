using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FluentMY.Models
{
    public class Lesson
    {
        public int LessonId { get; set; }

        [Required, StringLength(120)]
        public string Title { get; set; }

        [StringLength(20)]
        public string DifficultyLevel { get; set; }  // "A1", "A2", etc.

        public string Content { get; set; }          // later you can store HTML/Markdown

        // FK - Course
        public int CourseId { get; set; }
        public virtual Course Course { get; set; }

        // FK - Educator
        [Required]
        public string EducatorId { get; set; }

        [ForeignKey("EducatorId")]
        public virtual ApplicationUser Educator { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<Exercise> Exercises { get; set; }

        [StringLength(150)]
        public string SeedKey { get; set; }
    }
}