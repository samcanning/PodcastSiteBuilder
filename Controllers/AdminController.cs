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
            if(HttpContext.Session.GetString("admin") != "true")
            {
                return RedirectToAction("AdminLogin");
            }
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
                if(thisAdmin.Password == model.Password)
                {
                    System.Console.WriteLine("matches!");
                    return RedirectToAction("Admin");
                }
            }
            return View("AdminLogin");
        }
    }
}