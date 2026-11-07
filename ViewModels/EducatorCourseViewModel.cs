using System;
using System.ComponentModel.DataAnnotations;

namespace FluentMY.ViewModels
{
    public class EducatorCourseViewModel
    {
        public int CourseId { get; set; }

        [Required]
        [StringLength(120)]
        public string Title { get; set; }

        [Required]
        [StringLength(50)]
        public string Language { get; set; }

        [StringLength(50)]
        public string Level { get; set; }

        [Display(Name = "Estimated Duration")]
        [StringLength(50)]
        public string EstimatedDuration { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public int LessonCount { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public DateTime LastActivityAt => UpdatedAt ?? CreatedAt;
    }
}