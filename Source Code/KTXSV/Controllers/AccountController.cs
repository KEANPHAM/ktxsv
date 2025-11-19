using KTXSV.Models;
using System;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Web.Configuration;
using System.Web.Mvc;
using BCrypt.Net;

namespace KTXSV.Controllers
{
    public class AccountController : Controller
    {
        private KTXSVEntities db = new KTXSVEntities();

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (string.IsNullOrWhiteSpace(model.FullName))
                {
                    ViewBag.Error = "Họ tên không được để trống";
                    return View(model);
                }

                string[] nameParts = model.FullName.Trim().Split(' ');
                if (nameParts.Length < 2)
                {
                    ViewBag.Error = "Họ tên phải có ít nhất 2 thành phần";
                    return View(model);
                }
                if (!Regex.IsMatch(model.FullName, @"^[a-zA-ZÀ-ỹ\s]+$"))
                {
                    ViewBag.Error = "Họ tên không được chứa ký tự đặc biệt hoặc số";
                    return View(model);
                }
                foreach (var part in nameParts)
                {
                    if (!string.IsNullOrWhiteSpace(part) && !char.IsUpper(part[0]))
                    {
                        ViewBag.Error = "Chữ cái đầu mỗi thành phần trong họ tên phải viết hoa";
                        return View(model);
                    }
                }

                if (string.IsNullOrWhiteSpace(model.PasswordHash))
                {
                    ViewBag.Error = "Mật khẩu không được để trống";
                    return View(model);
                }
                if (model.PasswordHash.Length < 8 ||
                    !Regex.IsMatch(model.PasswordHash, @"[A-Z]") ||
                    !Regex.IsMatch(model.PasswordHash, @"\d") ||
                    !Regex.IsMatch(model.PasswordHash, @"[!@#$%^&*(),.?""':{}|<>]"))
                {
                    ViewBag.Error = "Mật khẩu phải có ít nhất 8 ký tự, gồm chữ hoa, số và ký tự đặc biệt";
                    return View(model);
                }
                if (model.PasswordHash != model.ConfirmPassword)
                {
                    ViewBag.Error = "Xác nhận mật khẩu không khớp";
                    return View(model);
                }

                // ===== Kiểm tra ngày sinh =====
                if (!model.BirthDate.HasValue)
                {
                    ViewBag.Error = "Vui lòng nhập ngày sinh.";
                    return View(model);
                }

                DateTime birthDate = model.BirthDate.Value;
                int age = DateTime.Today.Year - birthDate.Year;
                if (birthDate > DateTime.Today.AddYears(-age)) age--;
                if (age < 18)
                {
                    ViewBag.Error = "Bạn phải đủ 18 tuổi trở lên để đăng ký.";
                    return View(model);
                }

                if (db.Users.Any(u => u.Username == model.Username))
                {
                    ViewBag.Error = "Mã số sinh viên đã tồn tại!";
                    return View(model);
                }
                if (db.Users.Any(u => u.Email == model.Email))
                {
                    ViewBag.Error = "Email đã được đăng ký!";
                    return View(model);
                }

                if (model.Gender != "Male" && model.Gender != "Female")
                {
                    ViewBag.Error = "Giới tính không hợp lệ!";
                    return View(model);
                }

                if (string.IsNullOrEmpty(model.CCCD) || model.CCCD.Length != 12)
                {
                    ViewBag.Error = "Số CCCD phải gồm 12 chữ số.";
                    return View(model);
                }

                string[] validProvinces = { "001", "002", "004", "006", "008", "010", "011", "012", "014", "015", "017", "019", "020", "022", "024", "025", "026", "027", "030", "031", "033", "034", "035", "036", "037", "038", "040", "042", "044", "045", "046", "048", "049", "051", "052", "054", "056", "058", "060", "062", "064", "066", "067", "068", "070", "072", "074", "075", "077", "079", "080", "082", "083", "084", "086", "087", "089", "091", "092", "093", "094", "095", "096" };
                if (!validProvinces.Contains(model.CCCD.Substring(0, 3)))
                {
                    ViewBag.Error = "Mã tỉnh/thành phố trong CCCD không hợp lệ.";
                    return View(model);
                }

                int birthYear = model.BirthDate.Value.Year;
                int centuryCode = (birthYear / 100) - 19;
                int expectedDigit = (model.Gender == "Male") ? (centuryCode * 2) : (centuryCode * 2 + 1);
                if (int.Parse(model.CCCD.Substring(3, 1)) != expectedDigit)
                {
                    ViewBag.Error = "Số CCCD không khớp với giới tính hoặc năm sinh.";
                    return View(model);
                }

                string[] validPrefixes = { "032","033","034","035","036","037","038","039",
                                           "086","096","097","098","070","076","077","078","079",
                                           "090","093","089","056","058","092","059","099" };
                if (!Regex.IsMatch(model.Phone ?? "", @"^\d{10}$") || !validPrefixes.Contains(model.Phone.Substring(0, 3)))
                {
                    ViewBag.Error = "Số điện thoại không hợp lệ.";
                    return View(model);
                }

                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.PasswordHash);

