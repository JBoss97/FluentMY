using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FluentMY.Models
{
    public class StudentGameProfile
    {
        public int StudentGameProfileId { get; set; }

        [Required]
        [StringLength(128)]
        public string StudentId { get; set; }

        [ForeignKey("StudentId")]
        public virtual ApplicationUser Student { get; set; }

        public int TotalXP { get; set; }
        public int CurrentStreakDays { get; set; }
        public int LongestStreakDays { get; set; }

        public int Hearts { get; set; } = 5;

        public DateTime? LastPracticeDateUtc { get; set; }
    }
}