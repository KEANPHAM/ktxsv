using KTXSV.Models;  // namespace của bạn
using System;
using System.Linq;
using System.Web.Mvc;

namespace KTXSV.Controllers
{
    public class AdminRegistrationsController : Controller
    {
        private KTXSVEntities db = new KTXSVEntities();

        // Kiểm tra quyền Admin
        private bool IsAdmin() => Session["Role"] != null &&
                                   Session["Role"].ToString().Equals("Admin", StringComparison.OrdinalIgnoreCase);

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!IsAdmin())
            {
                // Redirect về trang Login nếu chưa đăng nhập hoặc không phải admin
                filterContext.Result = RedirectToAction("Index", "Account");
                return;
            }
            base.OnActionExecuting(filterContext);
        }

        // GET: AdminRegistrations
        public ActionResult Index()
        {
            var registrations = db.Registrations
                .Include("User")
                .Include("Room")
                .ToList();

            return View(registrations);
        }


        // GET: AdminRegistrations/Details/5
        public ActionResult Details(int id)
        {
            var room = db.Rooms.Find(id);
            if (room == null) return HttpNotFound();

            var currentStudents = db.Registrations
                .Where(r => r.RoomID == id && (r.Status == "Approved" || r.Status == "Active"))
                .Select(r => new {
                    r.User.FullName,
                    r.User.Phone,
                    r.StartDate,
                    r.EndDate,
                    r.Status,
                    r.RegID
                }).ToList();

            var history = db.Registrations
                .Where(r => r.RoomID == id && r.Status == "Ended")
                .OrderByDescending(r => r.EndDate)
                .Select(r => new {
                    r.User.FullName,
                    r.StartDate,
                    r.EndDate
                }).ToList();

            ViewBag.CurrentStudents = currentStudents;
            ViewBag.History = history;

            return View(room);
        }


        


        [HttpPost]
        public ActionResult Reject(int id)
        {
            var reg = db.Registrations.FirstOrDefault(r => r.RegID == id);
            if (reg == null)
                return HttpNotFound();

            reg.Status = "Rejected";

            db.SaveChanges();

            TempData["Success"] = "Từ chối đăng ký thành công.";
            return RedirectToAction("Index");
        }

    }
}
