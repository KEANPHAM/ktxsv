using KTXSV.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace KTXSV.Controllers
{
    public class AdminController : Controller
    {
        private KTXSVEntities db = new KTXSVEntities();

        // GET: Admin/PendingRegistrations
        public ActionResult PendingRegistrations()
        {
            if (Session["UserID"] != null)
            {
                int currentUserId = Convert.ToInt32(Session["UserID"]);
                var currentUser = db.Users.Find(currentUserId);
                ViewBag.FullName = currentUser != null ? currentUser.FullName : "Admin";
            }
            else
            {
                ViewBag.FullName = "Admin";
            }




            // Load đăng ký Pending kèm thông tin User và Room
            var pendingRegs = db.Registrations
                .Where(r => r.Status == "Pending")
                .Include(r => r.User)
                .Include(r => r.Room)
                .ToList();

            return View(pendingRegs);
        }

        public ActionResult Approve(int id)
        {
            // Load registration
            var reg = db.Registrations
                .FirstOrDefault(r => r.RegID == id);

            if (reg == null)
                return HttpNotFound();

            reg.Status = "Active";

            var bed = db.Beds.FirstOrDefault(b => b.BedID == reg.BedID);

            if (bed != null)
            {
                bed.IsOccupied = true;
            }

            var room = db.Rooms
                .Include("Beds")
                .FirstOrDefault(r => r.RoomID == reg.RoomID);

            if (room != null)
            {
                room.Occupied = room.Beds.Count(b => b.IsOccupied == true);

                room.Status = (room.Occupied < room.Capacity) ? "Available" : "Full";
            }

            db.SaveChanges();

            TempData["Message"] = "Phê duyệt thành công!";
            return RedirectToAction("PendingRegistrations");
        }

        // Từ chối đăng ký
        public ActionResult Reject(int id)
        {
            var reg = db.Registrations.Find(id);
            if (reg != null)
            {
                reg.Status = "Rejected";
                db.SaveChanges();
                TempData["Message"] = "Đã từ chối đăng ký!";
            }
            else
            {
                TempData["Message"] = "Đăng ký không tồn tại!";
            }
            var bed = db.Beds.Find(reg.BedID);
           
            return RedirectToAction("PendingRegistrations");
        }


    }
}
