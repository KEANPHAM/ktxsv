using KTXSV.Models;
using KTXSV.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace KTXSV.Controllers.Admin
{
    public class PaymentAdminController : Controller
    {
        private readonly AdminNotificationService _adminNotificationService;
        private readonly StudentNotificationService _studentNotificationService;

        public PaymentAdminController()
        {
            _adminNotificationService = new AdminNotificationService(new KTXSVEntities());
            _studentNotificationService = new StudentNotificationService(new KTXSVEntities());
        }
        KTXSVEntities db = new KTXSVEntities();
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

        [HttpGet]
        public ActionResult Index(string search = "", string type = "", string status = "", DateTime? date = null)
        {
            if (Session["UserID"] == null)
                return RedirectToAction("LoginStudent", "Account");

            int userID;
            if (!int.TryParse(Session["UserID"].ToString(), out userID))
                return RedirectToAction("LoginStudent", "Account");

            // (Tuỳ chọn) kiểm tra có phải Admin không, nếu cần
            var user = db.Users.Find(userID);
            if (user == null /* || user.Role != "Admin" */)
                return RedirectToAction("LoginStudent", "Account");

            // Fill thông tin header (bạn đã có trong OnActionExecuting nên đoạn dưới thực ra không bắt buộc)
            ViewBag.Username = user.Username;
            ViewBag.FullName = user.FullName;
            ViewBag.Email = user.Email;

            var payments = db.Payments
                .Include(p => p.Registration.User)
                .Include(p => p.Registration.Room)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                payments = payments.Where(p => p.Registration.User.FullName.Contains(search));

            if (!string.IsNullOrWhiteSpace(type))
                payments = payments.Where(p => p.Type == type);

            if (!string.IsNullOrWhiteSpace(status))
                payments = payments.Where(p => p.Status == status);

            if (date.HasValue)
                payments = payments.Where(p => DbFunctions.TruncateTime(p.PaymentDate) == date.Value.Date);

            return View(payments.OrderByDescending(p => p.PaymentDate).ToList());
        }


        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            Payment payment = db.Payments.Find(id);
            if (payment == null) return HttpNotFound();
            return View(payment);
        }

        // GET: PaymentAdmin/Create
        public ActionResult Create()
        {
            ViewBag.RegID = new SelectList(
                db.Registrations.Include(r => r.User).Include(r => r.Room)
                .Select(r => new
                {
                    RegID = r.RegID,
                    DisplayText = r.User.FullName + " - Phòng " + r.Room.RoomNumber + " (" + r.Room.Building + ")"
                }).ToList(),
                "RegID", "DisplayText"
            );

            return View();
        }

        // POST: PaymentAdmin/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "PaymentID,RegID,Amount,Type,PaymentDate,Status")] Payment payment)
        {
            // 1. Validate theo CHECK constraint trong DB
            var validTypes = new[] { "Rent", "Electricity", "Water", "Other" };
            var validStatuses = new[] { "Paid", "Unpaid", "Overdue", "owe" };

            if (!validTypes.Contains(payment.Type))
                ModelState.AddModelError("Type", "Loại thanh toán không hợp lệ.");

            if (!validStatuses.Contains(payment.Status))
                ModelState.AddModelError("Status", "Trạng thái không hợp lệ.");

            if (payment.Amount < 0)
                ModelState.AddModelError("Amount", "Số tiền phải >= 0.");

            if (!payment.PaymentDate.HasValue)
                payment.PaymentDate = DateTime.Now;   // nếu để trống thì tự lấy ngày hôm nay

            if (ModelState.IsValid)
            {
                try
                {
                    // 2. Lưu hóa đơn vào bảng Payments
                    db.Payments.Add(payment);
                    db.SaveChanges();   // lúc này PaymentID đã có

                    // 3. Lấy thông tin đăng ký để biết sinh viên + phòng
                    var reg = db.Registrations
                                .Include(r => r.User)
                                .Include(r => r.Room)
                                .FirstOrDefault(r => r.RegID == payment.RegID);

                    // 4. Tạo thông báo cho đúng sinh viên
                    if (reg != null && reg.User != null)
                    {
                        var noti = new Notification
                        {
                            Title = "Hóa đơn ký túc xá mới",
                            Content = string.Format(
                                "Bạn có hóa đơn {0} phòng {1} tòa {2}, số tiền {3:N0} VND, ngày thanh toán {4}.",
                                payment.Type,
                                reg.Room?.RoomNumber,
                                reg.Room?.Building,
                                payment.Amount,
                                payment.PaymentDate.Value.ToString("dd/MM/yyyy")
                            ),
                            CreatedAt = DateTime.Now,
                            TargetRole = "Student",

                            // 3 cột mới của bảng Notifications
                            UserID = reg.UserID,       // gửi cho đúng sinh viên
                            IsRead = false,            // mặc định chưa đọc
                            Url = "/ThanhToan/Index"   // khi bấm thông báo sẽ dẫn tới trang thanh toán
                        };

                        db.Notifications.Add(noti);
                        db.SaveChanges();
                    }

                    // 5. Thông báo lại cho admin
                    TempData["SuccessMessage"] = "Lưu hóa đơn và gửi thông báo cho sinh viên thành công.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Không lưu được hóa đơn. Lý do: " + ex.Message);
                }
            }

            // Nếu có lỗi, load lại dropdown RegID và trả về view
            ViewBag.RegID = new SelectList(
                db.Registrations.Include(r => r.User).Include(r => r.Room)
                .Select(r => new
                {
                    RegID = r.RegID,
                    DisplayText = r.User.FullName + " - Phòng " + r.Room.RoomNumber + " (" + r.Room.Building + ")"
                }).ToList(),
                "RegID", "DisplayText", payment.RegID
            );

            return View(payment);
        }


        // GET: PaymentAdmin/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var payment = db.Payments.Find(id);
            if (payment == null) return HttpNotFound();

            ViewBag.RegID = new SelectList(
                db.Registrations.Include(r => r.User).Include(r => r.Room)
                .Select(r => new
                {
                    RegID = r.RegID,
                    DisplayText = r.User.FullName + " - Phòng " + r.Room.RoomNumber + " (" + r.Room.Building + ")"
                }).ToList(),
                "RegID", "DisplayText", payment.RegID
            );

            return View(payment);
        }

        // POST: PaymentAdmin/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "PaymentID,RegID,Amount,Type,PaymentDate,Status")] Payment payment)
        {
            if (ModelState.IsValid)
            {
                db.Entry(payment).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.RegID = new SelectList(db.Registrations, "RegID", "RegID", payment.RegID);
            return View(payment);
        }

        // GET: PaymentAdmin/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            Payment payment = db.Payments.Find(id);
            if (payment == null) return HttpNotFound();
            return View(payment);
        }

        // POST: PaymentAdmin/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Payment payment = db.Payments.Find(id);
            db.Payments.Remove(payment);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
