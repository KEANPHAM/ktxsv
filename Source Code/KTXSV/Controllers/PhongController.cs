using KTXSV.Models;
using System;
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

            var dangKyHienTai = db.Registrations
                .FirstOrDefault(r => r.UserID == userId && (r.Status == "Pending" || r.Status == "Active"));
            ViewBag.DaDangKy = dangKyHienTai != null;

            var phongTrong = db.Rooms.AsQueryable();

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
        public ActionResult DangKyPhong(int roomId)
        {
            int userId = int.Parse(Session["UserID"].ToString());

            bool daDangKy = db.Registrations.Any(r => r.UserID == userId &&
                (r.Status == "Active" || r.Status == "Pending"));
            if (daDangKy)
            {
                TempData["Error"] = "Bạn đã có phòng hoặc đang chờ duyệt. Không thể đăng ký thêm.";
                return RedirectToAction("DangKyPhong");
            }

            var phong = db.Rooms.Find(roomId);

            if (phong != null && phong.Status == "Available")
            {
                var dangKyMoi = new Registration
                {
                    UserID = userId,
                    RoomID = roomId,
                    StartDate = DateTime.Now,
                    Status = "Pending",
                    
                };
                phong.Occupied = (phong.Occupied ?? 0) + 1;
                db.Registrations.Add(dangKyMoi);
                db.SaveChanges();

                TempData["Success"] = "Đăng ký phòng thành công. Vui lòng chờ duyệt.";
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
        public ActionResult HuyDangKy(int regId)
        {
            var reg = db.Registrations.Find(regId);
            var phong = db.Rooms.Find(regId);
            if (reg != null && reg.Status == "Pending")
            {
                db.Registrations.Remove(reg);
                db.SaveChanges();
                phong.Occupied = (phong.Occupied ?? 0) - 1;

                TempData["Success"] = "Đã hủy yêu cầu đăng ký phòng.";
            }
            else
            {
                TempData["Error"] = "Không thể hủy đăng ký đã duyệt hoặc không tồn tại.";
            }

            return RedirectToAction("DanhSachPhong");
        }
    }
}
