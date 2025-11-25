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
        KTXSVEntities db = new KTXSVEntities();
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            if (Session["UserID"] == null || !int.TryParse(Session["UserID"].ToString(), out int userId))
            {
                filterContext.Result = RedirectToAction("LoginStudent", "Account");
                return;
            }

            var user = db.Users.Find(userId);
            if (user == null)
            {
                filterContext.Result = RedirectToAction("LoginStudent", "Account");
                return;
            }

            ViewBag.Username = user.Username;
            ViewBag.FullName = user.FullName;
            ViewBag.Email = user.Email;
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SendNotification(int userId, int regId, string title, string content)
        {
            var notification = new Notification
            {
                UserID = userId,
                RegID = regId,
                Title = title,
                Content = content,
                TargetRole = "Student",
                IsRead = false,
                CreatedAt = DateTime.Now,
                Url = ""
            };

            db.Notifications.Add(notification);
            db.SaveChanges();
            notification.Url = $"/Phong/ViewNotification/{notification.NotiID}";
            db.SaveChanges();

            TempData["Success"] = "Đã gửi thông báo đến sinh viên!";
            return Redirect(Request.UrlReferrer.ToString());
        }

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
            using (var db = new KTXSVEntities())
            {
                int currentUserId = Convert.ToInt32(Session["UserID"]);

                var notifications = db.Notifications
                                      .Where(n => n.TargetRole == "Admin" || n.UserID == currentUserId)
                                      .OrderByDescending(n => n.CreatedAt)
                                      .ToList();

                ViewBag.StudentList = db.Users
                                        .Where(u => u.Role == "Student")
                                        .ToList();

                return View(notifications);
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateNotification(string Title, string Content, string TargetType, int? UserID)
        {
            using (var db = new KTXSVEntities())
            {
                if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Content))
                {
                    TempData["error"] = "Tiêu đề và nội dung không được để trống!";
                    return RedirectToAction("Notifications");
                }

                if (TargetType == "All")
                {
                    var students = db.Users.Where(u => u.Role == "Student").ToList();
                    foreach (var s in students)
                    {
                        db.Notifications.Add(new Notification
                        {
                            Title = Title,
                            Content = Content,
                            UserID = s.UserID,
                            TargetRole = "Student",
                            CreatedAt = DateTime.Now,
                            IsRead = false
                        });
                    }
                }
                else if (TargetType == "One" && UserID.HasValue)
                {
                    var user = db.Users.Find(UserID.Value);
                    if (user != null)
                    {
                        db.Notifications.Add(new Notification
                        {
                            Title = Title,
                            Content = Content,
                            UserID = user.UserID,
                            TargetRole = "Student",
                            CreatedAt = DateTime.Now,
                            IsRead = false
                        });
                    }
                }

                db.SaveChanges();
            }

            TempData["success"] = "Gửi thông báo thành công!";
            return RedirectToAction("Notifications");
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
