using KTXSV.Models;
using KTXSV.Services;
using QRCoder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;

namespace KTXSV.Controllers
{
    public class ThanhToanController : Controller
    {
        private readonly AdminNotificationService _adminNotificationService;
        private readonly StudentNotificationService _studentNotificationService;

        public ThanhToanController()
        {
            _adminNotificationService = new AdminNotificationService(new KTXSVEntities());
            _studentNotificationService = new StudentNotificationService(new KTXSVEntities());
        }
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

        // GET: ThanhToan
        public ActionResult HoaDon()
        {

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

                payment.Status = "Pending";
                payment.PaymentDate = DateTime.Now;
                _adminNotificationService.SendAdminNotification("PaymentPending", payment);
                _studentNotificationService.SendStudentNotification(
                    payment.Registration.UserID,
                    payment.RegID,
                    "PaymentPending",
                    payment.Registration
                ); db.SaveChanges();
                TempData["Success"] = "Thanh toán thành công.";
            }

            return RedirectToAction("HoaDon");
        }

        public ActionResult QR(int id)
        {
            // Kiểm tra đăng nhập
            if (Session["UserID"] == null)
                return RedirectToAction("LoginStudent", "Account");

            //lấy hóa đơn từ db
            var payment = db.Payments
                .Include(p => p.Registration.User)
                .Include(p => p.Registration.Room)
                .FirstOrDefault(p => p.PaymentID == id);

            if (payment == null) return HttpNotFound();

            if (payment.Status == "Paid")
            {
                TempData["Success"] = "Hóa đơn này đã được thanh toán.";
                return RedirectToAction("HoaDon");
            }

            // Nội dung mã QR (bạn có thể đổi thành URL xác nhận sau này)
            string qrText =
                $"Hoa don #{payment.PaymentID}\n" +
                $"Sinh vien: {payment.Registration.User.FullName}\n" +
                $"Phong: {payment.Registration.Room.RoomNumber}\n" +
                $"So tien: {payment.Amount:N0} VND\n" +
                $"Han thanh toan: {(payment.PaymentDate.HasValue ? payment.PaymentDate.Value.ToString("dd/MM/yyyy") : "-")}";

            // Tạo ảnh QR và đưa ra dạng base64 để nhúng thẳng vào <img>
            using (var generator = new QRCodeGenerator())
            using (var data = generator.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.Q))
            using (var qrCode = new QRCode(data))
            using (var bmp = qrCode.GetGraphic(18))
            using (var ms = new MemoryStream())
            {
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ViewBag.QRImage = "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
            }

            return View(payment);
        }

        public ActionResult QRPopup(int id)
        {
            var payment = db.Payments
                .Include(p => p.Registration.User)
                .Include(p => p.Registration.Room)
                .FirstOrDefault(p => p.PaymentID == id);

            if (payment == null) return HttpNotFound();

            string qrText = $"Hoa don #{payment.PaymentID}\nSinh vien: {payment.Registration.User.FullName}\nPhong: {payment.Registration.Room.RoomNumber}\nSo tien: {payment.Amount:N0} VND";

            using (var generator = new QRCodeGenerator())
            using (var data = generator.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.Q))
            using (var qrCode = new QRCode(data))
            using (var bmp = qrCode.GetGraphic(18))
            using (var ms = new MemoryStream())
            {
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ViewBag.QRImage = "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
            }

            return PartialView("QRPartial", payment);
        }


    }
}