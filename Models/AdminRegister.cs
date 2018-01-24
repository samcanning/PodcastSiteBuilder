using System.ComponentModel.DataAnnotations;

namespace PodcastSiteBuilder.Models
{
    public class AdminRegister
    {
        [Required(ErrorMessage = "Username is required.")]
        [MinLength(5, ErrorMessage = "Username must be at least 5 characters.")]
        [RegularExpression("[A-Za-z0-9]+", ErrorMessage = "Username must be alphanumeric characters only.")]
        public string Username {get;set;}
        [Required(ErrorMessage = "Password is required.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        [RegularExpression("[A-Za-z0-9!?_-]+", ErrorMessage = "Password must be alphanumeric characters or special characters. (? ! _ -)")]
        [DataType(DataType.Password)]
        public string Password {get;set;}
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string PasswordConfirm {get;set;}
    }
}