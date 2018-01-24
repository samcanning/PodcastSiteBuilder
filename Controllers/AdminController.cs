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
            if(NotHead()) ViewBag.Head = false;
            else ViewBag.Head = true;
            return View();
        }

        [Route("admin")]
        public IActionResult AdminLogin()
        {
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
            if(NotLogged() || NotHead()) return RedirectToAction("AdminLogin");
            return View();
        }

        [HttpPost]
        [Route("admin/register/error")]
        public IActionResult Register(AdminRegister model)
        {
            if(NotLogged() || NotHead()) return RedirectToAction("AdminLogin");
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

        public bool NotLogged()
        {
            if(HttpContext.Session.GetString("admin") == "true") return false;
            return true;
        }

        public bool NotHead()
        {
            if(_context.Admins.SingleOrDefault(a => a.Username == HttpContext.Session.GetString("user")).Head == 1) return false;
            return true;
        }

        [Route("test")]
        public IActionResult Test()
        {
            Podcast newPodcast = new Podcast();
            newPodcast.Feed = "http://juergenit.libsyn.com/rss";
            newPodcast.Title = "Juergen' It";
            _context.Podcasts.Add(newPodcast);
            _context.SaveChanges();
            return RedirectToAction("Index", "Home");
        }
    }
}