using System;

namespace FluentMY.ViewModels
{
    public class AdminUserListItemViewModel
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string RoleName { get; set; }
        public bool EmailConfirmed { get; set; }
        public DateTime? JoinedAt { get; set; }
        public bool IsSelf { get; set; }

        public int OwnedCourseCount { get; set; }
        public int OwnedLessonCount { get; set; }
        public bool HasOwnedContent
        {
            get { return OwnedCourseCount > 0 || OwnedLessonCount > 0; }
        }
    }
}