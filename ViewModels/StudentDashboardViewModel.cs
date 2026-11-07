using System;
using System.Collections.Generic;

namespace FluentMY.ViewModels
{
    public class StudentDashboardViewModel
    {
        public string FullNameOrEmail { get; set; }

        public int? ContinueLessonId { get; set; }
        public string ContinueLessonTitle { get; set; }
        public string ContinueCourseTitle { get; set; }

        public int EnrolledCoursesCount { get; set; }
        public int CompletedCoursesCount { get; set; }
        public int TotalLessonsInEnrolledCourses { get; set; }
        public int CompletedLessonsInEnrolledCourses { get; set; }
        public int OverallProgressPercent { get; set; }

        public List<MyCourseCard> MyCourses { get; set; } = new List<MyCourseCard>();
    }

    public class MyCourseCard
    {
        public int CourseId { get; set; }
        public string Title { get; set; }
        public string Language { get; set; }
        public string Level { get; set; }

        public int ProgressPercent { get; set; }
        public int? ContinueLessonId { get; set; }

        public bool IsCompleted { get; set; }
        public int TotalLessons { get; set; }
        public int CompletedLessons { get; set; }
    }
}
