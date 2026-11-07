using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FluentMY.Models
{
    public class StudentCourse
    {
        public int StudentCourseId { get; set; }

        [Required]
        public string StudentId { get; set; } // AspNetUsers.Id

        [Required]
        public int CourseId { get; set; }

        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

        // Continue learning
        public int? CurrentLessonId { get; set; }

        // Finish course
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }

        [ForeignKey("StudentId")]
        public virtual ApplicationUser Student { get; set; }

        public virtual Course Course { get; set; }
    }
}