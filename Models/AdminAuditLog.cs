using System;
using System.ComponentModel.DataAnnotations;

namespace FluentMY.Models
{
    public class AdminAuditLog
    {
        public int AdminAuditLogId { get; set; }

        [StringLength(128)]
        public string AdminUserId { get; set; }

        [StringLength(200)]
        public string AdminUserName { get; set; }

        [Required]
        [StringLength(100)]
        public string ActionType { get; set; }

        [Required]
        [StringLength(100)]
        public string EntityType { get; set; }

        [StringLength(128)]
        public string EntityId { get; set; }

        [StringLength(200)]
        public string EntityDisplayName { get; set; }

        public string Details { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}