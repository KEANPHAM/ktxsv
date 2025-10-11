using KTXSV.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static System.Collections.Specialized.BitVector32;
namespace KTXSV.Controllers
{
    public class PhongController : Controller
    {
        KTXSVEntities db = new KTXSVEntities();

        public ActionResult DangKyPhong(string gender, string building, int? capacity)
        {
            //tạo session user mẫu để test
            Session["UserID"] = 1;
            Session["UserName"] = "Kiên Phạm";
            Session["Role"] = "Student";
            //lấy dsach phòng trống
            var phongTrong = db.Rooms.Where(phong => phong.Status == "Available").ToList();

            if (!string.IsNullOrEmpty(gender) )
            {
                phongTrong = phongTrong.Where(p => p.Gender == gender).ToList();
            }
            if (!string.IsNullOrEmpty(building))
            {
                phongTrong = phongTrong.Where(p => p.Building == building).ToList();
            }
            if (capacity.HasValue)

            {
                phongTrong = phongTrong.Where(p => p.Capacity ==capacity.Value).ToList();
            }
            return View(phongTrong);
        }

        [HttpPost]
        public ActionResult DangKyPhong(int roomId)
        {

            int userId = int.Parse(Session["UserID"].ToString());

            //Kiểm tra user đã đăng ký phòng chưa
            bool kiemtra = db.Registrations.Any(r => r.UserID == userId && (r.Status == "Active" || r.Status == "Pending"));
            if (kiemtra)
            {
                TempData["Error"] = "Bạn đã đăng ký phòng hoặc đang chờ duyệt.";
                return RedirectToAction("DangKyPhong");
            }

            var phong = db.Rooms.Find(roomId);

            if (phong != null ||phong.Status == "Available" )
            {
                var dangkymoi = new Registration
                {
                    UserID = userId,
                    RoomID = roomId,
                    StartDate = DateTime.Now,
                    Status = "Pending"
                };
                db.Registrations.Add(dangkymoi);
                phong.Occupied = (phong.Occupied ?? 0) + 1;
                if (phong.Occupied >= phong.Capacity)
                {
                    phong.Status = "Full";
                }
                db.SaveChanges();

                TempData["Success"]= "Đăng ký phòng thành công. Vui lòng chờ duyệt.";
            }
            else
            {
                TempData["Error"] = "Phong đã đầy.";
            }
            return RedirectToAction("DangKyPhong");
        }
        [HttpPost]
        public ActionResult HuyDangKy(int regId)
        {
            var reg = db.Registrations.Find(regId);

            if (reg != null && reg.Status == "Pending")
            {
                db.Registrations.Remove(reg); 
                db.SaveChanges();

                TempData["Success"] = "Hủy yêu cầu đăng ký phòng thành công.";
            }
            else
            {
                TempData["Error"] = "Không thể hủy phòng đang duyệt hoặc đã active.";
            }

            return RedirectToAction("DanhSachDangKy"); 
        }
        public ActionResult DanhSachPhong()
        {
            return View();
        }
        public ActionResult Index()
        {
            return View();
        }
    }
}

