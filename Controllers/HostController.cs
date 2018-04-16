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
    public class HostController : Controller
    {

        private AdminContext _context;

        public HostController(AdminContext context)
        {
            _context = context;
        }

        [Route("admin/hosts/{id}")]
        public IActionResult Host(int id)
        {
            if(NotLogged()) return RedirectToAction("AdminLogin", "Admin");
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

        [Route("admin/hosts/edit")]
        public IActionResult EditHosts()
        {
            if(NotLogged()) return RedirectToAction("AdminLogin", "Admin");
            List<Host> hosts = _context.Hosts.ToList();
            return View(hosts);
        }

        [Route("admin/hosts/add")]
        public IActionResult AddHost()
        {
            if(NotLogged()) return RedirectToAction("AdminLogin", "Admin");
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
                    newHost.image = key;
                }
            }
            _context.Add(newHost);
            _context.SaveChanges();        
            return RedirectToAction("Admin", "Admin");
        }

        [HttpPost]
        public IActionResult AddImage(int id, IFormFile file)
        {
            TransferUtility transfer = new TransferUtility(Credentials.AccessKey, Credentials.SecretKey, Amazon.RegionEndpoint.USWest2);
            Host thisHost = _context.Hosts.SingleOrDefault(h => h.id == id);
            if(thisHost.image != null)
            {
                using(Amazon.S3.AmazonS3Client client = new Amazon.S3.AmazonS3Client(Credentials.AccessKey, Credentials.SecretKey, Amazon.RegionEndpoint.USWest2))
                {
                    client.DeleteObjectAsync("dhcimages", thisHost.image);
                }                
            }
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
            if(NotLogged()) return RedirectToAction("AdminLogin", "Admin");
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
            return RedirectToAction("Admin", "Admin");
        }

        [Route("admin/hosts/{hostid}/removelink/{linkid}")]
        public IActionResult RemoveLink(int hostid, int linkid)
        {
            if(NotLogged()) return RedirectToAction("AdminLogin", "Admin");
            Host thisHost = _context.Hosts.SingleOrDefault(h => h.id == hostid);
            Link thisLink = _context.Links.SingleOrDefault(l => l.id == linkid);
            if(thisLink == null || thisHost == null) return RedirectToAction("EditHosts");
            _context.Remove(thisLink);
            _context.SaveChanges();
            return RedirectToAction("Host", new {id = hostid});
        }

        [HttpPost]
        public IActionResult EditName(int id, string name)
        {
            Host thisHost = _context.Hosts.SingleOrDefault(h => h.id == id);
            thisHost.name = name;
            _context.Update(thisHost);
            _context.SaveChanges();
            return RedirectToAction("Host", new {id = id});
        }

        [HttpPost]
        public IActionResult EditBio(int id, string bio)
        {
            Host thisHost = _context.Hosts.SingleOrDefault(h => h.id == id);
            thisHost.bio = bio;
            _context.Update(thisHost);
            _context.SaveChanges();
            return RedirectToAction("Host", new {id = id});
        }

        [Route("admin/hosts/{id}/addlink")]
        public IActionResult AddLink(int id)
        {
            if(NotLogged()) return RedirectToAction("AdminLogin", "Admin");
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
            if(NotLogged()) return RedirectToAction("AdminLogin", "Admin");
            Host thisHost = _context.Hosts.SingleOrDefault(h => h.id == id);
            if(thisHost == null) return RedirectToAction("EditHosts");
            ViewBag.name = thisHost.name;
            ViewBag.id = id;
            if(url == null) ModelState.AddModelError("url", "Must enter a URL.");
            if(site == null) ModelState.AddModelError("site", "Must enter a title for your link.");
            if(!ModelState.IsValid) return View("AddLink");
            try
            {
                if(url.Substring(0, 7) != "http://" && url.Substring(0, 8) != "https://") url = "http://" + url;
            } catch{ url = "http://" + url; } 
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
            if(NotLogged()) return RedirectToAction("AdminLogin", "Admin");
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
            if(NotLogged()) return RedirectToAction("AdminLogin", "Admin");
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
            if(NotLogged()) return RedirectToAction("AdminLogin", "Admin");
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

        [Route("admin/hosts/{id}/removeimage")]
        public IActionResult RemoveImage(int id)
        {
            if(NotLogged()) return RedirectToAction("AdminLogin", "Admin");
            Host thisHost = _context.Hosts.SingleOrDefault(h => h.id == id);
            if(thisHost == null) return RedirectToAction("Admin", "Admin");
            Amazon.S3.AmazonS3Client client = new Amazon.S3.AmazonS3Client(Credentials.AccessKey, Credentials.SecretKey, Amazon.RegionEndpoint.USWest2);
            client.DeleteObjectAsync("dhcimages", thisHost.image);
            thisHost.image = null;
            _context.Update(thisHost);
            _context.SaveChanges();
            return RedirectToAction("Host", new {id = id});
        }

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