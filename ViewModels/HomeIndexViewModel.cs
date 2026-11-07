using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FluentMY.ViewModels
{
    public class HomeIndexViewModel
    {
        public HomeCourseCardViewModel RecommendedCourse { get; set; }
        public List<HomeCourseCardViewModel> PopularCourses { get; set; } = new List<HomeCourseCardViewModel>();
    }

    public class HomeCourseCardViewModel
    {
        public int CourseId { get; set; }
        public string Title { get; set; }
        public string Language { get; set; }
        public string Level { get; set; }
        public string EstimatedDuration { get; set; }
        public string Description { get; set; }
        public int LessonCount { get; set; }
    }
}