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
        private KTXSVEntities db = new KTXSVEntities();

        // GET: SupportRequests
        public ActionResult Index()
        {
            Session["UserID"] = 1;
            Session["UserName"] = "Kiên Phạm";
            Session["Role"] = "Student";

            var requests = db.SupportRequests.Include(s => s.User).Include(s => s.Room);
            return View(requests.ToList());
        }

        // GET: SupportRequests/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            SupportRequest supportRequest = db.SupportRequests.Find(id);
            if (supportRequest == null) return HttpNotFound();

            return View(supportRequest);
        }

        // GET: SupportRequests/Create
        // GET: SupportRequests/Create
        public ActionResult Create()
        {
            // chỉ chọn phòng thôi, user lấy từ session
            ViewBag.RoomID = new SelectList(db.Rooms, "RoomID", "RoomNumber");
            return View();
        }

        // POST: SupportRequests/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "RoomID,Title,Description")] SupportRequest supportRequest)
        {
            try
            {
                // ✅ Tạo session tạm (nếu chưa có)
                if (Session["UserID"] == null)
                {
                    Session["UserID"] = 1;
                    Session["UserName"] = "Kiên Phạm";
                    Session["Role"] = "Student";
                }

                int userId = Convert.ToInt32(Session["UserID"]);

                supportRequest.UserID = userId;
                supportRequest.CreatedAt = DateTime.Now;
                supportRequest.Status = "Pending";

                if (ModelState.IsValid)
                {
                    db.SupportRequests.Add(supportRequest);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException ex)
            {
                var inner = ex.InnerException?.InnerException;
                ModelState.AddModelError("", "Lỗi lưu CSDL: " + (inner != null ? inner.Message : ex.Message));
            }

            ViewBag.RoomID = new SelectList(db.Rooms, "RoomID", "RoomNumber", supportRequest.RoomID);
            return View(supportRequest);
        }




        // GET: SupportRequests/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            SupportRequest supportRequest = db.SupportRequests.Find(id);
            if (supportRequest == null) return HttpNotFound();

            ViewBag.UserID = new SelectList(db.Users, "UserID", "FullName", supportRequest.UserID);
            ViewBag.RoomID = new SelectList(db.Rooms, "RoomID", "RoomNumber", supportRequest.RoomID);
            return View(supportRequest);
        }

        // POST: SupportRequests/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "RequestID,UserID,RoomID,Title,Description,Status,CreatedAt")] SupportRequest supportRequest)
        {
            if (ModelState.IsValid)
            {
                db.Entry(supportRequest).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.UserID = new SelectList(db.Users, "UserID", "FullName", supportRequest.UserID);
            ViewBag.RoomID = new SelectList(db.Rooms, "RoomID", "RoomNumber", supportRequest.RoomID);
            return View(supportRequest);
        }

        // GET: SupportRequests/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            SupportRequest supportRequest = db.SupportRequests.Find(id);
            if (supportRequest == null) return HttpNotFound();

            return View(supportRequest);
        }

        // POST: SupportRequests/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            SupportRequest supportRequest = db.SupportRequests.Find(id);
            db.SupportRequests.Remove(supportRequest);
            db.SaveChanges();
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
    }
}
