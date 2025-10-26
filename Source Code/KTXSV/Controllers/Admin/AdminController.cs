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

        // Phê duyệt đăng ký
        public ActionResult Approve(int id)
        {
            var reg = db.Registrations.Find(id);
            if (reg != null)
            {
                reg.Status = "Active"; // Hoặc "Approved" tùy logic

                var room = db.Rooms.Find(reg.RoomID);
                if (room != null)
                {
                    room.Occupied += 1;
                    room.Status = room.Occupied < room.Capacity ? "Available" : "Full";
                }

                db.SaveChanges();
                TempData["Message"] = "Phê duyệt thành công!";
            }
            else
            {
                TempData["Message"] = "Đăng ký không tồn tại!";
            }

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

            return RedirectToAction("PendingRegistrations");
        }


    }
}
