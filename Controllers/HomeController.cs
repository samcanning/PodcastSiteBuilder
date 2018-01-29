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

        private AdminContext _context;

        public HomeController(AdminContext context)
        {
            _context = context;
        }

        public IActionResult Index() //index page currently only displays list of all episodes with podcast title
        //need to add navigation - probably change podcast episodes display to its own route, not index?
        {
            // List<RssData> rssFeedData = GetRss();
            List<RssData> rssFeedData = GetEpisodes(5);
            ViewBag.PodcastTitle = _context.Podcasts.FirstOrDefault().Title;
            return View(rssFeedData);
        }

        public List<RssData> GetEpisodes(int count, int start = 0) //count determines number of episodes to display, start determines where to begin (default 0) 
        //currently only supports one RSS feed
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(_context.Podcasts.FirstOrDefault().Feed);
            XmlElement root = doc.DocumentElement;
            XmlNodeList nodes = root.SelectNodes("channel/item");
            List<RssData> rssFeedData = new List<RssData>();
            for(int i = start; i < count + start; i++)
            {
                RssData episode = new RssData();
                try
                {
                    episode.Title = nodes[i].SelectSingleNode("title").InnerText;
                    episode.Image = nodes[i].SelectSingleNode("./*[name()='itunes:image']/@href").InnerText;
                    episode.Link = nodes[i].SelectSingleNode("link").InnerText; //may decide later to remove the Link property
                    episode.PubDate = nodes[i].SelectSingleNode("pubDate").InnerText.Substring(5, 11);
                    //may decide later to reformat publish date string
                    //currently just truncates from "Day, DD MMM YYYY HH:mm:SS +TTTT" to "DD MM YYYY"
                    episode.Audio = nodes[i].SelectSingleNode("enclosure/@url").InnerText;
                    episode.AudioType = nodes[i].SelectSingleNode("enclosure/@type").InnerText;
                    try
                    {
                        episode.Description = nodes[i].SelectSingleNode("./*[name()='itunes:summary']").InnerText;
                    }
                    catch
                    {
                        try
                        {
                            episode.Description = nodes[i].SelectSingleNode("description").InnerText;
                        }
                        catch
                        { }
                    }

                rssFeedData.Add(episode);
                    
                }
                catch { } //no handling yet for any RSS feed entries without all of these attributes - would this ever happen for an actual episode? (as opposed to non-episode posts, which do appear in the feed)
                
            }
            return rssFeedData;
        }

        /* pulls title from RSS feed - probably no longer needed now that DB contains title
        public string GetTitle()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("http://feeds.feedburner.com/mbmbam?format=xml"); //using MBMBAM as a temporary RSS feed - will later allow admin to select RSS feed
            XmlElement root = doc.DocumentElement;
            return root.SelectSingleNode("channel/title").InnerText;
        }
        */
    }
}
