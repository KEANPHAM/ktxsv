using KTXSV.Models;
using System.Linq;
using System.Web.Mvc;

namespace KTXSV.Controllers
{
    public class HomeController : Controller
    {
        private readonly KTXSVEntities db = new KTXSVEntities();

        public ActionResult Index()
        {
            // 🧠 Giả lập đăng nhập (chỉ khi Session chưa có)
            if (Session["UserID"] == null)
            {
                Session["UserID"] = 1;
                Session["UserName"] = "Kiên Phạm";
                Session["Role"] = "Student";
            }

            int userId = (int)Session["UserID"];
            var role = db.Users
                         .Where(u => u.UserID == userId)
                         .Select(u => u.Role)
                         .FirstOrDefault();

            if (role == null)
                return HttpNotFound("Không tìm thấy vai trò người dùng.");

            string currentController = ControllerContext.RouteData.Values["controller"].ToString();

            if (role == "Admin")
            {
                if (currentController != "Admin")
                    return RedirectToAction("PendingRegistrations", "Admin");
            }
            else
            {
                if (currentController != "SupportRequests")
                    return RedirectToAction("Index", "SupportRequests");
            }

            return View();
        }
    }
}
