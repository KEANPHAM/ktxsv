using KTXSV.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace KTXSV.Controllers
{
    public class AdminSupportController : Controller
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

        // GET: danh sách tất cả yêu cầu
        public ActionResult Index()
        {
            var requests = db.SupportRequests
                             .Include(r => r.User)
                             .Include(r => r.Room)
                             .OrderByDescending(r => r.CreatedAt)
                             .ToList();
            return View(requests);
        }

        // GET: chi tiết yêu cầu
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var request = db.SupportRequests
                            .Include(r => r.User)
                            .Include(r => r.Room)
                            .FirstOrDefault(r => r.RequestID == id);

            if (request == null) return HttpNotFound();
            return View(request);
        }

        // POST: cập nhật trạng thái
        [HttpPost]
        public ActionResult UpdateStatus(int id, string status)
        {
            var req = db.SupportRequests.Find(id);
            if (req == null) return HttpNotFound();

            string[] validStatuses = { "Pending", "Resolved", "Rejected" };
            if (!string.IsNullOrWhiteSpace(status) && validStatuses.Contains(status.Trim()))
            {
                req.Status = status.Trim();
                db.SaveChanges();
                TempData["Message"] = $"✅ Cập nhật trạng thái yêu cầu #{id} thành '{status}' thành công.";
            }
            return RedirectToAction("Index");
        }

        // GET: hiển thị form phản hồi
        [HttpGet]
        public ActionResult Reply(int id)
        {
            var request = db.SupportRequests
                            .Include(r => r.User)
                            .Include(r => r.Room)
                            .FirstOrDefault(r => r.RequestID == id);

            if (request == null) return HttpNotFound();
            return View(request);
        }

        // POST: gửi phản hồi và cập nhật trạng thái
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reply(int id, string replyContent, string newStatus)
        {
            var req = db.SupportRequests.Find(id);
            if (req == null) return HttpNotFound();

            string[] validStatuses = { "Pending", "Resolved", "Rejected" };

            // Cập nhật trạng thái nếu hợp lệ
            if (!string.IsNullOrWhiteSpace(newStatus) && validStatuses.Contains(newStatus.Trim()))
            {
                req.Status = newStatus.Trim();
            }

            // Lưu phản hồi admin, giữ lịch sử và xử lý null
            if (!string.IsNullOrWhiteSpace(replyContent))
            {
                string timestampedReply = $"({DateTime.Now:dd/MM/yyyy HH:mm}) {replyContent}";
                req.Reply = string.IsNullOrWhiteSpace(req.Reply) ? timestampedReply : req.Reply + "\n" + timestampedReply;
            }

            db.SaveChanges();
            TempData["Message"] = "✅ Đã gửi phản hồi và cập nhật trạng thái thành công.";
            return RedirectToAction("Details", new { id = req.RequestID });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
