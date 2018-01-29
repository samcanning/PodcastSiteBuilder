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
    public class HostController : Controller
    {

        private AdminContext _context;

        public HostController(AdminContext context)
        {
            _context = context;
        }

        [HttpPost]
        public IActionResult EditName(int id, string name)
        {
            Host thisHost = _context.Hosts.SingleOrDefault(h => h.id == id);
            thisHost.name = name;
            _context.Update(thisHost);
            _context.SaveChanges();
            return RedirectToAction("Host", "Admin", new {id = id});
        }

        [HttpPost]
        public IActionResult EditBio(int id, string bio)
        {
            Host thisHost = _context.Hosts.SingleOrDefault(h => h.id == id);
            thisHost.bio = bio;
            _context.Update(thisHost);
            _context.SaveChanges();
            return RedirectToAction("Host", "Admin", new {id = id});
        }

        [Route("admin/hosts/{hostId}/removelink/{linkId}")]
        public IActionResult RemoveLink(int hostId, int linkId)
        {
            if(HttpContext.Session.GetString("admin") != "true") return RedirectToAction("AdminLogin", "Admin");
            Link toRemove = _context.Links.SingleOrDefault(l => l.id == linkId);
            _context.Remove(toRemove);
            _context.SaveChanges();
            return RedirectToAction("Host", "Admin", new {id = hostId});
        }

        //need AddLink function!!

        //need AddHost function!!

    }
}