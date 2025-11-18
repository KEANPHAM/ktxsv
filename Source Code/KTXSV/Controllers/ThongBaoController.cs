using KTXSV.Models;
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
        public ActionResult ThongBao()
        {
            if (Session["UserID"] == null)
                return RedirectToAction("LoginStudent", "Account");

            int userId;
            if (!int.TryParse(Session["UserID"].ToString(), out userId))
                return RedirectToAction("LoginStudent", "Account");

            var notifications = db.Notifications
                .Where(n =>
                    (n.TargetRole == "ALL" || n.TargetRole == "Student") &&
                    (n.UserID == null || n.UserID == userId)
                )
                .OrderByDescending(n => n.CreatedAt)
                .ToList();
            foreach (var n in notifications.Where(n => n.UserID == userId && !n.IsRead))
            {
                n.IsRead = true;
            }
            db.SaveChanges();

            return View(notifications);
        }
    }
}
