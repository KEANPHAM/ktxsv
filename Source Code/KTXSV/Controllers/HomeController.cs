using KTXSV.Models;
using System.Linq;
using System.Web.Mvc;

namespace KTXSV.Controllers
{
    public class HomeController : Controller
    {
<<<<<<< HEAD

=======
>>>>>>> 1d24f5c (Trung Kiên)
        KTXSVEntities db = new KTXSVEntities();

        public ActionResult Index()
        {
<<<<<<< HEAD
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
=======
            // Kiểm tra xem người dùng đã đăng nhập chưa
            if (Session["UserID"] != null)
            {
                int userID = (int)Session["UserID"];
                var user = db.Users.Find(userID);

                ViewBag.Username = user.Username;
                ViewBag.FullName = user.FullName;
                ViewBag.Email = user.Email;
            }
            else
            {
                return RedirectToAction("Index", "Account"); // chưa đăng nhập
            }
>>>>>>> 1d24f5c (Trung Kiên)

            return View();
        }

    }
<<<<<<< HEAD
}
    
=======

}
>>>>>>> 1d24f5c (Trung Kiên)
