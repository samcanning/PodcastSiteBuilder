using System.ComponentModel.DataAnnotations;

namespace PodcastSiteBuilder.Models
{
    public class NewLink
    {
        [Required(ErrorMessage = "Must enter site.")]
        [RegularExpression("[A-Za-z0-9.-]+", ErrorMessage = "Invalid site name.")]
        public string site {get;set;}
        [Required(ErrorMessage = "Must enter URL.")]
        [DataType(DataType.Url, ErrorMessage = "Invalid URL.")]
        public string url {get;set;}
    }
}