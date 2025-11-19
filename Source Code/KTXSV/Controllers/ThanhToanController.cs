using KTXSV.Models;
using KTXSV.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

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

                payment.Status = "Paid";
                payment.PaymentDate = DateTime.Now;

                db.SaveChanges();
                TempData["Success"] = "Thanh toán thành công.";
            }

            return RedirectToAction("HoaDon");
        }

    }
}