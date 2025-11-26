using KTXSV.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace KTXSV.Controllers
{
    public class ManageStudentsController : Controller
    {
        private KTXSVEntities db = new KTXSVEntities();

        // Kiểm tra  Admin
        private bool IsAdmin() => Session["Role"] != null &&
                                   Session["Role"].ToString().Equals("Admin", StringComparison.OrdinalIgnoreCase);

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!IsAdmin())
            {
                filterContext.Result = RedirectToAction("Index", "Account");
                return;
            }
            if (Session["UserID"] != null)
            {
                int currentUserId = Convert.ToInt32(Session["UserID"]);
                var currentUser = db.Users.Find(currentUserId);
                ViewBag.FullName = currentUser != null ? currentUser.FullName : "Admin";
            }
            else
            {
                ViewBag.FullName = "Admin";
            }

            base.OnActionExecuting(filterContext);
        }

        // GET: ManageStudents
        public ActionResult Index(string search, string gender, string status, string sort)
        {
            var students = db.Users.Include("Registrations")
                                   .Where(u => u.Role == "Student")
                                   .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                students = students.Where(u => u.FullName.Contains(search) || u.Username.Contains(search));
            }

            if (!string.IsNullOrEmpty(gender))
            {
                students = students.Where(u => u.Gender == gender);
            }

            var studentList = students.ToList();

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
        public ActionResult Active(string search, string gender, string status, string sort)
        {
            var students = db.Users.Include("Registrations")
                .Where(u => u.Role == "Student" && u.Registrations.Any(r => r.Status == "Active"))
                .ToList();

            // Tìm kiếm
            if (!string.IsNullOrEmpty(search))
            {
                students = students.Where(u => u.FullName.Contains(search) || u.Username.Contains(search)).ToList();
            }

            // Lọc giới tính
            if (!string.IsNullOrEmpty(gender))
            {
                students = students.Where(u => u.Gender == gender).ToList();
            }

            // Sắp xếp
            switch (sort)
            {
                case "name_asc":
                    students = students.OrderBy(u => u.FullName).ToList();
                    break;
                case "name_desc":
                    students = students.OrderByDescending(u => u.FullName).ToList();
                    break;
                case "date_asc":
                    students = students.OrderBy(u => u.CreatedAt).ToList();
                    break;
                case "date_desc":
                    students = students.OrderByDescending(u => u.CreatedAt).ToList();
                    break;
                default:
                    students = students.OrderBy(u => u.UserID).ToList();
                    break;
            }

            return View("Index", students); //
        }


        public ActionResult Details(int id)
        {
            var student = db.Users
                .Include(u => u.Registrations.Select(r => r.Room)) 
                .Include(u => u.Registrations.Select(r => r.Bed))  
                .Include(u => u.StudentFiles)
                .FirstOrDefault(u => u.UserID == id);

            if (student == null)
                return HttpNotFound();

            return View(student);
        }


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
        public ActionResult Edit(User student, int? RegID, string RegistrationStatus)
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

                    if (RegID.HasValue)
                    {
                        var reg = db.Registrations.Find(RegID.Value);
                        if (reg != null)
                        {
                            reg.Status = RegistrationStatus;
                            db.SaveChanges();
                        }
                    }

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
        // GET: ManageStudents/Create
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(User student)
        {
            if (ModelState.IsValid)
            {
                student.Role = "Student";
                student.CreatedAt = DateTime.Now;

                if (string.IsNullOrEmpty(student.PasswordHash))
                {
                    student.PasswordHash = BCrypt.Net.BCrypt.HashPassword("student");
                }
                else
                {
                    student.PasswordHash = BCrypt.Net.BCrypt.HashPassword(student.PasswordHash);
                }
                if (string.IsNullOrEmpty(student.CCCD))
                    student.CCCD = "";
                if (string.IsNullOrEmpty(student.Gender))
                    student.Gender = "Male"; 
                if (string.IsNullOrEmpty(student.Username))
                    student.Username = "user" + DateTime.Now.Ticks;

                db.Users.Add(student);

                try
                {
                    db.SaveChanges();
                    TempData["Message"] = "Thêm sinh viên thành công!";
                    return RedirectToAction("Index");
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                {
                    foreach (var eve in ex.EntityValidationErrors)
                    {
                        foreach (var ve in eve.ValidationErrors)
                        {
                            System.Diagnostics.Debug.WriteLine($"Lỗi tại {ve.PropertyName}: {ve.ErrorMessage}");
                        }
                    }
                    TempData["Message"] = "Lỗi dữ liệu không hợp lệ, vui lòng kiểm tra lại!";
                }
            }
            return View(student);
        }


    }
}
