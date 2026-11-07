using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FluentMY.Models
{
    public class ContactEnquiry
    {
        public int ContactEnquiryId { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; }

        [Required]
        [StringLength(150)]
        public string Subject { get; set; }

        [Required]
        [StringLength(30)]
        public string Role { get; set; }

        [Required]
        [StringLength(2000)]
        public string Message { get; set; }

        public DateTime SubmittedAtUtc { get; set; }

        public bool IsRead { get; set; }
    }
}