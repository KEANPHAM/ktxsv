using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using KTXSV.Models;

namespace KTXSV.Controllers.Admin
{
    public class PaymentAdminController : Controller
    {
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

        // GET: PaymentAdmin
        public ActionResult Index(string search, string type, string status, DateTime? date)
        {
            var payments = db.Payments
                .Include(p => p.Registration.User)
                .Include(p => p.Registration.Room)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                payments = payments.Where(p => p.Registration.User.FullName.Contains(search));
            }

            if (!string.IsNullOrEmpty(type))
            {
                payments = payments.Where(p => p.Type == type);
            }

            if (!string.IsNullOrEmpty(status))
            {
                payments = payments.Where(p => p.Status == status);
            }

            if (date.HasValue)
            {
                payments = payments.Where(p => DbFunctions.TruncateTime(p.PaymentDate) == date.Value.Date);
            }

            return View(payments.OrderByDescending(p => p.PaymentDate).ToList());
        }

        // GET: PaymentAdmin/Details/5
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
            if (ModelState.IsValid)
            {
                db.Payments.Add(payment);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.RegID = new SelectList(db.Registrations, "RegID", "RegID", payment.RegID);
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
