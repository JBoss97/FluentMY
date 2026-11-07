using System.Collections.Generic;
using System.Web.Mvc;

namespace FluentMY.ViewModels
{
    public class AdminCourseIndexViewModel
    {
        public AdminCourseIndexViewModel()
        {
            Courses = new List<AdminCourseListItemViewModel>();
            Educators = new List<SelectListItem>();
        }

        public string Search { get; set; }
        public string EducatorId { get; set; }
        public string Sort { get; set; }

        public int Page { get; set; }
        public int TotalPages { get; set; }

        public int TotalCourses { get; set; }
        public int TotalLessons { get; set; }
        public int TotalEnrollments { get; set; }
        public int TotalSubmissions { get; set; }

        public List<AdminCourseListItemViewModel> Courses { get; set; }
        public List<SelectListItem> Educators { get; set; }
    }
}