using LibraryManagementSystem.Models;
using System.Collections.Generic;

namespace LibraryManagementSystem.ViewModels
{
    public class BookDetailsViewModel
    {
        public Book Book { get; set; }

        public List<BorrowRecord> BorrowHistory { get; set; } = new List<BorrowRecord>();
    }
}