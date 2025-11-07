using KTXSV.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace KTXSV.Controllers
{
    public class SinhVienController : Controller
    {
        private KTXSVEntities db = new KTXSVEntities();
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

        // GET: SinhVien/ThongTinCaNhan
        public ActionResult ThongTinCaNhan()
        {
            if (Session["UserID"] == null)
                return RedirectToAction("Login", "Account");

            int userId = Convert.ToInt32(Session["UserID"]);
            var user = db.Users.Find(userId);

            if (user == null)
                return HttpNotFound();

            return View(user);
        }

        // GET: SinhVien/ChinhSuaThongTin
        public ActionResult ChinhSuaThongTin()
        {
            if (Session["UserID"] == null)
                return RedirectToAction("Index", "Account");

            int userId = Convert.ToInt32(Session["UserID"]);
            var user = db.Users.Find(userId);

            if (user == null)
                return HttpNotFound();

            return View(user);
        }

        // POST: SinhVien/ChinhSuaThongTin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChinhSuaThongTin([Bind(Include = "UserID,FullName,Gender,Email,Phone")] User updatedUser)
        {
            if (!ModelState.IsValid)
                return View(updatedUser);

            var user = db.Users.Find(updatedUser.UserID);
            if (user == null)
                return HttpNotFound();

            // Chỉ cho phép chỉnh sửa thông tin cơ bản
            user.FullName = updatedUser.FullName;
            user.Gender = updatedUser.Gender;
            user.Email = updatedUser.Email;
            user.Phone = updatedUser.Phone;

            db.SaveChanges();
            TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
            return RedirectToAction("ThongTinCaNhan");
        }
    }
}
