using System.Collections.Generic;
using System.Web.Mvc;

namespace FluentMY.ViewModels
{
    public class AdminUserIndexViewModel
    {
        public string SearchTerm { get; set; }
        public string SelectedRole { get; set; }
        public bool? EmailConfirmed { get; set; }
        public string SortBy { get; set; }
        public string SortDirection { get; set; }

        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }

        public IEnumerable<SelectListItem> AvailableRoles { get; set; } = new List<SelectListItem>();
        public List<AdminUserListItemViewModel> Users { get; set; } = new List<AdminUserListItemViewModel>();
    }
}