using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LibraryManagementSystem.ViewModels
{
    public class BorrowViewModel
    {
        [Required(ErrorMessage = "Please select a book")]
        public int BookId { get; set; }

        [Required(ErrorMessage = "Please select a member")]
        public int MemberId { get; set; }

        [Required(ErrorMessage = "Due date is required")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        public List<SelectListItem> Books { get; set; } = new();
        public List<SelectListItem> Members { get; set; } = new();
    }
}