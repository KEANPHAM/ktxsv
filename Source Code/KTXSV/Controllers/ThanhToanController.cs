using KTXSV.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace KTXSV.Controllers
{
    public class ThanhToanController : Controller
    {
        // GET: ThanhToan
        public ActionResult HoaDon()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }
            int userId = (int)Session["UserID"];  // Lấy ID người đang đăng nhập
            using (var db = new KTXSVEntities())
            {
                //var payments=db.Payments.ToList(); //get all hoa don
                var payments = db.Payments
                    .Where(p => p.Registration.UserID == userId) //lọc hd ng tạo hợp đồng đki phòng == userid đăng nhập
                    .ToList();
                return View(payments);

            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThanhToan(int id)
        {
            using (var db = new KTXSVEntities())
            {
                var payment = db.Payments.Find(id); //id==paymentid sql
                if (payment == null)
                {
                    return HttpNotFound(); //không tìm thấy hđ
                }

                if (payment.Status == "Paid")
                {
                    TempData["Error"] = "Hóa đơn đã được thanh toán trước đó.";
                    return RedirectToAction("HoaDon");
                }

                payment.Status = "Paid";
                payment.PaymentDate = DateTime.Now;

                db.SaveChanges();
                TempData["Success"] = "Thanh toán thành công.";
            }

            return RedirectToAction("HoaDon");
        }

    }
}