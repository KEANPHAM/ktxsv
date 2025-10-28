using KTXSV.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;


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

                // ===== Kiểm tra họ tên =====
                if (string.IsNullOrWhiteSpace(model.FullName))
                {
                    ViewBag.Error = "Họ tên không được để trống";
                    return View(model);
                }

                string[] nameParts = model.FullName.Trim().Split(' ');

                // Kiểm tra ít nhất 2 thành phần
                if (nameParts.Length < 2)
                {
                    ViewBag.Error = "Họ tên phải có ít nhất 2 thành phần";
                    return View(model);
                }

                // Kiểm tra không chứa ký tự đặc biệt hoặc số
                if (!Regex.IsMatch(model.FullName, @"^[a-zA-ZÀ-ỹ\s]+$"))
                {
                    ViewBag.Error = "Họ tên không được chứa ký tự đặc biệt hoặc số";
                    return View(model);
                }

                // Kiểm tra chữ cái đầu mỗi thành phần phải viết hoa
                foreach (var part in nameParts)
                {
                    if (string.IsNullOrWhiteSpace(part)) continue; // bỏ qua khoảng trắng
                    if (!char.IsUpper(part[0]))
                    {
                        ViewBag.Error = "Chữ cái đầu mỗi thành phần trong họ tên phải viết hoa";
                        return View(model);
                    }
                }
                // ===== Kiểm tra mật khẩu =====
                if (string.IsNullOrWhiteSpace(model.PasswordHash))
                {
                    ViewBag.Error = "Mật khẩu không được để trống";
                    return View(model);
                }

                if (model.PasswordHash.Length < 8)
                {
                    ViewBag.Error = "Mật khẩu phải có ít nhất 8 ký tự";
                    return View(model);
                }

                // Kiểm tra ít nhất 1 chữ hoa, 1 số, 1 ký tự đặc biệt
                if (!Regex.IsMatch(model.PasswordHash, @"[A-Z]"))
                {
                    ViewBag.Error = "Mật khẩu phải chứa ít nhất 1 chữ cái viết hoa";
                    return View(model);
                }

                if (!Regex.IsMatch(model.PasswordHash, @"\d"))
                {
                    ViewBag.Error = "Mật khẩu phải chứa ít nhất 1 chữ số";
                    return View(model);
                }

                if (!Regex.IsMatch(model.PasswordHash, @"[!@#$%^&*(),.?""':{}|<>]"))
                {
                    ViewBag.Error = "Mật khẩu phải chứa ít nhất 1 ký tự đặc biệt";
                    return View(model);
                }

                // Kiểm tra xác nhận mật khẩu
                if (model.PasswordHash != model.ConfirmPassword)
                {
                    ViewBag.Error = "Xác nhận mật khẩu không khớp";
                    return View(model);
                }
                try
                {
                    if (model.BirthDate != null)
                    {
                        DateTime birthDate = model.BirthDate.Value;
                        DateTime today = DateTime.Today;

                        int age = today.Year - birthDate.Year;
                        // Nếu chưa tới sinh nhật trong năm nay thì trừ thêm 1
                        if (birthDate.Date > today.AddYears(-age)) age--;

                        if (age < 18)
                        {
                            ViewBag.Error = "Bạn phải đủ 18 tuổi trở lên để đăng ký.";
                            return View(model);
                        }
                    }
                    else if (model.BirthYear > 0)
                    {
                        int ageByYear = DateTime.Today.Year - model.BirthYear;
                        if (ageByYear < 18)
                        {
                            ViewBag.Error = "Bạn phải đủ 18 tuổi trở lên để đăng ký.";
                            return View(model);
                        }
                    }
                    else
                    {
                        ViewBag.Error = "Vui lòng nhập ngày sinh hoặc năm sinh.";
                        return View(model);
                    }
                }
                catch
                {
                    ViewBag.Error = "Ngày sinh không hợp lệ.";
                    return View(model);
                }

                // =====  Kiểm tra trùng MSSV (Username) =====
                if (db.Users.Any(u => u.Username == model.Username))
                {
                    ViewBag.Error = "Mã số sinh viên đã tồn tại!";
                    return View(model);
                }

                // =====  Kiểm tra trùng Email =====
                if (db.Users.Any(u => u.Email == model.Email))
                {
                    ViewBag.Error = "Email đã được đăng ký!";
                    return View(model);
                }

                // =====  Kiểm tra giới tính =====
                if (model.Gender != "Male" && model.Gender != "Female")
                {
                    ViewBag.Error = "Giới tính không hợp lệ!";
                    return View(model);
                }

                // =====  Kiểm tra CCCD =====
                string[] validProvinces = { "001", "002", "004", "006", "008", "010", "011", "012", "014", "015", "017", "019", "020", "022", "024", "025", "026", "027", "030", "031", "033", "034", "035", "036", "037", "038", "040", "042", "044", "045", "046", "048", "049", "051", "052", "054", "056", "058", "060", "062", "064", "066", "067", "068", "070", "072", "074", "075", "077", "079", "080", "082", "083", "084", "086", "087", "089", "091", "092", "093", "094", "095", "096"

 /* ... thêm các mã tỉnh khác */ };
                if (!Regex.IsMatch(model.CCCD ?? "", @"^\d{12}$"))
                {
                    ViewBag.Error = "Số CCCD phải gồm 12 chữ số";
                    return View(model);
                }
                else if (!validProvinces.Contains(model.CCCD.Substring(0, 3)))
                {
                    ViewBag.Error = "Mã tỉnh/thành phố trong CCCD không hợp lệ";
                    return View(model);
                }
                else
                {
                    // Kiểm tra mã thế kỷ + giới tính nếu có năm sinh
                    if (model.BirthYear > 0)
                    {
                        int centuryCode = (model.BirthYear / 100) - 19; // 19xx -> 0, 20xx -> 1 ...
                        int expectedDigit = (model.Gender == "Male") ? (centuryCode * 2) : (centuryCode * 2 + 1);

                        if (int.Parse(model.CCCD.Substring(3, 1)) != expectedDigit)
                        {
                            ViewBag.Error = "Số CCCD không khớp với giới tính hoặc năm sinh";
                            return View(model);
                        }
                    }
                }

                // ===== Kiểm tra số điện thoại =====
                string[] validPrefixes = { "032","033","034","035","036","037","038","039",
                                   "086","096","097","098","070","076","077","078","079",
                                   "090","093","089","056","058","092","059","099" };

                if (!Regex.IsMatch(model.Phone ?? "", @"^\d{10}$") || !validPrefixes.Contains(model.Phone.Substring(0, 3)))
                {
                    ViewBag.Error = "Số điện thoại không hợp lệ";
                    return View(model);
                }


                // ===== 7️⃣ Lưu dữ liệu =====
                model.Role = "Student";
                model.PasswordHash = model.PasswordHash; // hoặc hash nếu muốn
                model.CreatedAt = DateTime.Now;

                db.Users.Add(model);
                db.SaveChanges();

                return RedirectToAction("LoginStudent", "Account");
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
            var admin = db.Users.FirstOrDefault(u => u.Username == username && u.Role == "Admin");
            if (admin != null && admin.PasswordHash == password)
            {
                Session["UserID"] = admin.UserID;
                Session["Role"] = admin.Role;
                return RedirectToAction("PendingRegistrations", "Admin");
            }

            ViewBag.Message = "Sai tài khoản hoặc mật khẩu admin.";
            return View();
        }
        [HttpPost]
        public ActionResult Logout()
        {
            Session.Clear(); // hoặc Session.Abandon();
            return RedirectToAction("Index", "Account"); // trả về trang đăng nhập sinh viên
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

            // Các xử lý tiếp theo nếu email hợp lệ
            TempData["Message"] = "Mã xác minh đã được gửi đến email của bạn.";



            // Tạo mã xác minh ngẫu nhiên 6 chữ số
            string otp = new Random().Next(100000, 999999).ToString();

            // Lưu mã OTP và thời gian hết hạn (5 phút)
            Session["OTP"] = otp;
            Session["EmailReset"] = Email;
            Session["OTPExpire"] = DateTime.Now.AddMinutes(5);

            // Gửi email xác minh
            SendEmail(Email, "Mã xác nhận đặt lại mật khẩu",
                $"<h3>Mã xác nhận của bạn là: <b>{otp}</b></h3><p>Có hiệu lực trong 5 phút.</p>");

            return RedirectToAction("VerifyOTP");
        }

        // GET: VerifyOTP
        [HttpGet]
        public ActionResult VerifyOTP()
        {
            return View();
        }

        // POST: VerifyOTP
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

        // GET: ResetPassword
        [HttpGet]
        public ActionResult ResetPassword()
        {
            return View();
        }

        // POST: ResetPassword
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

            user.PasswordHash = NewPassword; // hoặc mã hóa lại nếu có
            db.SaveChanges();

            // Xóa session
            Session.Remove("OTP");
            Session.Remove("EmailReset");
            Session.Remove("OTPExpire");

            return RedirectToAction("LoginStudent", "Account");
        }

        // Hàm gửi email xác minh
        private bool SendEmail(string toEmail, string subject, string body)
        {
            try
            {
                string fromEmail = "thieutrungkien59@gmail.com"; // Gmail gửi đi
                string appPassword = "tcyh ukgf ugys ochz";    // App password 16 ký tự

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
                System.Diagnostics.Debug.WriteLine("General Error: " + ex.Message);
                return false;
            }
        }
    }
}
