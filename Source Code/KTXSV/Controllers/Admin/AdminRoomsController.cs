using KTXSV.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace KTXSV.Controllers
{
    public class AdminRoomsController : Controller
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

            if (!string.IsNullOrEmpty(capacity) && int.TryParse(capacity, out int capacityValue))
                rooms = rooms.Where(r => r.Capacity == capacityValue);

            rooms = rooms.OrderBy(r => r.RoomNumber);

            return View(rooms.ToList());
        }

        public ActionResult Details(int id)
        {
            var room = db.Rooms.Find(id);
            if (room == null) return HttpNotFound();

            // Sinh viên đang ở (Active hoặc Approved)
            var currentStudents = db.Registrations
                .Where(r => r.RoomID == id && (r.Status == "Approved" || r.Status == "Active"))
                .Include(r => r.User)
                .Include(r => r.Bed)
                .ToList();

            // Lịch sử đăng ký
            var history = db.Registrations
                .Where(r => r.RoomID == id && (r.Status == "Ended" || r.Status == "Transferred"))
                .Include(r => r.User)
                .Include(r => r.Bed)
                .OrderByDescending(r => r.EndDate)
                .ToList();

            ViewBag.CurrentStudents = currentStudents;
            ViewBag.History = history;

            return View(room);
        }

        public ActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Room room)
        {
            if (!ModelState.IsValid) return View(room);

            if (db.Rooms.Any(r => r.Building == room.Building && r.RoomNumber == room.RoomNumber))
            {
                ViewBag.Error = "Phòng đã tồn tại!";
                return View(room);
            }

            room.Occupied = 0;
            room.Status = "Available";
            db.Rooms.Add(room);
            db.SaveChanges();

            TempData["Success"] = "Thêm phòng thành công";
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int id)
        {
            var room = db.Rooms.Find(id);
            if (room == null) return HttpNotFound();

            room.Occupied = room.Beds.Count(b => b.IsOccupied == true);
            ViewBag.MaxCapacity = room.MaxCapacity;
            return View(room);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Room room)
        {
            if (!ModelState.IsValid) return View(room);

            if (db.Rooms.Any(r => r.RoomID != room.RoomID && r.Building == room.Building && r.RoomNumber == room.RoomNumber))
            {
                ViewBag.Error = "Phòng đã tồn tại!";
                return View(room);
            }

            var dbRoom = db.Rooms.Include("Beds").FirstOrDefault(r => r.RoomID == room.RoomID);
            if (dbRoom == null) return HttpNotFound();

            int occupiedBeds = dbRoom.Beds.Count(b => b.IsOccupied == true);

            if (room.Capacity > dbRoom.MaxCapacity)
            {
                ViewBag.Error = $"Không thể tăng số giường quá sức chứa tối đa ({dbRoom.MaxCapacity})!";
                return View(dbRoom);
            }

            if (room.Capacity < occupiedBeds)
            {
                ViewBag.Error = $"Không thể giảm số giường. Hiện tại có {occupiedBeds} sinh viên đang ở.";
                return View(dbRoom);
            }

            int oldCapacity = dbRoom.Capacity;
            int newCapacity = room.Capacity;

            if (newCapacity < oldCapacity)
            {
                int needDisable = oldCapacity - newCapacity;
                var bedsToDisable = dbRoom.Beds
                    .Where(b => b.IsOccupied != true && b.IsActive)
                    .OrderByDescending(b => b.BedNumber)
                    .Take(needDisable)
                    .ToList();

                if (bedsToDisable.Count < needDisable)
                {
                    ViewBag.Error = "Không thể giảm số giường do số lượng giường trống ít hơn số giường cần giảm.";
                    return View(dbRoom);
                }

                foreach (var bed in bedsToDisable) bed.IsActive = false;
            }
            else if (newCapacity > oldCapacity)
            {
                int needActivate = newCapacity - oldCapacity;
                var bedsToActivate = dbRoom.Beds
                    .Where(b => !b.IsActive)
                    .OrderBy(b => b.BedNumber)
                    .Take(needActivate)
                    .ToList();

                foreach (var bed in bedsToActivate) bed.IsActive = true;
            }

            dbRoom.RoomNumber = room.RoomNumber;
            dbRoom.Building = room.Building;
            dbRoom.Capacity = newCapacity;

            occupiedBeds = dbRoom.Beds.Count(b => b.IsOccupied == true);
            dbRoom.Status = occupiedBeds >= newCapacity ? "Full" : "Available";

            db.Entry(dbRoom).State = EntityState.Modified;
            db.SaveChanges();

            TempData["Success"] = "Cập nhật phòng thành công";
            return RedirectToAction("Index");
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

            bool hasStudents = db.Registrations.Any(r => r.RoomID == id && (r.Status == "Approved" || r.Status == "Active"));
            if (hasStudents)
            {
                TempData["Error"] = "Phòng đang có sinh viên ở, không thể xóa!";
                return RedirectToAction("Index");
            }

            db.Rooms.Remove(room);
            db.SaveChanges();

            TempData["Success"] = "Xóa phòng thành công";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult DeleteBed(int bedId)
        {
            var bed = db.Beds.Find(bedId);
            if (bed == null) return HttpNotFound();

            if (bed.IsOccupied.GetValueOrDefault())
            {
                TempData["Error"] = "Giường hiện đang có sinh viên, không thể xóa!";
                return RedirectToAction("Details", new { id = bed.RoomID });
            }

            var room = db.Rooms.Find(bed.RoomID);
            if (room != null) room.Capacity--;

            db.Beds.Remove(bed);
            db.SaveChanges();

            TempData["Success"] = "Xóa giường thành công";
            return RedirectToAction("Details", new { id = bed.RoomID });
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

            // 1. Kết thúc đăng ký hiện tại bằng EndDate cũ hoặc EndDate của đăng ký mới
            registration.Status = "Transferred";
            registration.Note = note;

            db.Entry(registration).State = EntityState.Modified;

            // 2. Cập nhật phòng và giường cũ
            var oldRoom = db.Rooms.FirstOrDefault(r => r.RoomID == registration.RoomID);
            if (oldRoom != null)
            {
                if (oldRoom.Occupied > 0)
                    oldRoom.Occupied--;

                if (oldRoom.Occupied < oldRoom.Capacity)
                    oldRoom.Status = "Available";
            }

            var oldBed = db.Beds.FirstOrDefault(b => b.BedID == registration.BedID);
            if (oldBed != null)
                oldBed.IsOccupied = false;

            // 3. Tạo đăng ký mới cho phòng mới
            var newReg = new Registration
            {
                UserID = registration.UserID,
                RoomID = newRoomID,
                BedID = newBedID,
                StartDate = DateTime.Now,
                EndDate = registration.EndDate.Value, // dùng EndDate cũ nếu có
                Status = "Active",
                Note = note
            };
            db.Registrations.Add(newReg);
            registration.EndDate = DateTime.Now;

            // 4. Cập nhật phòng và giường mới
            var newRoom = db.Rooms.FirstOrDefault(r => r.RoomID == newRoomID);
            if (newRoom != null)
            {
                newRoom.Occupied++;
                if (newRoom.Occupied >= newRoom.Capacity)
                    newRoom.Status = "Full";
            }

            var newBed = db.Beds.FirstOrDefault(b => b.BedID == newBedID);
            if (newBed != null)
                newBed.IsOccupied = true;

            // 5. Lưu thay đổi
            try
            {
                db.SaveChanges();
                TempData["Success"] = "Chuyển phòng thành công!";
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException ex)
            {
                var inner = ex.InnerException?.InnerException?.Message;
                throw new Exception("Lỗi khi cập nhật CSDL: " + inner);
            }

            return RedirectToAction("Index");

        }
        // POST: AdminRooms/ReleaseBed
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ReleaseBed(int bedID)
        {
            var bed = db.Beds.Find(bedID);
            if (bed == null) return HttpNotFound();

            // Mở giường cho đăng ký trước
            bed.Booking = true; // kiểu bool? trong model
            db.Entry(bed).State = EntityState.Modified;
            db.SaveChanges();

            TempData["Success"] = "Giường đã được mở cho đăng ký.";
            return RedirectToAction("Details", new { id = bed.RoomID });
        }
    }
}
