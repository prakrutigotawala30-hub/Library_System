using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagementSystem.ClassLibrary.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public string MemberId { get; set; }

        [ForeignKey("MemberId")]
        public ApplicationUser Member { get; set; }

        [Required]
        [StringLength(300)]
        public string Message { get; set; }

        [StringLength(500)]
        public string? Link { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedOn { get; set; } = DateTime.Now;
    }
}