using System.ComponentModel.DataAnnotations;

namespace FluentMY.ViewModels
{
    public class AdminResetPasswordViewModel
    {
        [Required]
        public string UserId { get; set; }

        public string FullName { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public bool IsSelf { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "New password and confirm password do not match.")]
        public string ConfirmPassword { get; set; }
    }
}