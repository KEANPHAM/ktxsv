using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using KTXSV.Models;


namespace KTXSV.Controllers
{
    public class AccountController : Controller
    {
        // GET: Account
        public ActionResult Index()
        {
            return View();
        }
        private KTXSVEntities db = new KTXSVEntities();

        // GET: Register
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Register(User model)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra trùng MSSV (Username)
                var exists = db.Users.FirstOrDefault(s => s.Username == model.Username);
                if (exists != null)
                {
                    ViewBag.Error = "Mã số sinh viên đã tồn tại!";
                    return View(model);
                }

                // Hash mật khẩu (cơ bản)
                model.PasswordHash = model.PasswordHash; // Có thể mã hóa MD5/SHA sau
                model.CreatedAt = DateTime.Now;

                db.Users.Add(model);
                db.SaveChanges();
                return RedirectToAction("Login");
            }

            return View(model);
        }


        // GET: Login
        public ActionResult Login()
        {
            return View();
        }


        public ActionResult LoginStudent()
        {
            return View();
        }

        [HttpPost]
        public ActionResult LoginStudent(string username, string password)
        {
            if (username == "student" && password == "123")
            {
                ViewBag.Message = "Đăng nhập sinh viên thành công!";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Message = "Sai tài khoản hoặc mật khẩu sinh viên.";
            return View();
        }
        public ActionResult LoginAdmin()
        {
            return View();
        }

        [HttpPost]
        public ActionResult LoginAdmin(string username, string password)
        {
            // Tài khoản admin do máy chủ cấp
            if (username == "admin" && password == "admin123")
            {
                ViewBag.Message = "Đăng nhập admin thành công!";
                return RedirectToAction("Dashboard", "Admin");
            }

            ViewBag.Message = "Sai tài khoản hoặc mật khẩu admin.";
            return View();
        }
    }
}