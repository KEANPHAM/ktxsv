using KTXSV.Models;
//using KTXSV.Services;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.EnterpriseServices;
using System.Linq;
using System.Web.Mvc;

namespace KTXSV.Controllers
{
    public class PhongController : Controller
    {
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

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult HoSo()
        {
            return View();
        }
        public ActionResult GiaHanPhong(int regId)
        {
            var registration = db.Registrations
                                 .Include(r => r.Room)
                                 .FirstOrDefault(r => r.RegID == regId);

            if (registration == null)
                return HttpNotFound();

            return View(registration);
        }

        [HttpPost]
        public ActionResult GiaHanPhong(int regId, DateTime newEndDate)
        {
            var registration = db.Registrations
                                 .Include(r => r.Room)
                                 .Include(r => r.User)
                                 .FirstOrDefault(r => r.RegID == regId);

            if (registration == null)
                return HttpNotFound();

            // Kiểm tra giường đã bị người khác đăng ký chưa
            var bed = db.Beds.Find(registration.BedID);
            if (bed == null || (bed.IsOccupied.GetValueOrDefault() && registration.Status != "Expiring"))
            {
                TempData["Error"] = "Giường đã bị người khác đăng ký. Không thể gia hạn.";
                return RedirectToAction("DanhSachDangKy");
            }

            // Cập nhật ngày kết thúc
            registration.EndDate = newEndDate;
            registration.Status = "Active";
            db.SaveChanges();

            var noti = new Notification
            {
                UserID = registration.UserID,
                Title = "Gia hạn phòng thành công",
                Content = $"Bạn đã gia hạn phòng {registration.Room.RoomNumber} đến {newEndDate:dd/MM/yyyy}.Vui lòng thanh toán trước ngày <strong>{registration.StartDate.AddMonths(-1):dd/MM/yyyy}</strong>.",
                CreatedAt = DateTime.Now,
                TargetRole = "Student",
                IsRead = false,
                RegID = registration.RegID,
                Url = "/Phong/DanhSachPhong"
            };
            db.Notifications.Add(noti);
            db.SaveChanges();
            // Tạo hóa đơn mới cho kỳ gia hạn
            var newPayment = new Payment
            {
                RegID = registration.RegID,
                Amount = registration.Room.Price, // giả sử 1 tháng
                Type = "Rent",
                PaymentDate = DateTime.Today,
                Status = "Unpaid"
            };
            db.Payments.Add(newPayment);

            db.SaveChanges();

            TempData["Success"] = "Gia hạn phòng thành công! Hóa đơn mới đã được tạo.";
            return RedirectToAction("DanhSachDangKy");
        }


        public ActionResult DangKyPhong(string gender, string building, int? capacity)
        {

            var userId = int.Parse(Session["UserID"].ToString());

            var uploadedTypes = db.StudentFiles
                .Where(f => f.UserID == userId)
                .Select(f => f.FileType)
                .ToList();

            var requiredFiles = new List<string> { "CCCD", "BHYT", "StudentCard", "Portrait" };
            bool isComplete = requiredFiles.All(t => uploadedTypes.Contains(t));

            if (!isComplete)
            {
                TempData["Error"] = "Vui lòng hoàn tất hồ sơ trước khi đăng ký phòng";
                return RedirectToAction("Index", "StudentFiles");
            }

            var dangKyHienTai = db.Registrations
                .FirstOrDefault(r => r.UserID == userId && (r.Status == "Pending" || r.Status == "Active"));
            ViewBag.DaDangKy = dangKyHienTai != null;

            var phongTrong = db.Rooms.Include(r => r.Beds).AsQueryable();
            if (!string.IsNullOrEmpty(gender))
                phongTrong = phongTrong.Where(p => p.Gender == gender);
            if (!string.IsNullOrEmpty(building))
                phongTrong = phongTrong.Where(p => p.Building == building);
            if (capacity.HasValue)
                phongTrong = phongTrong.Where(p => p.Capacity == capacity.Value);

            var pendingBedIds = db.Registrations
                .Where(r => r.Status == "Pending" || r.Status == "Active")
                .Select(r => r.BedID.Value)
                .ToList();

            ViewBag.PendingBedIds = pendingBedIds;

            return View(phongTrong.ToList());
        }

