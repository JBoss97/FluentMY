using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace FluentMY.ViewModels
{
    public class AdminBulkReassignOwnershipViewModel
    {
        public List<string> SourceEducatorIds { get; set; } = new List<string>();
        public List<AdminBulkReassignOwnershipItemViewModel> SourceEducators { get; set; } = new List<AdminBulkReassignOwnershipItemViewModel>();

        [Required]
        [Display(Name = "Target Educator")]
        public string TargetEducatorId { get; set; }

        public IEnumerable<SelectListItem> AvailableEducators { get; set; } = new List<SelectListItem>();

        public string SearchTerm { get; set; }
        public string SelectedRole { get; set; }
        public bool? EmailConfirmed { get; set; }
        public string SortBy { get; set; }
        public string SortDirection { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}