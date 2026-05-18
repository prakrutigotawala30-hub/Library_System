using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Library_Management_System.ViewModels
{
    public class ProfileViewModel
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string Name { get; set; }

        [Phone(ErrorMessage = "Enter valid phone number")]
        [Display(Name = "Phone Number")]
        public string Phone { get; set; }

        [Display(Name = "Profile Photo")]
        public IFormFile? AvatarFile { get; set; }

        public string? ProfileImagePath { get; set; }

        [Display(Name = "Enable Notifications")]
        public bool NotificationPrefs { get; set; }
    }
}