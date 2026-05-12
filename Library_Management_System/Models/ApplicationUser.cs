using Microsoft.AspNetCore.Identity;

namespace Library_Management_System.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }

        public string Address { get; set; }
    }
}