using KTXSV.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace KTXSV.Controllers
{

    public class AdminController : Controller
    {
        private KTXSVEntities db = new KTXSVEntities();
        // GET: Admin
        //cho duyet

        public ActionResult PendingRegistrations()
        {
            Session["UserID"] = 7;
            Session["UserName"] = "Kiên Admin";
            Session["Role"] = "Admin";
            var pendingRegs = db.Registrations
                .Where(r => r.Status == "Pending")
                .Include(r => r.User)
                .Include(r => r.Room)
                .ToList();
            return View(pendingRegs);
        }
        //phe duyet
        public ActionResult Approve(int id)
        {
            var reg = db.Registrations.Find(id);
            if (reg != null)
            {

                reg.Status = "Active";

                var room = db.Rooms.Find(reg.RoomID);
                if (room != null)
                {
                    room.Occupied += 1;
                    room.Status = room.Occupied < room.Capacity ? "Available" : "Full";
                }

                db.SaveChanges();
            }
            return RedirectToAction("PendingRegistrations");
        }
        //tu choi
        public ActionResult Reject(int id)
        {
            var reg = db.Registrations.Find(id);

            if (reg != null)
            {
                reg.Status = "Rejected";
                db.SaveChanges();
            }
            return RedirectToAction("PendingRegistrations");
        }


    }
}