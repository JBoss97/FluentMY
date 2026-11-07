using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace FluentMY.ViewModels
{
    public class AdminEditUserViewModel
    {
        public string UserId { get; set; }

        [Required]
        [StringLength(80)]
        public string FullName { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Username")]
        public string UserName { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        [Required]
        [Display(Name = "Role")]
        public string SelectedRole { get; set; }

        public bool EmailConfirmed { get; set; }

        public IEnumerable<SelectListItem> AvailableRoles { get; set; }

        public bool IsSelf { get; set; }
        public string CurrentRoleName { get; set; }
    }
}