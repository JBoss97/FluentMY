using System;
using System.Collections.Generic;

namespace FluentMY.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalStudents { get; set; }
        public int TotalEducators { get; set; }
        public int TotalAdmins { get; set; }

        public int TotalCourses { get; set; }
        public int TotalLessons { get; set; }
        public int TotalExercises { get; set; }

        public int TotalEnrollments { get; set; }
        public int TotalSubmissions { get; set; }
        public int PendingReviewCount { get; set; }

        // NEW
        public int TotalEnquiries { get; set; }
        public int UnreadEnquiriesCount { get; set; }
        public int TotalAuditLogs { get; set; }

        public List<AdminRecentSubmissionViewModel> RecentSubmissions { get; set; } = new List<AdminRecentSubmissionViewModel>();
        public List<AdminQuickLinkViewModel> QuickLinks { get; set; } = new List<AdminQuickLinkViewModel>();
        public List<AdminDashboardAlertViewModel> Alerts { get; set; } = new List<AdminDashboardAlertViewModel>();
        public List<AdminRecentEnquiryItemViewModel> RecentEnquiries { get; set; } = new List<AdminRecentEnquiryItemViewModel>();
        public List<AdminAuditLogItemViewModel> RecentAuditLogs { get; set; } = new List<AdminAuditLogItemViewModel>();
    }

    public class AdminRecentSubmissionViewModel
    {
        public int SubmissionId { get; set; }
        public string StudentName { get; set; }
        public string CourseTitle { get; set; }
        public string LessonTitle { get; set; }
        public int Percentage { get; set; }
        public bool IsReviewed { get; set; }
        public DateTime SubmittedAt { get; set; }
    }

    public class AdminRecentEnquiryItemViewModel
    {
        public int ContactEnquiryId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string Subject { get; set; }
        public bool IsRead { get; set; }
        public DateTime SubmittedAtUtc { get; set; }
    }

    public class AdminAuditLogItemViewModel
    {
        public int AdminAuditLogId { get; set; }
        public string AdminUserName { get; set; }
        public string ActionType { get; set; }
        public string EntityType { get; set; }
        public string EntityDisplayName { get; set; }
        public string Details { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}