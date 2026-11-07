using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FluentMY.ViewModels
{
    public class AdminBulkReassignOwnershipItemViewModel
    {
        public string EducatorId { get; set; }
        public string EducatorName { get; set; }
        public string Email { get; set; }
        public int OwnedCourseCount { get; set; }
        public int OwnedLessonCount { get; set; }
    }
}