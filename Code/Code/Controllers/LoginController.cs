using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Code.Controllers
{
    public class LoginController : Controller
    {

    
        // Kiểm tra đăng nhập
        public ActionResult Index( string Email, string Password)
        {
            var user = db.Users.FirstOrDefault(u => u.Email == Email && u.Password == Password);
            if (user != null)
            {
                Session["UserId"] = user.UserId;
                Session["Email"] = user.Email;
                Session["Role"] = user.Role;
                return RedirectToAction("Index", "Home");
            }
            ViewBag.Error = "Email hoặc mật khẩu không đúng";
            return View();
        }
    }
}