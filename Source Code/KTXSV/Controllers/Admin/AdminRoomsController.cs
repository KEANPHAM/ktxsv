using KTXSV.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using System.Web.Services.Description;

namespace KTXSV.Controllers
{
    public class AdminRoomsController : Controller
    {

        private KTXSVEntities db = new KTXSVEntities();

        // Kiểm tra  Admin
        private bool IsAdmin() => Session["Role"] != null &&
                                   Session["Role"].ToString().Equals("Admin", StringComparison.OrdinalIgnoreCase);

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!IsAdmin())
            {
                filterContext.Result = RedirectToAction("Index", "Account");
                return;
            }
                if (Session["UserID"] != null)
                {
                    int currentUserId = Convert.ToInt32(Session["UserID"]);
                    var currentUser = db.Users.Find(currentUserId);
                    ViewBag.FullName = currentUser != null ? currentUser.FullName : "Admin";
                }
                else
                {
                    ViewBag.FullName = "Admin";
                }
            
            base.OnActionExecuting(filterContext);
        }
        public ActionResult Index()
        {
           

            var rooms = db.Rooms.AsQueryable();

            string building = Request.QueryString["building"];
            string status = Request.QueryString["status"];
            string capacity = Request.QueryString["capacity"];

            if (!string.IsNullOrEmpty(building))
                rooms = rooms.Where(r => r.Building == building);

            if (!string.IsNullOrEmpty(status))
                rooms = rooms.Where(r => r.Status == status);
            if (!string.IsNullOrEmpty(capacity))
            {
                if (int.TryParse(capacity, out int capacityValue))
                {
                    rooms = rooms.Where(r => r.Capacity == capacityValue);
                }
            }
            rooms = rooms.OrderBy(r => r.RoomNumber);

            return View(rooms.ToList());
        }

        public ActionResult Details(int id)
        {
            var room = db.Rooms.Find(id);
            if (room == null) return HttpNotFound();

            var currentStudents = db.Registrations
                .Where(r => r.RoomID == id && (r.Status == "Approved" || r.Status == "Active"))
                .Include(r => r.User)
                .ToList();

            var history = db.Registrations
                .Where(r => r.RoomID == id && (r.Status == "Ended" || r.Status== "Transferred"))
                .Include(r => r.User)
                .OrderByDescending(r => r.EndDate)
                .ToList();

            ViewBag.CurrentStudents = currentStudents;
            ViewBag.History = history;

            return View(room);
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Room room)
        {
            if (ModelState.IsValid)
            {
                bool exists = db.Rooms.Any(r => r.Building == room.Building && r.RoomNumber == room.RoomNumber);
                if (exists)
                {
                    ViewBag.Error = "Phòng đã tồn tại!";
                    return View(room);
                }

                room.Occupied = 0;
                db.Rooms.Add(room);
                db.SaveChanges();
                TempData["Success"] = "Thêm phòng thành công";
                return RedirectToAction("Index");
            }
            return View(room);
        }
        public ActionResult Edit(int id)
        {
            var room = db.Rooms.Find(id);
            if (room == null) return HttpNotFound();
            return View(room);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Room room)
        {
            if (ModelState.IsValid)
            {
                db.Entry(room).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(room);
        }

        public ActionResult Delete(int id)
        {
            var room = db.Rooms.Find(id);
            if (room == null) return HttpNotFound();
            return View(room);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var room = db.Rooms.Find(id);
            if (room == null) return HttpNotFound();

            bool hasStudents = db.Registrations.Any(r => r.RoomID == id &&
                (r.Status == "Approved" || r.Status == "Active"));

            if (hasStudents)
            {
                TempData["Error"] = "Phòng đang có sinh viên ở, không thể xóa!";
                return RedirectToAction("Index");
            }

            db.Rooms.Remove(room);
            db.SaveChanges();
            return RedirectToAction("Index");
        }
        [HttpGet]
        public JsonResult GetAvailableBeds(int roomId)
        {
            var beds = db.Beds
                .Where(b => b.RoomID == roomId && b.IsOccupied == false)
                .Select(b => new { b.BedID, b.BedNumber })
                .ToList();

            return Json(beds, JsonRequestBehavior.AllowGet);
        }
        public ActionResult TransferRoom(int id)
        {
            var registration = db.Registrations.Include("User").Include("Room").FirstOrDefault(r => r.RegID == id);
            if (registration == null)
                return HttpNotFound();

            // Danh sách phòng
            ViewBag.RoomID = new SelectList(db.Rooms, "RoomID", "RoomNumber");

            // Lấy giường trống theo phòng hiện tại (ban đầu hiển thị giường của phòng hiện tại)
            var availableBeds = db.Beds
                .Where(b => b.RoomID == registration.RoomID && b.IsOccupied == false)
                .ToList();
            ViewBag.BedID = new SelectList(availableBeds, "BedID", "BedNumber");

            return View(registration);
        }

        [HttpPost]
        public ActionResult TransferRoom(int id, int newRoomID, int newBedID, string note)
        {
            var registration = db.Registrations.FirstOrDefault(r => r.RegID == id && r.Status == "Active");
            if (registration == null)
            {
                return HttpNotFound();
            }

            registration.EndDate = DateTime.Now;
            registration.Status = "Transferred";
            registration.Note = note;
            db.Entry(registration).State = EntityState.Modified;


            var room = db.Rooms.FirstOrDefault(r => r.RoomID == registration.RoomID);
            if (room != null)
            {
                if (room.Status == "Full" || room.Status == "full")
                    room.Status = "Available";
                if (room.Occupied > 0)
                    room.Occupied--;
            }
            var oldBed = db.Beds.FirstOrDefault(b => b.BedID == registration.BedID);
            if (oldBed != null) oldBed.IsOccupied = false;

            
            var newReg = new Registration
            {
                UserID = registration.UserID,
                RoomID = newRoomID,
                BedID = newBedID,
                StartDate = DateTime.Now,
                Status = "Active",
                Note = note
            };


            db.Registrations.Add(newReg);

            var newRoom = db.Rooms.FirstOrDefault(r => r.RoomID == newRoomID);
            newRoom.Occupied++;
            if (newRoom.Capacity == newRoom.Occupied)
                newRoom.Status = "Full";
            var newBed = db.Beds.FirstOrDefault(b => b.BedID == newBedID);
            if (newBed != null) newBed.IsOccupied = true;

            try
            {
                db.SaveChanges();
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException ex)
            {
                var inner = ex.InnerException?.InnerException?.Message;
                throw new Exception("Lỗi khi cập nhật CSDL: " + inner);
            }

            TempData["Success"] = "Chuyển phòng thành công!";
            return RedirectToAction("Index");
        }

    }
}
