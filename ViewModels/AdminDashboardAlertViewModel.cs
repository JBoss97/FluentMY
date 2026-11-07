using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FluentMY.ViewModels
{
    public class AdminDashboardAlertViewModel
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public string Severity { get; set; }   // info / warning / danger
        public string CountText { get; set; }
        public string ActionText { get; set; }
        public string ActionUrl { get; set; }
    }
}