        [HttpPost]
        public ActionResult DangKyPhong(int roomId, int bedId, int ContractDuration, int StartMonth)
        {
            int userId = int.Parse(Session["UserID"].ToString());

            int year = DateTime.Now.Year;
            if (StartMonth < DateTime.Now.Month)
                year += 1;

            DateTime startDate = new DateTime(year, StartMonth, 1);
            DateTime endDate = startDate.AddMonths(ContractDuration).AddDays(-1);

            var uploadedTypes = db.StudentFiles
                .Where(f => f.UserID == userId)
                .Select(f => f.FileType.Trim())
                .ToList();

            var requiredFiles = new List<string> { "CCCD", "BHYT", "StudentCard", "Portrait" };

            if (!requiredFiles.All(t => uploadedTypes.Contains(t)))
            {
                TempData["Error"] = "Vui lòng hoàn tất hồ sơ trước khi đăng ký phòng";
                return RedirectToAction("Index", "StudentFiles");
            }

            bool daDangKy = db.Registrations.Any(r => r.UserID == userId &&
                (r.Status == "Active" || r.Status == "Pending"));
            if (daDangKy)
            {
                TempData["Error"] = "Bạn đã có phòng hoặc đang chờ duyệt. Không thể đăng ký thêm.";
                return RedirectToAction("DangKyPhong");
            }

            var phong = db.Rooms.Find(roomId);
            var bed = db.Beds.FirstOrDefault(b => b.BedID == bedId &&
                (!db.Registrations.Any(r => r.BedID == b.BedID && (r.Status == "Pending" || r.Status == "Active"))
                 || b.Booking == true));
            if (phong != null && phong.Status == "Available" && bed != null)
            {
                var dangKyMoi = new Registration
                {
                    UserID = userId,
                    RoomID = roomId,
                    BedID = bed.BedID,
                    StartDate = startDate,
                    EndDate = endDate,
                    Status = "Pending"

                };
                db.Registrations.Add(dangKyMoi);
                db.SaveChanges();

                var thongBao = new Notification
                {
                    Title = "Đăng ký phòng thành công",
                    Content = $"Đăng ký phòng {dangKyMoi.Room.RoomNumber}, Tòa {dangKyMoi.Room.Building}, Giường {dangKyMoi.Bed.BedNumber} từ {startDate:dd/MM/yyyy} đến {endDate:dd/MM/yyyy}",
                    CreatedAt = DateTime.Now,
                    TargetRole = "Student"
                };
                db.Notifications.Add(thongBao);
                db.SaveChanges();


                if (phong.Occupied == phong.Capacity)
                {
                    phong.Status = "Full";
                }
                db.SaveChanges();
                TempData["Success"] = "Đăng ký phòng thành công. Vui lòng chờ duyệt.";
                return RedirectToAction("DanhSachPhong");

            }
            else
            {
                TempData["Error"] = "Phòng không tồn tại hoặc đã đầy.";
            }
            return RedirectToAction("DangKyPhong");

        }
        public ActionResult DanhSachPhong()
        {
            int userId = int.Parse(Session["UserID"].ToString());

            var dsDangKy = db.Registrations
                .Where(r => r.UserID == userId)
                .Include(r => r.Room)
                .Include(r => r.Bed)
                .Include(r => r.Payments) 
                .OrderByDescending(r => r.StartDate)
                .ToList();

            return View(dsDangKy);
        }
        [HttpPost]
        public ActionResult HuyDangKy(int? regId, int? bedId)
        {
            var reg = db.Registrations
                           .Include(r => r.Room)
                           .Include(r => r.Bed)
                           .FirstOrDefault(r => r.RegID == regId);

            if (reg == null || bedId == null)
            {
                TempData["Error"] = "Không tìm thấy đăng ký.";
                return RedirectToAction("DanhSachPhong");
            }

            if (reg.Status == "Pending" || reg.Status == "Active")
            {
                reg.Status = "Canceled";

                if (reg.Bed != null)
                {
                    reg.Bed.IsOccupied = false;

                }

                if (reg.Room != null)
                {
                    reg.Room.Occupied = Math.Max((reg.Room.Occupied ?? 1) - 1, 0);
                    if (reg.Room.Status == "Full" && reg.Room.Occupied < reg.Room.Capacity)
                        reg.Room.Status = "Available";
                }

                db.SaveChanges();
                TempData["Success"] = "Đã hủy đăng ký phòng và cập nhật giường thành công.";
            }
            else
            {
                TempData["Error"] = "Không thể hủy đăng ký đã bị từ chối hoặc không tồn tại.";
            }
            var thongBao = new Notification
            {
                UserID = reg.UserID,
                Title = "Hủy đăng ký phòng",
                Content = $"Hủy đăng ký phòng {reg.Room.RoomNumber}, Tòa {reg.Room.Building}, Giường  {reg.Bed.BedNumber}",
                CreatedAt = DateTime.Now,
                TargetRole = "Student",
                IsRead = false,
                RegID = reg.RegID,
                Url = "/Phong/DanhSachPhong"
            };
            db.Notifications.Add(thongBao);
            db.SaveChanges();

            return RedirectToAction("DanhSachPhong");
        }
        // Action xem thông báo
        public ActionResult ViewNotification(int id)
        {
            var noti = db.Notifications.Find(id);
            if (noti == null)
                return HttpNotFound();

            if (!noti.IsRead)
            {
                noti.IsRead = true;
                db.SaveChanges();
            }

            return View(noti);
        }

        public ActionResult Notifications()
        {
            int userId = int.Parse(Session["UserID"].ToString());
            var notifications = db.Notifications
                                  .Where(n => n.UserID == userId)
                                  .OrderByDescending(n => n.CreatedAt)
                                  .ToList();
            return View(notifications);
        }
        //private readonly NotificationService _notificationService;

        //public PhongController()
        //{
        //    _notificationService = new NotificationService(new KTXSVEntities());
        //}

    }
}
