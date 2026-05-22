using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagementSystem.ClassLibrary.Models
{
    public class Wishlist
    {
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string MemberId { get; set; }

        [ForeignKey(nameof(MemberId))]
        public ApplicationUser Member { get; set; }

        public int? BookId { get; set; }

        [ForeignKey(nameof(BookId))]
        public Book Book { get; set; }

        public int? EventId { get; set; }

        [ForeignKey(nameof(EventId))]   
        public Event Event { get; set; }

        public DateTime AddedOn { get; set; } = DateTime.Now;
    }
}
