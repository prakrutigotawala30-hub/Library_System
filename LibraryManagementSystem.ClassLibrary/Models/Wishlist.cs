using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagementSystem.ClassLibrary.Models
{
    public class Wishlist
    {
        public int Id { get; set; }

        // Foreign key to AspNetUsers.Id. Named MemberId because in this app
        // wishlists are owned by Member-role users, but technically it accepts
        // any ApplicationUser primary key.
        [Required]
        [StringLength(450)]
        public string MemberId { get; set; } = string.Empty;

        [ForeignKey("MemberId")]
        public ApplicationUser? Member { get; set; }

        [Required]
        public int BookId { get; set; }

        [ForeignKey("BookId")]
        public Book? Book { get; set; }

        public DateTime AddedOn { get; set; } = DateTime.Now;
    }
}
