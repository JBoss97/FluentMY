using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FluentMY.ViewModels
{
    public class CourseCatalogItemViewModel
    {
        public int CourseId { get; set; }
        public string Title { get; set; }
        public string Language { get; set; }
        public string Level { get; set; }
        public string EstimatedDuration { get; set; }
        public string Description { get; set; }

        public int LessonCount { get; set; }

        public bool IsEnrolled { get; set; }
        public bool IsCompleted { get; set; }
        public int ProgressPercent { get; set; }
        public int? ContinueLessonId { get; set; }
    }

    public class CourseDetailsViewModel
    {
        public int CourseId { get; set; }
        public string Title { get; set; }
        public string Language { get; set; }
        public string Level { get; set; }
        public string EstimatedDuration { get; set; }
        public string Description { get; set; }

        public bool IsEnrolled { get; set; }
        public bool IsCompleted { get; set; }
        public int ProgressPercent { get; set; }
        public int? ContinueLessonId { get; set; }

        public List<CourseLessonItemViewModel> Lessons { get; set; } = new List<CourseLessonItemViewModel>();
    }

    public class CourseLessonItemViewModel
    {
        public int LessonId { get; set; }
        public string Title { get; set; }
        public string DifficultyLevel { get; set; }
        public bool IsCompleted { get; set; }
    }
}