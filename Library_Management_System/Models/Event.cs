using System.ComponentModel.DataAnnotations;

namespace Library_Management_System.Models
{
    public class Event
    {
        public int Id { get; set; }
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public DateTime Date { get; set; }

        [Required]
        [StringLength(200)]
        public string Location { get; set; } = string.Empty;

        public string? ImagePath { get; set; }
    }
}
