using System.ComponentModel.DataAnnotations;

namespace PodcastSiteBuilder.Models
{
    public class AdminLogin
    {
        [Required(ErrorMessage = "Must enter username.")]
        public string Username {get;set;}
        [Required(ErrorMessage = "Must enter password.")]
        [DataType(DataType.Password)]
        public string Password {get;set;}
    }
}