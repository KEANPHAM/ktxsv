﻿using KTXSV.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace KTXSV.Controllers
{
    public class ManageStudentsController : Controller
    {
        private KTXSVEntities db = new KTXSVEntities();

        // Kiểm tra quyền admin
        private bool IsAdmin()
        {
            return Session["Role"] != null &&
                   Session["Role"].ToString().Equals("Admin", StringComparison.OrdinalIgnoreCase);
        }

        // Chặn truy cập nếu không phải admin
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!IsAdmin())
            {
                filterContext.Result = RedirectToAction("Index", "Account");
                return;
            }
            base.OnActionExecuting(filterContext);
        }

        // GET: ManageStudents
        public ActionResult Index(string search, string gender, string status, string sort)
        {
            // Lấy dữ liệu sinh viên, kèm registrations
            var students = db.Users.Include("Registrations")
                                   .Where(u => u.Role == "Student")
                                   .AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrEmpty(search))
            {
                students = students.Where(u => u.FullName.Contains(search) || u.Username.Contains(search));
            }

            // Lọc giới tính
            if (!string.IsNullOrEmpty(gender))
            {
                students = students.Where(u => u.Gender == gender);
            }

            // Đưa dữ liệu về memory để xử lý trạng thái, vì EF6 không hiểu FirstOrDefault trong Where
            var studentList = students.ToList();

            // Lọc trạng thái
            if (!string.IsNullOrEmpty(status))
            {
                studentList = studentList.Where(u => u.Registrations
                                                     .OrderByDescending(r => r.StartDate)
                                                     .FirstOrDefault() != null &&
                                                     u.Registrations
                                                     .OrderByDescending(r => r.StartDate)
                                                     .FirstOrDefault().Status == status)
                                         .ToList();
            }

            // Sắp xếp
            switch (sort)
            {
                case "name_asc":
                    studentList = studentList.OrderBy(u => u.FullName).ToList();
                    break;
                case "name_desc":
                    studentList = studentList.OrderByDescending(u => u.FullName).ToList();
                    break;
                case "date_asc":
                    studentList = studentList.OrderBy(u => u.CreatedAt).ToList();
                    break;
                case "date_desc":
                    studentList = studentList.OrderByDescending(u => u.CreatedAt).ToList();
                    break;
                default:
                    studentList = studentList.OrderBy(u => u.UserID).ToList();
                    break;
            }

            return View(studentList);
        }

        // GET: ManageStudents/Details/5
        public ActionResult Details(int id)
        {
            var student = db.Users.Find(id);
            if (student == null || student.Role != "Student")
                return HttpNotFound();

            return View(student);
        }

        // GET: ManageStudents/Edit/5
        public ActionResult Edit(int id)
        {
            var student = db.Users.Find(id);
            if (student == null || student.Role != "Student")
                return HttpNotFound();

            return View(student);
        }

        // POST: ManageStudents/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(User student)
        {
            if (ModelState.IsValid)
            {
                var existing = db.Users.Find(student.UserID);
                if (existing != null && existing.Role == "Student")
                {
                    existing.FullName = student.FullName;
                    existing.Email = student.Email;
                    existing.Phone = student.Phone;
                    existing.Gender = student.Gender;

                    db.SaveChanges();
                    TempData["Message"] = "Cập nhật sinh viên thành công!";
                    return RedirectToAction("Index");
                }
                return HttpNotFound();
            }
            return View(student);
        }

        // POST: ManageStudents/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var student = db.Users.Find(id);
            if (student != null && student.Role == "Student")
            {
                db.Users.Remove(student);
                db.SaveChanges();
                TempData["Message"] = "Xóa sinh viên thành công!";
            }
            else
            {
                TempData["Message"] = "Không tìm thấy sinh viên!";
            }
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
