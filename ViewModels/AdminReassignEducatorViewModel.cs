using System.Collections.Generic;
using System.Web.Mvc;

namespace FluentMY.ViewModels
{
    public class AdminReassignEducatorViewModel
    {
        public string SourceEducatorId { get; set; }
        public string SourceEducatorName { get; set; }

        public int OwnedCourseCount { get; set; }
        public int OwnedLessonCount { get; set; }

        public string TargetEducatorId { get; set; }

        public IEnumerable<SelectListItem> AvailableEducators { get; set; } = new List<SelectListItem>();
    }
}