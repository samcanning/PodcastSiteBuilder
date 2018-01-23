using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PodcastSiteBuilder.Models;
using System.Net;
using System.Xml;
using System.Xml.Linq;
using System.Data;

namespace PodcastSiteBuilder.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index() //index page currently only displays list of all episodes with podcast title
        //need to add navigation - probably change podcast episodes display to its own route, not index?
        {
            // List<RssData> rssFeedData = GetRss();
            List<RssData> rssFeedData = GetEpisodes(5);
            ViewBag.PodcastTitle = GetTitle();
            return View(rssFeedData);
        }

        public List<RssData> GetEpisodes(int count, int start = 0) //count determines number of episodes to display, start determines where to begin (default 0) 
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("http://feeds.feedburner.com/mbmbam?format=xml"); //using MBMBAM as a temporary RSS feed - will later allow admin to select RSS feed
            XmlElement root = doc.DocumentElement;
            XmlNodeList nodes = root.SelectNodes("channel/item");
            List<RssData> rssFeedData = new List<RssData>();
            for(int i = start; i < count + start; i++)
            {
                try
                {
                    RssData episode = new RssData
                    {
                        Title = nodes[i].SelectSingleNode("title").InnerText,
                        Description = nodes[i].SelectSingleNode("./*[name()='itunes:summary']").InnerText,
                        Image = nodes[i].SelectSingleNode("./*[name()='itunes:image']/@href").InnerText,
                        Link = nodes[i].SelectSingleNode("link").InnerText, //may decide later to remove the Link property
                        PubDate = nodes[i].SelectSingleNode("pubDate").InnerText.Substring(5, 11),
                        //may decide later to reformat publish date string
                        //currently just truncates from "Day, DD MMM YYYY HH:mm:SS +TTTT" to "DD MM YYYY"
                        Audio = nodes[i].SelectSingleNode("enclosure/@url").InnerText,
                        AudioType = nodes[i].SelectSingleNode("enclosure/@type").InnerText
                    };
                    rssFeedData.Add(episode);
                }
                catch { } //no handling yet for any RSS feed entries without all of these attributes - would this ever happen for an actual episode? (as opposed to non-episode posts, which do appear in the feed)
            }
            return rssFeedData;
        }

        public string GetTitle() //once database is set up, title will be stored in database with other basic podcast info instead of pulling from RSS feed on every page view
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("http://feeds.feedburner.com/mbmbam?format=xml"); //using MBMBAM as a temporary RSS feed - will later allow admin to select RSS feed
            XmlElement root = doc.DocumentElement;
            return root.SelectSingleNode("channel/title").InnerText;
        }
        
        //probably remove this function, GetEpisodes is more versatile
        public List<RssData> GetRss() //pull episode data from RSS feed - title, description, link to source, etc
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("http://feeds.feedburner.com/mbmbam?format=xml"); //using MBMBAM as a temporary RSS feed - will later allow admin to select RSS feed
            XmlElement root = doc.DocumentElement;
            XmlNodeList nodes = root.SelectNodes("channel/item");
            List<RssData> rssFeedData = new List<RssData>();
            for(int i = 0; i < 5; i++)
            {
                try
                {
                    RssData episode = new RssData
                    {
                        Title = nodes[i].SelectSingleNode("title").InnerText,
                        Description = nodes[i].SelectSingleNode("./*[name()='itunes:summary']").InnerText,
                        Image = nodes[i].SelectSingleNode("./*[name()='itunes:image']/@href").InnerText,
                        Link = nodes[i].SelectSingleNode("link").InnerText, //may decide later to remove the Link property
                        PubDate = nodes[i].SelectSingleNode("pubDate").InnerText.Substring(5, 11),
                        //may decide later to reformat publish date string
                        //currently just truncates from "Day, DD MMM YYYY HH:mm:SS +TTTT" to "DD MM YYYY"
                        Audio = nodes[i].SelectSingleNode("enclosure/@url").InnerText,
                        AudioType = nodes[i].SelectSingleNode("enclosure/@type").InnerText
                    };
                    rssFeedData.Add(episode);
                }
                catch { } //no handling yet for any RSS feed entries without all of these attributes - would this ever happen for an actual episode? (as opposed to non-episode posts, which do appear in the feed)
            }
            /* this version displays every single episode of the podcast - probably not ideal
            foreach(XmlNode x in nodes)
            {
                try
                {
                    RssData episode = new RssData
                    {
                        Title = x.SelectSingleNode("title").InnerText,
                        Description = x.SelectSingleNode("./*[name()='itunes:summary']").InnerText,
                        Image = x.SelectSingleNode("./*[name()='itunes:image']/@href").InnerText,
                        Link = x.SelectSingleNode("link").InnerText, //may decide later to remove the Link property
                        PubDate = x.SelectSingleNode("pubDate").InnerText.Substring(5, 11),
                        //may decide later to reformat publish date string
                        //currently just truncates from "Day, DD MMM YYYY HH:mm:SS +TTTT" to "DD MM YYYY"
                        Audio = x.SelectSingleNode("enclosure/@url").InnerText,
                        AudioType = x.SelectSingleNode("enclosure/@type").InnerText
                    };
                    rssFeedData.Add(episode);
                }
                catch { } //no handling yet for any RSS feed entries without all of these attributes - would this ever happen for an actual episode? (as opposed to non-episode posts, which do appear in the feed)
            }
            */
            return rssFeedData;
        }

        

        

    }
}
