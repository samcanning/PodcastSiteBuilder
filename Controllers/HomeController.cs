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

        public IActionResult Index()
        {
            // List<RssData> rssFeedData = GetRss();
            if(_context.Podcasts.FirstOrDefault() == null) return View("NoFeed"); //if no podcast feed is set, admin needs to add one
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
                    episode.Link = nodes[i].SelectSingleNode("link").InnerText;
                    episode.PubDate = nodes[i].SelectSingleNode("pubDate").InnerText.Substring(5, 11);
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

        [Route("hosts")]
        public IActionResult Hosts()
        {
            List<HostDisplay> HostDisplays = new List<HostDisplay>();
            if(_context.Hosts.FirstOrDefault() == null) return View("NoHosts");
            foreach(Host h in _context.Hosts)
            {
                HostDisplay newHost = new HostDisplay()
                {
                    name = h.name,
                    image = h.image,
                    bio = h.bio,
                    links = _context.Links.Where(l => l.host_id == h.id).ToList()
                };
                HostDisplays.Add(newHost);
            }
            return View(HostDisplays);
        }

        [Route("episodes")]
        public IActionResult Episodes(int page = 1)
        {
            if(_context.Podcasts.FirstOrDefault() == null) return RedirectToAction("Index");
            int offset = 10 * (page-1);
            List<RssData> episodes = GetEpisodes(10, offset);
            ViewBag.page = page;
            int epCount = EpisodeCount();
            ViewBag.totalEps = epCount;
            ViewBag.totalPages = (int)Math.Ceiling((decimal)epCount / 10);
            return View(episodes);
        }

        public int EpisodeCount() //gets total number of episodes
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(_context.Podcasts.FirstOrDefault().Feed);
            XmlElement root = doc.DocumentElement;
            XmlNodeList nodes = root.SelectNodes("channel/item");
            return nodes.Count;
        }

        [Route("about")]
        public IActionResult About() //feed podcast links into view
        {
            if(_context.Podcasts.Count() == 0) return View("NoFeed");
            Podcast thisPodcast = _context.Podcasts.FirstOrDefault();
            ViewBag.Title = thisPodcast.Title;
            ViewBag.Description = thisPodcast.Description;
            return View(_context.PodcastLinks.ToList());
        }

    }
}
