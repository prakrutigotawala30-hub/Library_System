using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }

        public string? ProfileImagePath { get; set; }

        public bool NotificationPrefs { get; set; }

    }
}