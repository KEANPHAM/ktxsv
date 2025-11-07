using KTXSV.Models;
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

            if (Session["UserID"] != null)
            {
                int userId;
                if (int.TryParse(Session["UserID"].ToString(), out userId))
                {
                    var user = db.Users.Find(userId);
                    if (user != null)
                    {
                        ViewBag.Username = user.Username;
                        ViewBag.FullName = user.FullName;
                        ViewBag.Email = user.Email;
                    }
                }
            }
        }

        public ActionResult Index()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("LoginStudent", "Account");
            }

            int userID;
            if (!int.TryParse(Session["UserID"].ToString(), out userID))
            {
                return RedirectToAction("LoginStudent", "Account");
            }

            var user = db.Users.Find(userID);
            if (user == null)
            {
                return RedirectToAction("LoginStudent", "Account");
            }

            ViewBag.Username = user.Username;
            ViewBag.FullName = user.FullName;
            ViewBag.Email = user.Email;

            return View();
        }

        public ActionResult HoSo()
        {
            return View();
        }
        public ActionResult DangKyPhong(string gender, string building, int? capacity)
        {
            
            int userId = int.Parse(Session["UserID"].ToString());

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

            var phongTrong = db.Rooms
                .Include(r => r.Beds)
                .AsQueryable();

            if (!string.IsNullOrEmpty(gender))
                phongTrong = phongTrong.Where(p => p.Gender == gender);

            if (!string.IsNullOrEmpty(building))
                phongTrong = phongTrong.Where(p => p.Building == building);

            if (capacity.HasValue)
                phongTrong = phongTrong.Where(p => p.Capacity == capacity.Value);

            return View(phongTrong.ToList());
        }

        // đăng ký phòng
        [HttpPost]
        public ActionResult DangKyPhong(int roomId, int bedId)
        {
            int userId = int.Parse(Session["UserID"].ToString());

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
            var bed = db.Beds.FirstOrDefault(b => b.BedID == bedId && (b.IsOccupied ?? false) == false);
            if (phong != null && phong.Status == "Available" && bed != null)
            {
                var dangKyMoi = new Registration
                {
                    UserID = userId,
                    RoomID = roomId,
                    StartDate = DateTime.Now,
                    Status = "Pending",
                    BedID = bed.BedID,

                };
                db.Registrations.Add(dangKyMoi);
                bed.IsOccupied = true;
                db.SaveChanges();




                var thanhToanMoi = new Payment
                {
                    RegID = dangKyMoi.RegID,
                    Amount = dangKyMoi.Room.Price,
                    PaymentDate = DateTime.Now,
                    Type = "Rent",
                    Status = "Unpaid"
                };
                db.Payments.Add(thanhToanMoi);
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
        //Phòng đã đk
        public ActionResult DanhSachPhong()
        {
            int userId = int.Parse(Session["UserID"].ToString());

            var dsDangKy = db.Registrations
                .Where(r => r.UserID == userId)
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

            return RedirectToAction("DanhSachPhong");
        }

    }
}
