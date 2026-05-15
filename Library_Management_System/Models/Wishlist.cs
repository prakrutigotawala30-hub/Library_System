using LibraryManagementSystem.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Library_Management_System.Models
{
    public class Wishlist
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

        public DateTime AddedOn { get; set; } = DateTime.Now;
    }
}