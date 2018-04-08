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
using Amazon.S3.Transfer;
using System.IO;

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

        [Route("admin/hosts/edit")]
        public IActionResult EditHosts()
        {
            if(NotLogged()) return RedirectToAction("AdminLogin");
            List<Host> hosts = _context.Hosts.ToList();
            return View(hosts);
        }

        [Route("admin/hosts/add")]
        public IActionResult AddHost()
        {
            if(NotLogged()) return RedirectToAction("AdminLogin");
            return View();
        }

        [HttpPost]
        [Route("admin/hosts/add/submit")]
        public IActionResult CreateHost(Host model, IFormFile file)
        {
            if(model.name == null)
            {
                ModelState.AddModelError("name", "No name entered.");
                return View("AddHost");
            }
            Host newHost = new Host(){name = model.name};
            if(model.bio != null) newHost.bio = model.bio;
            if(file != null)
            {
                TransferUtility transfer = new TransferUtility(Credentials.AccessKey, Credentials.SecretKey, Amazon.RegionEndpoint.USWest2);
                using(var stream = new MemoryStream())
                {
                    string key = null;
                    while(_context.Hosts.FirstOrDefault(h => h.image == key) != null) key = GenerateKey();
                    file.CopyTo(stream);
                    transfer.Upload(stream, "dhcimages", key);
                }
            }
            _context.Add(newHost);
            _context.SaveChanges();        
            return RedirectToAction("Admin");
        }

        [Route("admin/hosts/{id}")]
        public IActionResult Host(int id)
        {
            if(NotLogged()) return RedirectToAction("AdminLogin");
            Host thisHost = _context.Hosts.SingleOrDefault(h => h.id == id);
            if(thisHost == null) return RedirectToAction("EditHosts");
            HostDisplay display = new HostDisplay()
            {
                id = id,
                name = thisHost.name,
                bio = thisHost.bio,
                image = thisHost.image,
                links = _context.Links.Where(l => l.host_id == thisHost.id).ToList()
            };
            return View(display);
        }
        
        [Route("admin/hosts/{id}/removeimage")]
        public IActionResult RemoveImage(int id)
        {
            if(NotLogged()) return RedirectToAction("AdminLogin");
            Host thisHost = _context.Hosts.SingleOrDefault(h => h.id == id);
            if(thisHost == null) return RedirectToAction("Admin");
            thisHost.image = null;
            _context.Update(thisHost);
            _context.SaveChanges();
            return RedirectToAction("Host", new {id = id});
        }

        [HttpPost]
        public IActionResult AddImage(int id, IFormFile file)
        {
            TransferUtility transfer = new TransferUtility(Credentials.AccessKey, Credentials.SecretKey, Amazon.RegionEndpoint.USWest2);
            Host thisHost = _context.Hosts.SingleOrDefault(h => h.id == id);
            using(var stream = new MemoryStream())
            {
                string key = null;
                while(_context.Hosts.FirstOrDefault(h => h.image == key) != null) key = GenerateKey();
                file.CopyTo(stream);
                transfer.Upload(stream, "dhcimages", key);
                thisHost.image = key;
            }
            _context.Update(thisHost);
            _context.SaveChanges();
            return RedirectToAction("Host", new {id = id});
        }

        [Route("admin/hosts/delete")]
        public IActionResult DeleteHost()
        {
            return View(_context.Hosts.ToList());
        }

        [Route("admin/hosts/delete/{id}")]
        public IActionResult SubmitDeleteHost(int id)
        {
            if(NotLogged()) return RedirectToAction("AdminLogin");
            Host toDelete = _context.Hosts.SingleOrDefault(h => h.id == id);
            if(toDelete == null) return RedirectToAction("DeleteHost");
            List<Link> hostLinks = _context.Links.Where(l => l.host_id == id).ToList();
            foreach(Link l in hostLinks)
            {
                _context.Remove(l);
            }
            _context.SaveChanges();
            _context.Remove(toDelete);
            _context.SaveChanges();
            return RedirectToAction("Admin");
        }

        [Route("admin/hosts/{id}/addlink")]
        public IActionResult AddLink(int id)
        {
            if(NotLogged()) return RedirectToAction("AdminLogin");
            Host thisHost = _context.Hosts.SingleOrDefault(h => h.id == id);
            if(thisHost == null) return RedirectToAction("EditHosts");
            ViewBag.id = id;
            ViewBag.name = thisHost.name;
            return View();
        }

        [HttpPost]
        [Route("admin/hosts/{id}/addlink/submit")]
        public IActionResult CreateLink(int id, string url, string site)
        {
            if(NotLogged()) return RedirectToAction("AdminLogin");
            Host thisHost = _context.Hosts.SingleOrDefault(h => h.id == id);
            if(thisHost == null) return RedirectToAction("EditHosts");
            ViewBag.name = thisHost.name;
            ViewBag.id = id;
            if(url == null) ModelState.AddModelError("url", "Must enter a URL.");
            if(site == null) ModelState.AddModelError("site", "Must enter a title for your link.");
            if(!ModelState.IsValid) return View("AddLink");
            Link newLink = new Link(){
                url = url,
                site = site,
                host_id = id
            };
            _context.Add(newLink);
            _context.SaveChanges();
            return RedirectToAction("Host", new {id = id});
        }

        [Route("admin/hosts/{hostid}/editlink/{linkid}")]
        public IActionResult EditLink(int hostid, int linkid)
        {
            if(NotLogged()) return RedirectToAction("AdminLogin");
            Host thisHost = _context.Hosts.SingleOrDefault(h => h.id == hostid);
            Link thisLink = _context.Links.SingleOrDefault(l => l.id == linkid);
            if(thisLink == null || thisHost == null) return RedirectToAction("EditHosts");
            ViewBag.name = thisHost.name;
            ViewBag.id = hostid;
            return View(thisLink);
        }

        [HttpPost]
        [Route("admin/hosts/{hostid}/editlink/{linkid}/submit_url")]
        public IActionResult EditLinkURL(Link model, int hostid, int linkid, string site)
        {
            if(NotLogged()) return RedirectToAction("AdminLogin");
            Host thisHost = _context.Hosts.SingleOrDefault(h => h.id == hostid);
            Link thisLink = _context.Links.SingleOrDefault(l => l.id == linkid);
            if(thisLink == null || thisHost == null) return RedirectToAction("EditHosts");
            ViewBag.id = hostid;
            ViewBag.name = thisHost.name;
            if(model.url == null)
            {
                ModelState.AddModelError("url", "URL cannot be empty.");
                thisLink.site = site;
                return View("EditLink", thisLink);
            }
            thisLink.url = model.url;
            _context.Update(thisLink);
            _context.SaveChanges();
            return RedirectToAction("Host", new {id = hostid});
        }

        [HttpPost]
        [Route("admin/hosts/{hostid}/editlink/{linkid}/submit_title")]
        public IActionResult EditLinkTitle(Link model, int hostid, int linkid, string url)
        {
            if(NotLogged()) return RedirectToAction("AdminLogin");
            Host thisHost = _context.Hosts.SingleOrDefault(h => h.id == hostid);
            Link thisLink = _context.Links.SingleOrDefault(l => l.id == linkid);
            if(thisLink == null || thisHost == null) return RedirectToAction("EditHosts");
            ViewBag.id = hostid;
            ViewBag.name = thisHost.name;
            if(model.site == null)
            {
                ModelState.AddModelError("site", "Title cannot be empty.");
                thisLink.url = url;
                return View("EditLink", thisLink);
            }
            thisLink.site = model.site;
            _context.Update(thisLink);
            _context.SaveChanges();
            return RedirectToAction("Host", new {id = hostid});
        }

        // [Route("admin/hosts/{hostid}/removelink/{linkid}")]
        // public IActionResult RemoveLink(int hostid, int linkid)
        // {
        //     if(NotLogged()) return RedirectToAction("AdminLogin");
        //     Host thisHost = _context.Hosts.SingleOrDefault(h => h.id == hostid);
        //     Link thisLink = _context.Links.SingleOrDefault(l => l.id == linkid);
        //     if(thisLink == null || thisHost == null) return RedirectToAction("EditHosts");
        //     _context.Remove(thisLink);
        //     _context.SaveChanges();
        //     return RedirectToAction("Host", new {id = hostid});
        // }

        public bool NotLogged()
        {
            if(HttpContext.Session.GetString("admin") == "true") return false;
            return true;
        }

        public string GenerateKey()
        {
            Random random = new Random();
            string chars = "qwertyuiopasdfghjklzxcvbnm1234567890";
            string key = "";
            for(int i = 0; i < 16; i++)
            {
                key += chars[random.Next(chars.Length)];
            }
            return key;
        }

    }
}