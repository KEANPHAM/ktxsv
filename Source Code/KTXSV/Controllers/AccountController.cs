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
                if (db.Users.Any(u => u.Username == model.Username))
                {
                    ViewBag.Error = "Mã số sinh viên đã tồn tại!";
                    return View(model);
                }

                // Kiểm tra trùng Email
                if (db.Users.Any(u => u.Email == model.Email))
                {
                    ViewBag.Error = "Email đã được đăng ký!";
                    return View(model);
                }

                // Kiểm tra Gender hợp lệ (ví dụ: "Male" hoặc "Female")
                if (model.Gender != "Male" && model.Gender != "Female")
                {
                    ViewBag.Error = "Giới tính không hợp lệ!";
                    return View(model);
                }

                // Hash mật khẩu (nếu muốn)
                model.PasswordHash = model.PasswordHash; // hoặc hash MD5/SHA
                model.CreatedAt = DateTime.Now;

                db.Users.Add(model);
                db.SaveChanges();
                return RedirectToAction("LoginStudent");
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
            var userID = db.Users.Where(u => u.Username == username).FirstOrDefault();
            if (userID.Role == "Student" && userID.PasswordHash == password)
            {
                Session["UserID"] = userID.UserID;           // ID trong database
                Session["Role"] = userID.Role;           // Role

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
                return RedirectToAction("PendingRegistrations", "Admin");
            }

            ViewBag.Message = "Sai tài khoản hoặc mật khẩu admin.";
            return View();
        }
        [HttpPost]
        public ActionResult Logout()
        {
            Session.Clear(); // hoặc Session.Abandon();
            return RedirectToAction("Index","Account"); // trả về trang đăng nhập sinh viên
        }

        

        
    }
}