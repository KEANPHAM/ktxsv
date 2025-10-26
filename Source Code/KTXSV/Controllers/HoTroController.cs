using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using KTXSV.Models;

namespace KTXSV.Controllers
{
    public class SupportRequestsController : Controller
    {
        KTXSVEntities db = new KTXSVEntities();

        public ActionResult Index()
        {
            if (Session["UserID"] == null)
                return RedirectToAction("LoginStudent", "Account");

            int userId = Convert.ToInt32(Session["UserID"]);

            var requests = db.SupportRequests
                             .Include(s => s.Room)
                             .Include(s => s.User)
                             .Where(s => s.UserID == userId)
                             .OrderByDescending(s => s.CreatedAt)
                             .ToList();

            return View(requests);
        }

        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var supportRequest = db.SupportRequests
                                   .Include(s => s.Room)
                                   .Include(s => s.User)
                                   .FirstOrDefault(s => s.RequestID == id);

            if (supportRequest == null)
                return HttpNotFound();

            return View(supportRequest);
        }

        public ActionResult Create()
        {
            if (Session["UserID"] == null)
                return RedirectToAction("LoginStudent", "Account");

            int userId = Convert.ToInt32(Session["UserID"]);

            var current = db.Registrations
                .Where(r => r.UserID == userId && r.Status == "Active")
                .Select(r => new
                {
                    r.RoomID,
                    r.Room.RoomNumber
                })
                .FirstOrDefault();

            ViewBag.CurrentRoomNumber = current?.RoomNumber ?? "Chưa có";
            var model = new SupportRequest { RoomID = current?.RoomID };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Title,Description")] SupportRequest supportRequest)
        {
            try
            {
                if (Session["UserID"] == null)
                    return RedirectToAction("LoginStudent", "Account");

                int userId = Convert.ToInt32(Session["UserID"]);

                var currentRoomId = db.Registrations
                    .Where(r => r.UserID == userId && r.Status == "Active")
                    .Select(r => r.RoomID)
                    .FirstOrDefault();

                supportRequest.UserID = userId;
                supportRequest.RoomID = currentRoomId;
                supportRequest.CreatedAt = DateTime.Now;
                supportRequest.Status = "Pending";

                if (ModelState.IsValid)
                {
                    db.SupportRequests.Add(supportRequest);
                    db.SaveChanges();
                    TempData["Message"] = "Gửi yêu cầu hỗ trợ thành công!";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi khi lưu dữ liệu: " + ex.Message);
            }

            return View(supportRequest);
        }

        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var supportRequest = db.SupportRequests.Find(id);
            if (supportRequest == null)
                return HttpNotFound();

            ViewBag.RoomID = new SelectList(db.Rooms, "RoomID", "RoomNumber", supportRequest.RoomID);
            return View(supportRequest);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "RequestID,RoomID,Title,Description,Status,CreatedAt,UserID")] SupportRequest supportRequest)
        {
            if (ModelState.IsValid)
            {
                db.Entry(supportRequest).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.RoomID = new SelectList(db.Rooms, "RoomID", "RoomNumber", supportRequest.RoomID);
            return View(supportRequest);
        }

        public ActionResult Delete(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var supportRequest = db.SupportRequests.Find(id);
            if (supportRequest == null)
                return HttpNotFound();

            return View(supportRequest);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var supportRequest = db.SupportRequests.Find(id);
            db.SupportRequests.Remove(supportRequest);
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
