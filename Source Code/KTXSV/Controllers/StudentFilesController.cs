using KTXSV.Models;
using KTXSV.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace KTXSV.Controllers
{
    public class StudentFilesController : Controller
    {
        private readonly AdminNotificationService _adminNotificationService;
        private readonly StudentNotificationService _studentNotificationService;

        public StudentFilesController()
        {
            _adminNotificationService = new AdminNotificationService(new KTXSVEntities());
            _studentNotificationService = new StudentNotificationService(new KTXSVEntities());
        }
        KTXSVEntities db = new KTXSVEntities();
        // GET: StudentFiles
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

            return View();
        }
        [HttpPost]
        public ActionResult UploadFiles(HttpPostedFileBase CCCD, HttpPostedFileBase BHYT, HttpPostedFileBase StudentCard, HttpPostedFileBase Portrait)
        {
            int userId = int.Parse(Session["UserID"].ToString());
            string uploadPath = Server.MapPath("~/Uploads/Students/");

            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            var existingFiles = db.StudentFiles.Where(f => f.UserID == userId).ToList();

            void SaveFile(HttpPostedFileBase file, string type)
            {
                if (file != null && file.ContentLength > 0)
                {
                    var oldFile = existingFiles.FirstOrDefault(f => f.FileType == type);
                    if (oldFile != null)
                    {
                        string oldFullPath = Server.MapPath(oldFile.FilePath);
                        if (System.IO.File.Exists(oldFullPath))
                            System.IO.File.Delete(oldFullPath);
                        db.StudentFiles.Remove(oldFile);
                    }
                    string fileName = $"{userId}_{type}_{Path.GetFileName(file.FileName)}";
                    string fullPath = Path.Combine(uploadPath, fileName);
                    file.SaveAs(fullPath);
                    var newFile = new StudentFile
                    {
                        UserID = userId,
                        FileType = type,
                        FilePath = "/Uploads/Students/" + fileName,
                        UploadedAt = DateTime.Now
                    };
                    db.StudentFiles.Add(newFile);
                }
            }
            SaveFile(CCCD, "CCCD");
            SaveFile(BHYT, "BHYT");
            SaveFile(StudentCard, "StudentCard");
            SaveFile(Portrait, "Portrait");

            db.SaveChanges();

            try
            {
                _studentNotificationService.SendGeneralNotification(userId, "FileUploadSuccess");
            }
            catch (Exception ex)
            {
                // Ghi log lỗi nếu không gửi được thông báo nhưng không chặn luồng chính
                System.Diagnostics.Debug.WriteLine("Error sending file upload notification: " + ex.Message);
            }
            TempData["Success"] = "Cập nhật hồ sơ thành công.";
            return RedirectToAction("DangKyPhong", "Phong");
        }

    }
}