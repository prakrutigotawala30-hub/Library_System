using LibraryManagementSystem.ViewModels;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;



namespace LibraryManagementSystem.ViewModels
{
    public class BookImportViewModel
    {
        [Required]
        public IFormFile CsvFile { get; set; }
    }
}