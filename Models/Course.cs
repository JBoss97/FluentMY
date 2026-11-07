using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FluentMY.Models
{
    public class Course
    {
        public int CourseId { get; set; }

        [Required, StringLength(120)]
        public string Title { get; set; }

        [Required, StringLength(50)]
        public string Language { get; set; }

        [StringLength(50)]
        public string Level { get; set; }

        [StringLength(50)]
        public string EstimatedDuration { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [StringLength(128)]
        public string EducatorId { get; set; }

        [ForeignKey("EducatorId")]
        public virtual ApplicationUser Educator { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<Lesson> Lessons { get; set; } = new HashSet<Lesson>();

        [StringLength(100)]
        public string SeedKey { get; set; }
    }
}