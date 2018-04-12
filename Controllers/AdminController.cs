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
            if(HttpContext.Session.GetString("head") == "true") ViewBag.Head = true;
            else ViewBag.Head = false;
            if(_context.Admins.Count() > 1) ViewBag.toDelete = true;
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
            Admin thisAdmin = _context.Admins.SingleOrDefault(a => a.Username == model.Username);
            if(ModelState.IsValid)
            {                
                if(thisAdmin == null)
                {
                    ModelState.AddModelError("Username", "No admin found with this username.");
                    return View("AdminLogin");
                }

                PasswordHasher<Admin> hasher = new PasswordHasher<Admin>();
                if(hasher.VerifyHashedPassword(thisAdmin, thisAdmin.Password, model.Password) != 0)
                {
                    HttpContext.Session.SetString("admin", "true");
                    if(thisAdmin.Head == 1) HttpContext.Session.SetString("head", "true");
                    HttpContext.Session.SetString("username", thisAdmin.Username);
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
        [Route("admin/register/submit")]
        public IActionResult Register(AdminRegister model)
        {
            if(ModelState.IsValid)
            {
                if(model.Username == "default")
                {
                    ModelState.AddModelError("Username", "Username cannot be \"default\".");
                    return View("AdminRegistration");
                }
                PasswordHasher<Admin> hasher = new PasswordHasher<Admin>();
                Admin newAdmin = new Admin(){
                    Username = model.Username,
                    Head = 1
                    };
                newAdmin.Password = hasher.HashPassword(newAdmin, model.Password);
                _context.Add(newAdmin);
                _context.SaveChanges();
                HttpContext.Session.SetString("head", "true");
                HttpContext.Session.SetString("username", newAdmin.Username);
                return RedirectToAction("Admin");
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

        [Route("admin/description")]
        public IActionResult Description()
        {
            if(NotLogged()) return RedirectToAction("AdminLogin");
            ViewBag.desc = _context.Podcasts.FirstOrDefault().Description;
            return View();
        }

        [HttpPost]
        public IActionResult UpdateDescription(string description)
        {
            Podcast podcast = _context.Podcasts.FirstOrDefault();
            podcast.Description = description;
            _context.Update(podcast);
            _context.SaveChanges();
            return RedirectToAction("Description");
        }

        [Route("admin/add")]
        public IActionResult AddAdmin()
        {
            if(NotLogged() || NotHead()) return RedirectToAction("AdminLogin");
            return View();
        }

        [HttpPost]
        [Route("admin/add/submit")]
        public IActionResult CreateAdmin(AdminRegister model)
        {
            if(NotLogged() || NotHead()) return RedirectToAction("AdminLogin");
            Admin existingAdmin = _context.Admins.SingleOrDefault(a => a.Username == model.Username);
            if(existingAdmin != null) ModelState.AddModelError("Username", "This username is already in use.");
            if(!ModelState.IsValid) return View("AddAdmin");
            PasswordHasher<Admin> hasher = new PasswordHasher<Admin>();
            Admin newAdmin = new Admin(){Username = model.Username};
            newAdmin.Password = hasher.HashPassword(newAdmin, model.Password);
            _context.Add(newAdmin);
            _context.SaveChanges();
            return RedirectToAction("AdminList");
        }

        [Route("admin/delete")]
        public IActionResult DeleteAdmin()
        {
            if(NotLogged() || NotHead()) return RedirectToAction("AdminLogin");
            List<Admin> admins = _context.Admins.Where(u => u.Head != 1).ToList();
            if(admins.Count == 0) return RedirectToAction("Admin");
            return View(admins);
        }

        [HttpPost]
        [Route("admin/delete/submit")]
        public IActionResult SubmitDeleteAdmin(string password, int toDelete)
        {
            if(NotLogged() || NotHead()) return RedirectToAction("AdminLogin");
            Admin thisAdmin = _context.Admins.SingleOrDefault(a => a.Head == 1);
            if(thisAdmin == null) return RedirectToAction("AdminLogin");
            List<Admin> admins = _context.Admins.Where(u => u.Head != 1).ToList();
            if(password == null) return View("DeleteAdmin", admins);
            PasswordHasher<Admin> hasher = new PasswordHasher<Admin>();
            if(hasher.VerifyHashedPassword(thisAdmin, thisAdmin.Password, password) == 0) return View("DeleteAdmin", admins);
            Admin adminToDelete = _context.Admins.SingleOrDefault(a => a.id == toDelete);
            if(adminToDelete == null) return RedirectToAction("DeleteAdmin", admins);
            _context.Remove(adminToDelete);
            _context.SaveChanges();
            return RedirectToAction("AdminList");
        }

        [Route("admin/list")]
        public IActionResult AdminList()
        {
            if(NotLogged() || NotHead()) return RedirectToAction("AdminLogin");
            return View(_context.Admins.ToList());
        }

        [Route("admin/changepassword")]
        public IActionResult ChangePassword()
        {
            if(NotLogged()) return RedirectToAction("AdminLogin");
            return View();
        }

        [HttpPost]
        [Route("admin/changepassword/submit")]
        public IActionResult SubmitPWChange(NewPassword model)
        {
            if(NotLogged()) return RedirectToAction("AdminLogin");
            if(!ModelState.IsValid) return View("ChangePassword", model);
            Admin thisAdmin = _context.Admins.SingleOrDefault(a => a.Username == HttpContext.Session.GetString("username"));
            if(thisAdmin == null) return RedirectToAction("Logout");
            PasswordHasher<Admin> hasher = new PasswordHasher<Admin>();
            if(hasher.VerifyHashedPassword(thisAdmin, thisAdmin.Password, model.oldpw) == 0)
            {
                ModelState.AddModelError("oldpw", "Incorrect password.");
                return View("ChangePassword", model);
            }
            thisAdmin.Password = hasher.HashPassword(thisAdmin, model.newpw);
            _context.Update(thisAdmin);
            _context.SaveChanges();
            return RedirectToAction("Admin");
        }

        public bool NotLogged()
        {
            if(HttpContext.Session.GetString("admin") == "true") return false;
            return true;
        }

        public bool NotHead()
        {
            if(HttpContext.Session.GetString("head") == "true") return false;
            return true;
        }

        [Route("logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Admin");
        }

    }
}