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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace PodcastSiteBuilder.Controllers
{
    public class AdminController : Controller
    {

        private AdminContext _context;

        public AdminController(AdminContext context)
        {
            _context = context;
        }

        [Route("admin/main")]
        public IActionResult Admin()
        {
            if(NotLogged()) return RedirectToAction("AdminLogin");
            if(HttpContext.Session.GetString("user") == "default")
            {
                return RedirectToAction("AdminRegistration");
            }
            else ViewBag.Head = true;
            return View();
        }

        [Route("admin")]
        public IActionResult AdminLogin()
        {
            if(_context.Admins.FirstOrDefault() == null)
            {
                return View("AdminRegistration");
            }
            if(!NotLogged()) return RedirectToAction("Admin");
            return View();
        }

        [HttpPost]
        [Route("admin/login")]
        public IActionResult Login(AdminLogin model)
        {
            if(ModelState.IsValid)
            {
                Admin thisAdmin = _context.Admins.SingleOrDefault(a => a.Username == model.Username);
                if(thisAdmin == null)
                {
                    ModelState.AddModelError("Username", "No admin found with this username.");
                    return View("AdminLogin");
                }

                PasswordHasher<Admin> hasher = new PasswordHasher<Admin>();
                if(hasher.VerifyHashedPassword(thisAdmin, thisAdmin.Password, model.Password) != 0)
                {
                    HttpContext.Session.SetString("admin", "true");
                    HttpContext.Session.SetString("user", thisAdmin.Username);
                    return RedirectToAction("Admin");
                }
                ModelState.AddModelError("Password", "Incorrect password.");
            }
            return View("AdminLogin");
        }

        [Route("admin/register")]
        public IActionResult AdminRegistration()
        {
            if(_context.Admins.FirstOrDefault() != null) return RedirectToAction("AdminLogin");
            return View();
        }

        [HttpPost]
        [Route("admin/register/error")]
        public IActionResult Register(AdminRegister model)
        {
            if(ModelState.IsValid)
            {
                if(model.Username == "default")
                {
                    ModelState.AddModelError("Username", "Username cannot be \"Default\".");
                    return View("AdminRegistration");
                }
                PasswordHasher<Admin> hasher = new PasswordHasher<Admin>();
                if(HttpContext.Session.GetString("user") == "default")
                {
                    Admin newAdmin = _context.Admins.SingleOrDefault(a => a.Username == "default");
                    newAdmin.Username = model.Username;
                    newAdmin.Password = hasher.HashPassword(newAdmin, model.Password);
                    _context.Update(newAdmin);
                    _context.SaveChanges();
                    HttpContext.Session.SetString("user", newAdmin.Username);
                    return RedirectToAction("Admin");
                }
                else
                {
                    Admin newAdmin = new Admin(){Username = model.Username};
                    newAdmin.Password = hasher.HashPassword(newAdmin, model.Password);
                    _context.Add(newAdmin);
                    _context.SaveChanges();
                    return RedirectToAction("Admin");
                }
            }
            return View("AdminRegistration");
        }

        [Route("admin/rss")]
        public IActionResult EditRSS()
        {
            if(NotLogged()) return RedirectToAction("AdminLogin");
            Podcast podcast = _context.Podcasts.FirstOrDefault();
            if(podcast != null) ViewBag.Feed = podcast.Feed;
            else ViewBag.Feed = null;
            return View();
        }

        [HttpPost]
        [Route("admin/rss/set")]
        public IActionResult SetRSS(NewFeed model)
        {
            if(model.url.Substring(0,7) != "http://") model.url = "http://" + model.url;
            if(VerifyRSS(model.url) == false)
            {
                ModelState.AddModelError("url", "Invalid feed URL.");
                ViewBag.Feed = _context.Podcasts.FirstOrDefault().Feed;
                return View("EditRSS");
            }
            Podcast podcast = _context.Podcasts.FirstOrDefault();
            bool updating = true;
            if(podcast == null)
            {
                podcast = new Podcast();
                updating = false;
            }
            podcast.Feed = model.url;
            XmlDocument doc = new XmlDocument();
            doc.Load(model.url);
            XmlElement root = doc.DocumentElement;
            podcast.Title = root.SelectSingleNode("channel/title").InnerText;
            if(updating) _context.Update(podcast);
            else _context.Add(podcast);
            _context.SaveChanges();
            return RedirectToAction("EditRSS");
        }

        public bool VerifyRSS(string url) //verifies that the feed URL is valid by attempting to parse one episode
        {           
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(url);
                XmlElement root = doc.DocumentElement;
                XmlNodeList nodes = root.SelectNodes("channel/item");
                List<RssData> rssFeedData = new List<RssData>();
                RssData episode = new RssData();
                try
                {
                    episode.Title = nodes[0].SelectSingleNode("title").InnerText;
                    episode.Image = nodes[0].SelectSingleNode("./*[name()='itunes:image']/@href").InnerText;
                    episode.Link = nodes[0].SelectSingleNode("link").InnerText;
                    episode.PubDate = nodes[0].SelectSingleNode("pubDate").InnerText.Substring(5, 11);
                    episode.Audio = nodes[0].SelectSingleNode("enclosure/@url").InnerText;
                    episode.AudioType = nodes[0].SelectSingleNode("enclosure/@type").InnerText;
                    try
                    {
                        episode.Description = nodes[0].SelectSingleNode("./*[name()='itunes:summary']").InnerText;
                    }
                    catch
                    {
                        try
                        {
                            episode.Description = nodes[0].SelectSingleNode("description").InnerText;
                        }
                        catch { }
                    }
                    
                }
                catch { }
            }
            catch { return false; }
            return true;     
        }

        public bool NotLogged()
        {
            if(HttpContext.Session.GetString("admin") == "true") return false;
            return true;
        }

    }
}