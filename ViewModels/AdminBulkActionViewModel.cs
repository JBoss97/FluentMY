using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FluentMY.ViewModels
{
    public class AdminBulkActionViewModel
    {
        [Required]
        public string ActionName { get; set; }

        public List<string> SelectedUserIds { get; set; } = new List<string>();

        public string SearchTerm { get; set; }
        public string SelectedRole { get; set; }
        public bool? EmailConfirmed { get; set; }
        public string SortBy { get; set; }
        public string SortDirection { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}