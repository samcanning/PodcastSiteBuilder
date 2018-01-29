using System.Collections.Generic;

namespace PodcastSiteBuilder.Models
{
    public class HostDisplay
    {
        public int id {get;set;}
        public string name {get;set;}
        public string image {get;set;}
        public string bio {get;set;}

        public List<Link> links {get;set;}
    }
}