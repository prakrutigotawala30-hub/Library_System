using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
 
namespace LibraryManagementSystem.ClassLibrary.Models
{
    public class Wishlist
    {
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string MemberId { get; set; } = string.Empty;

        [ForeignKey("MemberId")]
        public ApplicationUser Member { get; set; }

        [Required]
        public int BookId { get; set; }

        public string UserId { get; set; }

        [ForeignKey("BookId")]
        public Book Book { get; set; }

        public DateTime AddedOn { get; set; } = DateTime.Now;
    }
}

