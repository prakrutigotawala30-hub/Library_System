using LibraryManagementSystem.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagementSystem.Models
{
    public class Reservation
    {
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string MemberId { get; set; }

        [ForeignKey("MemberId")]
        public ApplicationUser Member { get; set; }

        [Required]
        public int BookId { get; set; }

        [ForeignKey("BookId")]
        public Book Book { get; set; }

        public DateTime ReservedOn { get; set; } = DateTime.Now;

        [Required]
        public ReservationStatus Status { get; set; }
            = ReservationStatus.Waiting;
    }
}