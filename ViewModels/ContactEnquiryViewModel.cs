using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace FluentMY.ViewModels
{
    public class ContactEnquiryViewModel
    {
        [Required]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(150)]
        [Display(Name = "Email Address")]
        public string Email { get; set; }

        [Required]
        [StringLength(150)]
        public string Subject { get; set; }

        [Required]
        [Display(Name = "I am a")]
        public string Role { get; set; }

        [Required]
        [StringLength(2000)]
        public string Message { get; set; }
    }
}