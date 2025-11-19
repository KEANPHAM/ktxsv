using KTXSV.Models;
using KTXSV.Services;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace KTXSV.Controllers
{
    public class AdminController : Controller
    {

        private readonly AdminNotificationService _adminNotificationService;
        private readonly StudentNotificationService _studentNotificationService;

        public AdminController()
        {
            _adminNotificationService = new AdminNotificationService(new KTXSVEntities());
            _studentNotificationService = new StudentNotificationService(new KTXSVEntities());
        }



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

            var bed = db.Beds.Find(reg.BedID);
            bed.Booking = false;   
            bed.IsOccupied = true;  
            db.Entry(bed).State = EntityState.Modified;
            db.SaveChanges();

            var room = db.Rooms
                .Include("Beds")
                .FirstOrDefault(r => r.RoomID == reg.RoomID);

            if (room != null)
            {
                room.Occupied = room.Beds.Count(b => b.IsOccupied == true);

                room.Status = (room.Occupied < room.Capacity) ? "Available" : "Full";
            }
            var newPayment = new Payment
            {
                RegID = reg.RegID,
                Amount = reg.Room.Price, 
                PaymentDate = DateTime.Today,
                Type = "Rent",
                Status = "Unpaid"
            };
            db.Payments.Add(newPayment);
            db.SaveChanges();
            _studentNotificationService.SendStudentNotification(reg.UserID, reg.RegID, "Approved", reg);

            // Gửi thông báo cho admin
            _adminNotificationService.SendAdminNotification("Approved", reg);

            TempData["Message"] = "Phê duyệt thành công!";
            return RedirectToAction("PendingRegistrations");
        }

        // Từ chối đăng ký
        public ActionResult Reject(int id)
        {
            var reg = db.Registrations.Include(r => r.User).Include(r => r.Room).Include(r => r.Bed).FirstOrDefault(r => r.RegID == id);
            if (reg == null) return HttpNotFound();

            reg.Status = "Rejected";
            db.SaveChanges();

            _adminNotificationService.SendAdminNotification( "Rejected", reg);

            TempData["Message"] = "Đã từ chối đăng ký và gửi thông báo đến sinh viên";
            return RedirectToAction("PendingRegistrations");
        }
        public ActionResult Notifications()
        {
            // Lấy tất cả thông báo dành cho Admin hoặc All
            var notifications = db.Notifications
                                  .Where(n => n.TargetRole == "Admin" || n.TargetRole == "All")
                                  .OrderByDescending(n => n.CreatedAt)
                                  .ToList();

            return View(notifications);
        }

        // POST: Admin/MarkAsRead/5
        [HttpPost]
        public ActionResult MarkAsRead(int id)
        {
            var noti = db.Notifications.Find(id);
            if (noti != null && !noti.IsRead)
            {
                noti.IsRead = true;
                db.SaveChanges(); // Cập nhật trực tiếp vào database
            }
            return new EmptyResult(); // Không trả view, chỉ dùng để Ajax
        }

        // GET: Admin/Notifications/ChiTiet/5
        public ActionResult ChiTiet(int id)
        {
            var noti = db.Notifications.Find(id);
            if (noti == null) return HttpNotFound();

            // Nếu chưa đọc, đánh dấu đã đọc
            if (!noti.IsRead)
            {
                noti.IsRead = true;
                db.SaveChanges();
            }

            return View(noti);
        }


    }
}
