using KTXSV.Models;
using System.Linq;
using System.Web.Mvc;

namespace KTXSV.Controllers
{
    public class HomeController : Controller
    {

        KTXSVEntities db = new KTXSVEntities();

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

            var thongBao = db.Notifications.Where(n => n.TargetRole == "All" || n.TargetRole == user.Role)
                .OrderByDescending(n => n.CreatedAt)
                .Take(4).ToList();
            return View(thongBao);
        }
        

    }
}
    