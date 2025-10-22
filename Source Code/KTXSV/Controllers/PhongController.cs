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

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult HoSo()
        {
            return View();
        }
        public ActionResult DangKyPhong(string gender, string building, int? capacity)
        {
            Session["UserID"] = 1;
            Session["UserName"] = "Kiên Phạm";
            Session["Role"] = "Student";

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
                .Select(f => f.FileType)
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
                    BedID = bedId,

                };
                phong.Occupied = (phong.Occupied ?? 0) + 1;
                bed.IsOccupied = true;


                if (phong.Occupied == phong.Capacity)
                {
                    phong.Status = "Full";
                }
                db.Registrations.Add(dangKyMoi);
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
            var reg = db.Registrations.Find(regId);
            if (reg == null || bedId == null)
            {
                TempData["Error"] = "Không tìm thấy đăng ký.";
                return RedirectToAction("DanhSachPhong");
            }
            
            var phong = db.Rooms.Find(reg.RoomID);
            var bed = db.Beds.SingleOrDefault(b => b.BedID == bedId);

            if (reg.Status == "Pending" || reg.Status == "Active")
            {
                // 🔹 Cập nhật trạng thái đăng ký
                reg.Status = "Canceled";

                // 🔹 Cập nhật giường
                if (bed != null)
                {
                    bed.IsOccupied = false;

                    // Ép EF theo dõi thay đổi
                    db.Beds.Attach(bed);
                    db.Entry(bed).Property(b => b.IsOccupied).IsModified = true;
                }

                // 🔹 Cập nhật phòng
                if (phong != null)
                {
                    phong.Occupied = Math.Max((phong.Occupied ?? 1) - 1, 0);
                    if (phong.Status == "Full" && phong.Occupied < phong.Capacity)
                        phong.Status = "Available";

                    db.Rooms.Attach(phong);
                    db.Entry(phong).Property(p => p.Occupied).IsModified = true;
                    db.Entry(phong).Property(p => p.Status).IsModified = true;
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
