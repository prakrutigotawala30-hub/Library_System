using System;
using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models
{
    public class Event
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Event title is required")]
        [StringLength(150)]
        public string Title { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(1000)]
        public string Description { get; set; }

        [Required(ErrorMessage = "Event date is required")]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Location is required")]
        [StringLength(200)]
        public string Location { get; set; }

        [StringLength(300)]
        public string? ImagePath { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;
    }
}