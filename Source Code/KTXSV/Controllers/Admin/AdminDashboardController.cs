using KTXSV.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
namespace KTXSV.Controllers
{
    public class DashboardController : Controller
    {
        private KTXSVEntities db = new KTXSVEntities();

        private bool IsAdmin() => Session["Role"] != null &&
                                  Session["Role"].ToString().Equals("Admin", StringComparison.OrdinalIgnoreCase);

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!IsAdmin())
            {
                filterContext.Result = RedirectToAction("Index", "Account");
                return;
            }

            ViewBag.FullName = "Admin";
            if (Session["UserID"] != null)
            {
                int userId = Convert.ToInt32(Session["UserID"]);
                var user = db.Users.Find(userId);
                if (user != null) ViewBag.FullName = user.FullName;
            }

            base.OnActionExecuting(filterContext);
        }

        public ActionResult Index()
        {
            // Sinh viên
            ViewBag.TotalStudents = db.Users.Count(u => u.Role == "Student");
            ViewBag.ActiveStudents = db.Registrations.Count(r => r.Status == "Active" || r.Status == "Expiring");
            ViewBag.PendingRegistrations = db.Registrations.Count(r => r.Status == "Pending");
            DateTime warning = DateTime.Today.AddDays(7);

            ViewBag.ExpiringContracts = db.Registrations.Count(r =>
                (r.Status == "Active" || r.Status == "Expiring") &&
                r.EndDate != null &&
                DbFunctions.TruncateTime(r.EndDate) <= warning
            );

            // Phòng
            ViewBag.TotalRooms = db.Rooms.Count();
            ViewBag.OccupiedRooms = db.Rooms.Count(r => r.Status == "Full");
            ViewBag.AvailableRooms = db.Rooms.Count(r => r.Status == "Available");
            ViewBag.MaintenanceRooms = db.Rooms.Count(r => r.Status == "Maintenance");

            // Giường
            ViewBag.TotalBeds = db.Beds.Count();
            ViewBag.UsedBeds = db.Beds.Count(b => b.IsOccupied == true);
            ViewBag.BookingBeds = db.Beds.Count(b => b.Booking == true);

            // Thanh toán
            ViewBag.TotalPayments = db.Payments.Sum(p => (decimal?)p.Amount) ?? 0;
            ViewBag.PaidPayments = db.Payments.Where(p => p.Status == "Paid").Sum(p => (decimal?)p.Amount) ?? 0;
            ViewBag.UnpaidPayments = db.Payments.Where(p => p.Status == "Unpaid" || p.Status == "Overdue" || p.Status == "owe")
                                                .Sum(p => (decimal?)p.Amount) ?? 0;

            // Hỗ trợ
            ViewBag.PendingSupport = db.SupportRequests.Count(s => s.Status == "Pending");

            return View();
        }
    }
}
