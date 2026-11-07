using System;

namespace FluentMY.ViewModels
{
    public class AdminCourseListItemViewModel
    {
        public int CourseId { get; set; }

        public string Title { get; set; }
        public string Description { get; set; }

        public string EducatorId { get; set; }
        public string EducatorName { get; set; }

        public int LessonCount { get; set; }
        public int ExerciseCount { get; set; }
        public int EnrollmentCount { get; set; }
        public int SubmissionCount { get; set; }

        public decimal AveragePercentage { get; set; }
    }
}