using KTXSV.Models;
using System.Linq;
using System.Web.Mvc;

namespace KTXSV.Controllers
{
    public class HomeController : Controller
    {
<<<<<<< HEAD
        private readonly KTXSVEntities db = new KTXSVEntities();

        public ActionResult Index()
        {
           
            return View();
        }
    }
}
=======
        KTXSVEntities db = new KTXSVEntities();

        public ActionResult Index()
        {
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
                return RedirectToAction("LoginStudent", "Account"); // chưa đăng nhập
            }

            return View();
        }
    }

}
>>>>>>> eec902b (Cập nhật AccountController, Views và HomeController)
