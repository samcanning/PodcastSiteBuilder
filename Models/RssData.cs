namespace PodcastSiteBuilder.Models
{
    public class RssData
    {
        public string Title {get;set;}
        public string Link {get;set;} //this may be unnecessary?
        public string Audio {get;set;}
        public string AudioType {get;set;}
        public string Image {get;set;}
        public string Description {get;set;}
        public string PubDate {get;set;}
    }
}