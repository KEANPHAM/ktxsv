using KTXSV.Models;
using KTXSV.Services;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace KTXSV.Controllers.Admin
{
    public class PaymentAdminController : Controller
    {

        private readonly AdminNotificationService _adminNotificationService;
        private readonly StudentNotificationService _studentNotificationService;
        KTXSVEntities db = new KTXSVEntities();

        public PaymentAdminController()
        {
            _adminNotificationService = new AdminNotificationService(new KTXSVEntities());
            _studentNotificationService = new StudentNotificationService(new KTXSVEntities());
        }
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            if (Session["UserID"] == null || !int.TryParse(Session["UserID"].ToString(), out int userId))
            {
                filterContext.Result = RedirectToAction("LoginStudent", "Account");
                return;
            }

            var user = db.Users.Find(userId);
            if (user == null)
            {
                filterContext.Result = RedirectToAction("LoginStudent", "Account");
                return;
            }

            ViewBag.Username = user.Username;
            ViewBag.FullName = user.FullName;
            ViewBag.Email = user.Email;
        }
        // GET: PaymentAdmin
        public ActionResult Index(string search = "", string type = "", string status = "", DateTime? date = null)
        {
            if (Session["UserID"] == null)
                return RedirectToAction("LoginStudent", "Account");

            if (!int.TryParse(Session["UserID"].ToString(), out int userID))
                return RedirectToAction("LoginStudent", "Account");

            var user = db.Users.Find(userID);
            if (user == null)
                return RedirectToAction("LoginStudent", "Account");

            ViewBag.Username = user.Username;
            ViewBag.FullName = user.FullName;
            ViewBag.Email = user.Email;

            var payments = db.Payments
                .Include(p => p.Registration.User)
                .Include(p => p.Registration.Room)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                payments = payments.Where(p => p.Registration != null && p.Registration.User != null &&
                                               p.Registration.User.FullName.Contains(search));

            if (!string.IsNullOrWhiteSpace(type))
                payments = payments.Where(p => p.Type == type);

            if (!string.IsNullOrWhiteSpace(status))
                payments = payments.Where(p => p.Status == status);

            if (date.HasValue)
                payments = payments.Where(p => p.PaymentDate.HasValue &&
                                               DbFunctions.TruncateTime(p.PaymentDate) == date.Value.Date);

            var result = payments.OrderByDescending(p => p.PaymentDate).ToList();
            return View(result);
        }

        // GET: PaymentAdmin/DetailsEdit/5
        [HttpGet]
        public ActionResult DetailsEdit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var payment = db.Payments.Include(p => p.Registration.User)
                                     .Include(p => p.Registration.Room)
                                     .FirstOrDefault(p => p.PaymentID == id);
            if (payment == null) return HttpNotFound();

            var reg = payment.Registration;
            if (reg != null)
                ViewBag.RegDisplay = reg.User.FullName + " - Phòng " + reg.Room.RoomNumber + " (" + reg.Room.Building + ")";

            return View(payment);
        }

        // POST: PaymentAdmin/DetailsEdit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DetailsEdit([Bind(Include = "PaymentID,RegID,Amount,Type,Status")] Payment model)
        {
            if (!ModelState.IsValid)
            {
                var reg = db.Registrations.Include(r => r.User).Include(r => r.Room)
                                         .FirstOrDefault(r => r.RegID == model.RegID);
                if (reg != null)
                    ViewBag.RegDisplay = reg.User.FullName + " - Phòng " + reg.Room.RoomNumber + " (" + reg.Room.Building + ")";
                return View(model);
            }

            var payment = db.Payments.Find(model.PaymentID);
            if (payment == null) return HttpNotFound();

            payment.Amount = model.Amount;
            payment.Type = model.Type;
            payment.Status = model.Status;

            if (model.Status == "Paid" && !payment.PaymentDate.HasValue)
            {
                payment.PaymentDate = DateTime.Now;
            }

            db.SaveChanges();

            // Gửi thông báo đến sinh viên
            try
            {
                var reg = db.Registrations.Include(r => r.User).FirstOrDefault(r => r.RegID == payment.RegID);
                if (reg != null)
                {
                    _studentNotificationService.SendStudentNotification(
                        reg.UserID,
                        reg.RegID,
                        "PaymentReceived",
                        reg
                    );
                }
            }
            catch { }

            TempData["SuccessMessage"] = "Cập nhật hóa đơn thành công.";
            return RedirectToAction("DetailsEdit", new { id = payment.PaymentID });
        }

        // GET: PaymentAdmin/Create
        public ActionResult Create()
        {
            var list = db.Registrations
                .Include(r => r.User)
                .Include(r => r.Room)
                .Select(r => new
                {
                    RegID = r.RegID,
                    DisplayText = r.User.FullName + " - Phòng " + r.Room.RoomNumber + " (" + r.Room.Building + ")"
                }).ToList();

            ViewBag.RegID = new SelectList(list, "RegID", "DisplayText");
            return View(new Payment { Status = "Unpaid" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "RegID,Amount,Type,PaymentDate,Status")] Payment payment)
        {
            var validTypes = new[] { "Rent", "Electricity", "Water", "Other" };
            var validStatuses = new[] { "Unpaid", "Paid", "Overdue" };

            if (!validTypes.Contains(payment.Type))
                ModelState.AddModelError("Type", "Loại thanh toán không hợp lệ.");

            if (!validStatuses.Contains(payment.Status))
                ModelState.AddModelError("Status", "Trạng thái không hợp lệ.");

            if (payment.Amount < 0)
                ModelState.AddModelError("Amount", "Số tiền phải >= 0.");

            if (!payment.PaymentDate.HasValue)
                payment.PaymentDate = DateTime.Now;

            if (ModelState.IsValid)
            {
                try
                {
                    db.Payments.Add(payment);
                    db.SaveChanges();

                    var reg = db.Registrations.Include(r => r.User).Include(r => r.Room)
                                              .FirstOrDefault(r => r.RegID == payment.RegID);
                    if (reg != null)
                        _studentNotificationService.SendStudentNotification(
                            reg.UserID,
                            reg.RegID,
                            "NewPayment",
                            reg
                        );

                    TempData["SuccessMessage"] = "Hóa đơn đã được lưu và thông báo đã gửi.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Không lưu được hóa đơn: " + (ex.InnerException?.Message ?? ex.Message));
                }
            }

            var list2 = db.Registrations.Include(r => r.User).Include(r => r.Room)
                                        .Select(r => new
                                        {
                                            RegID = r.RegID,
                                            DisplayText = r.User.FullName + " - Phòng " + r.Room.RoomNumber + " (" + r.Room.Building + ")"
                                        }).ToList();
            ViewBag.RegID = new SelectList(list2, "RegID", "DisplayText", payment.RegID);
            return View(payment);
        }

        // GET: PaymentAdmin/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            Payment payment = db.Payments.Include(p => p.Registration.User)
                                         .Include(p => p.Registration.Room)
                                         .FirstOrDefault(p => p.PaymentID == id);
            if (payment == null) return HttpNotFound();
            return View(payment);
        }

        // POST: PaymentAdmin/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Payment payment = db.Payments.Find(id);
            if (payment != null)
            {
                db.Payments.Remove(payment);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }

        public JsonResult GetAmountByRegistration(int regId)
        {
            var reg = db.Registrations.Include(r => r.Room).FirstOrDefault(r => r.RegID == regId);
            if (reg != null && reg.Room != null)
            {
                return Json(new { amount = reg.Room.Price }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { amount = 0 }, JsonRequestBehavior.AllowGet);
        }

    }
}
