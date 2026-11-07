using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FluentMY.Models
{
    public class Feedback
    {
        public int FeedbackId { get; set; }

        [Required]
        public int SubmissionId { get; set; }
        public virtual Submission Submission { get; set; }

        public string EducatorId { get; set; }

        [StringLength(20)]
        public string FeedbackType { get; set; }   // AI / Educator

        [Required]
        [StringLength(1000)]
        public string CommentText { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("EducatorId")]
        public virtual ApplicationUser Educator { get; set; }
    }
}