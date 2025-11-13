using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using KTXSV.Models;

namespace KTXSV.Controllers
{
    public class ThongBaoController : Controller
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

        // GET: /ThongBao/ThongBao
        public ActionResult ThongBao()
        {
            // 1. Bắt buộc phải đăng nhập sinh viên
            if (Session["UserID"] == null)
                return RedirectToAction("LoginStudent", "Account");

            int userId;
            if (!int.TryParse(Session["UserID"].ToString(), out userId))
                return RedirectToAction("LoginStudent", "Account");

            // 2. Lấy danh sách thông báo dành cho:
            //    - đúng user này (UserID == userId)
            //    - hoặc thông báo chung cho ALL / Student không gắn UserID
            var notifications = db.Notifications
                .Where(n =>
                    (n.TargetRole == "ALL" || n.TargetRole == "Student") &&
                    (n.UserID == null || n.UserID == userId)
                )
                .OrderByDescending(n => n.CreatedAt)
                .ToList();

            // 3. Đánh dấu đã đọc những thông báo riêng của user
            foreach (var n in notifications.Where(n => n.UserID == userId && !n.IsRead))
            {
                n.IsRead = true;
            }
            db.SaveChanges();

            // 4. Trả về view kèm list thông báo
            return View(notifications);
        }
    }
}
