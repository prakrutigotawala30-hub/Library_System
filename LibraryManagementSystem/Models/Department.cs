using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models
{
    public class Department
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public List<Book> Books { get; set; } = new();
    }
}