                var user = new User
                {
                    Username = model.Username,
                    PasswordHash = hashedPassword,
                    FullName = model.FullName,
                    Gender = model.Gender,
                    Email = model.Email,
                    Phone = model.Phone,
                    Role = "Student",
                    CreatedAt = DateTime.Now,
                    BirthDate = model.BirthDate,
                    CCCD = model.CCCD
                };

                try
                {
                    db.Users.Add(user);
                    db.SaveChanges();
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                {
                    foreach (var eve in ex.EntityValidationErrors)
                    {
                        foreach (var ve in eve.ValidationErrors)
                        {
                            System.Diagnostics.Debug.WriteLine($"Lỗi: {ve.PropertyName} - {ve.ErrorMessage}");
                        }
                    }
                    ViewBag.Error = "Có lỗi xảy ra khi lưu dữ liệu. Vui lòng kiểm tra lại.";
                    return View(model);
                }

                return RedirectToAction("LoginStudent", "Account");
            }

            return View(model);
        }

        public ActionResult LoginStudent()
        {
            return View();
        }

        [HttpPost]
        public ActionResult LoginStudent(string username, string password)
        {
            var user = db.Users.FirstOrDefault(u => u.Username == username);

            if (user != null && user.Role == "Student" && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                Session["UserID"] = user.UserID;
                Session["Role"] = user.Role;
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
            var admin = db.Users.FirstOrDefault(u => u.Username == username && u.Role == "Admin");

            if (admin != null && BCrypt.Net.BCrypt.Verify(password, admin.PasswordHash))
            {
                Session["UserID"] = admin.UserID;
                Session["Role"] = admin.Role;
                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.Message = "Sai tài khoản hoặc mật khẩu admin.";
            return View();
        }

        [HttpPost]
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("LoginStudent", "Account");
        }

        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ForgotPassword(string Email)
        {
            var user = db.Users.FirstOrDefault(u => u.Email == Email);
            if (user == null)
            {
                TempData["Error"] = "Email này chưa được đăng ký.";
                return RedirectToAction("ForgotPassword");
            }

            string otp = new Random().Next(100000, 999999).ToString();
            Session["OTP"] = otp;
            Session["EmailReset"] = Email;
            Session["OTPExpire"] = DateTime.Now.AddMinutes(5);

            SendEmail(Email, "Mã xác nhận đặt lại mật khẩu",
                $"<h3>Mã xác nhận của bạn là: <b>{otp}</b></h3><p>Có hiệu lực trong 5 phút.</p>");

            TempData["Message"] = "Mã xác minh đã được gửi đến email của bạn.";
            return RedirectToAction("VerifyOTP");
        }

        // ===== Xác minh OTP =====
        [HttpGet]
        public ActionResult VerifyOTP() => View();

        [HttpPost]
        public ActionResult VerifyOTP(string otp)
        {
            string sessionOtp = Session["OTP"] as string;
            DateTime? expire = Session["OTPExpire"] as DateTime?;

            if (sessionOtp == null || expire == null || DateTime.Now > expire)
            {
                ViewBag.Error = "Mã xác minh đã hết hạn. Vui lòng thử lại.";
                return View();
            }

            if (otp != sessionOtp)
            {
                ViewBag.Error = "Mã xác minh không đúng.";
                return View();
            }

            return RedirectToAction("ResetPassword");
        }

        // ===== Đặt lại mật khẩu =====
        [HttpGet]
        public ActionResult ResetPassword() => View();

        [HttpPost]
        public ActionResult ResetPassword(string NewPassword, string ConfirmPassword)
        {
            if (NewPassword != ConfirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp.";
                return View();
            }

            string email = Session["EmailReset"] as string;
            var user = db.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                ViewBag.Error = "Không tìm thấy tài khoản.";
                return View();
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(NewPassword);
            db.SaveChanges();

            Session.Remove("OTP");
            Session.Remove("EmailReset");
            Session.Remove("OTPExpire");

            return RedirectToAction("LoginStudent", "Account");
        }

        // ===== Hàm gửi email =====
        private bool SendEmail(string toEmail, string subject, string body)
        {
            try
            {
                string fromEmail = "thieutrungkien59@gmail.com";
                string appPassword = "dgmy jntq fyix zmgu";

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(fromEmail, "Ban quản lý KTX");
                mail.To.Add(toEmail);
                mail.Subject = subject;
                mail.Body = body;
                mail.IsBodyHtml = true;

                using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtp.Credentials = new NetworkCredential(fromEmail, appPassword);
                    smtp.EnableSsl = true;
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtp.Timeout = 15000;
                    smtp.Send(mail);
                }

                return true;
            }
            catch (SmtpException ex)
            {
                System.Diagnostics.Debug.WriteLine("SMTP Error: " + ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Email Error: " + ex.Message);
                return false;
            }
        }
    }
}
