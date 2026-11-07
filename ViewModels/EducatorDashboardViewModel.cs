using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FluentMY.ViewModels
{
    public class EducatorDashboardViewModel
    {
        public string EducatorDisplayName { get; set; }

        public int TotalCourses { get; set; }
        public int TotalLessons { get; set; }
        public int TotalExercises { get; set; }
        public int TotalSubmissions { get; set; }
        public int PendingReviewCount { get; set; }
    }
}