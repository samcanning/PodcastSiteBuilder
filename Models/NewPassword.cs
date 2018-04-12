using System.ComponentModel.DataAnnotations;

namespace PodcastSiteBuilder.Models
{
    public class NewPassword
    {
        [DataType(DataType.Password)]
        [Required(ErrorMessage = "Must enter your old password.")]
        public string oldpw {get;set;}
        [DataType(DataType.Password)]
        [Required(ErrorMessage = "Must enter a new password.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        [RegularExpression("[A-Za-z0-9!?_-]+", ErrorMessage = "Password must be alphanumeric characters or special characters. (? ! _ -)")]
        public string newpw {get;set;}
        [DataType(DataType.Password)]
        [Required(ErrorMessage = "Must reenter new password to confirm.")]
        [Compare("newpw", ErrorMessage = "Passwords do not match.")]
        public string newpwconfirm {get;set;}
    }
}