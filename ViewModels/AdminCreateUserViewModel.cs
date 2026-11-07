using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace FluentMY.ViewModels
{
    public class AdminCreateUserViewModel
    {
        [Required]
        [StringLength(80)]
        public string FullName { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Username")]
        public string UserName { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [System.ComponentModel.DataAnnotations.Compare("Password", ErrorMessage = "Password and confirm password do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; }

        [Required]
        [Display(Name = "Role")]
        public string SelectedRole { get; set; }

        [Display(Name = "Email Confirmed")]
        public bool EmailConfirmed { get; set; }

        public IEnumerable<SelectListItem> AvailableRoles { get; set; } = new List<SelectListItem>();
    }
}