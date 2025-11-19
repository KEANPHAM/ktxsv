using KTXSV.Models;
using KTXSV.Services;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace KTXSV.Controllers
{
    public class AdminRoomsController : Controller
    {
        private readonly AdminNotificationService _adminNotificationService;
        private readonly StudentNotificationService _studentNotificationService;

        public AdminRoomsController()
        {
            _adminNotificationService = new AdminNotificationService(new KTXSVEntities());
            _studentNotificationService = new StudentNotificationService(new KTXSVEntities());
        }
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
                .Where(r => r.RoomID == id && (r.Status == "Approved" || r.Status == "Active" || r.Status == "Expiring"))
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
            int roomId = room.RoomID;

            for (int i = 1; i <= room.Capacity; i++)
            {
                Bed newBed = new Bed
                {
                    RoomID = roomId,
                    BedNumber = i,
                    IsOccupied = false,
                    Booking = true,
                    IsActive = true 
                };
                db.Beds.Add(newBed);
            }
            db.SaveChanges();

            _adminNotificationService.SendAdminNotification("RoomCreated", room);
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
            _adminNotificationService.SendAdminNotification("RoomUpdated", dbRoom);
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
            _adminNotificationService.SendAdminNotification("RoomDeleted", room);
            TempData["Success"] = "Xóa phòng thành công";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult DeleteBed(int bedId)
        {
            var bed = db.Beds.Include(b => b.Room).FirstOrDefault(b => b.BedID == bedId);

            if (bed == null) return HttpNotFound();

            if (bed.IsOccupied.GetValueOrDefault())
            {
                TempData["Error"] = "Giường hiện đang có sinh viên, không thể xóa!";
                return RedirectToAction("Details", new { id = bed.RoomID });
            }

            var room = bed.Room;
            int? roomId = bed.RoomID;

            var bedInfo = new
            {
                RoomNumber = room?.RoomNumber,
                BedNumber = bed.BedNumber,
                Building = room?.Building
            };

            if (room != null)
            {
                room.Capacity = Math.Max(room.Capacity - 1, 0);
                room.Status = (room.Occupied >= room.Capacity) ? "Full" : "Available";
                db.Entry(room).State = EntityState.Modified;
            }

            db.Beds.Remove(bed);
            db.SaveChanges();

            _adminNotificationService.SendAdminNotification("BedDeleted", bedInfo.RoomNumber, bedInfo.BedNumber, bedInfo.Building);

            TempData["Success"] = $"Đã xóa giường {bedInfo.BedNumber} của phòng {bedInfo.RoomNumber} thành công.";
            return RedirectToAction("Details", new { id = roomId });
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

            ViewBag.RoomID = new SelectList(db.Rooms, "RoomID", "RoomNumber");

            var availableBeds = db.Beds
                .Where(b => b.RoomID == registration.RoomID && b.IsOccupied == false)
                .ToList();
            ViewBag.BedID = new SelectList(availableBeds, "BedID", "BedNumber");

            return View(registration);
        }

        [HttpPost]
        public ActionResult TransferRoom(int id, int newRoomID, int newBedID, string note)
        {
            // Lấy hợp đồng hiện tại đang Active
            var oldReg = db.Registrations
                .Include(r => r.User)
                .Include(r => r.Room)
                .Include(r => r.Bed)
                .FirstOrDefault(r => r.RegID == id && r.Status == "Active");

            if (oldReg == null)
            {
                TempData["Error"] = "Không tìm thấy hợp đồng đang hoạt động.";
                return RedirectToAction("Index");
            }

            // Cập nhật hợp đồng cũ
            oldReg.Status = "Transferred";
            oldReg.Note = note;

            if (oldReg.Bed != null)
            {
                oldReg.Bed.Booking = true;
                oldReg.Bed.IsOccupied = false;
            }

            var oldRoom = oldReg.Room;
            if (oldRoom != null)
            {
                if (oldRoom.Occupied > 0)
                    oldRoom.Occupied--;

                if (oldRoom.Occupied < oldRoom.Capacity)
                    oldRoom.Status = "Available";
            }

            oldReg.EndDate = DateTime.Now;

            // Tạo hợp đồng mới cho phòng mới
            var newReg = new Registration
            {
                UserID = oldReg.UserID,
                RoomID = newRoomID,
                BedID = newBedID,
                StartDate = DateTime.Now,
                EndDate = oldReg.EndDate, // EndDate là DateTime non-nullable
                Status = "Active",
                Note = note
            };
            db.Registrations.Add(newReg);

            var newRoom = db.Rooms.FirstOrDefault(r => r.RoomID == newRoomID);
            if (newRoom != null)
            {
                newRoom.Occupied++;
                if (newRoom.Occupied >= newRoom.Capacity)
                    newRoom.Status = "Full";
            }

            var newBed = db.Beds.FirstOrDefault(b => b.BedID == newBedID);
            if (newBed != null)
            {
                newBed.IsOccupied = true;
                newBed.Booking = false;
            }

            try
            {
                db.SaveChanges();

                // Gửi thông báo cho sinh viên
                _studentNotificationService.SendStudentNotification(oldReg.UserID, oldReg.RegID, "Transferred", oldReg);

                // Gửi thông báo cho Admin
                _adminNotificationService.SendAdminNotification("Transferred", newReg);

                TempData["Success"] = "Chuyển phòng thành công!";
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException ex)
            {
                var inner = ex.InnerException?.InnerException?.Message;
                throw new Exception("Lỗi khi cập nhật CSDL: " + inner);
            }

            return RedirectToAction("Index");
        }

        // mở giường cho phép đăng ký
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ReleaseBed(int bedID)
        {
            var bed = db.Beds.Find(bedID);
            if (bed == null) return HttpNotFound();

            bed.Booking = true; 
            db.Entry(bed).State = EntityState.Modified;
            db.SaveChanges();

            TempData["Success"] = "Giường đã được mở cho đăng ký.";
            return RedirectToAction("Details", new { id = bed.RoomID });
        }

        //ktra sắp hết hạn
        public void CheckExpiringContracts()
        {
            DateTime today = DateTime.Today;
            DateTime warning = today.AddDays(7);

            var expiringRegs = db.Registrations
                .Where(r => r.Status == "Active" && r.EndDate != null && DbFunctions.TruncateTime(r.EndDate) <= warning)
                .Include(r => r.User)
                .Include(r => r.Room)
                .Include(r => r.Bed)
                .ToList();

            foreach (var reg in expiringRegs)
            {
                // Đồng bộ trạng thái
                reg.Status = "Expiring";

                // Đồng bộ giường: khóa đăng ký cho người khác
                if (reg.Bed != null)
                {
                    reg.Bed.IsOccupied = true;
                    reg.Bed.Booking = false;
                }

                // Kiểm tra Notification chưa gửi cho registration này hôm nay
                bool sent = db.Notifications.Any(n =>
                    n.UserID == reg.UserID &&
                    n.Title.Contains("sắp hết hạn") &&
                    DbFunctions.TruncateTime(n.CreatedAt) == today
                );

                if (!sent)
                {
                    db.Notifications.Add(new Notification
                    {
                        RegID = reg.RegID, 
                        UserID = reg.UserID,
                        Title = "Hợp đồng sắp hết hạn",
                        Content = $"Hợp đồng phòng {reg.Room.RoomNumber} sẽ hết hạn ngày {reg.EndDate:dd/MM/yyyy}. Vui lòng gia hạn hoặc liên hệ Admin.",
                        CreatedAt = DateTime.Now,
                        TargetRole = "Student",
                        IsRead = false,
                        Url = "/Phong/DanhSachPhong"
                    });
                }
            }

            db.SaveChanges();
        }


        //tự động kiểm tra hạn
        public ActionResult RunDailyTask()
        {
            CheckExpiringContracts();
            return Content("DONE");
        }


        //gia hạn
        public ActionResult Renew(int id)
        {
            var reg = db.Registrations
                .Include(r => r.User)
                .Include(r => r.Room)
                .Include(r => r.Bed)
                .FirstOrDefault(r => r.RegID == id);

            if (reg == null) return HttpNotFound();
            if (reg.Status == "Ended" || reg.Status == "Transferred")
            {
                TempData["Error"] = "Hợp đồng này đã kết thúc, không thể gia hạn!";
                return RedirectToAction("Details", new { id = reg.RoomID });
            }
            return View(reg);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Renew(int id, int months)
        {
            var reg = db.Registrations
                .Include(r => r.Room)
                .Include(r => r.User)
                .Include(r => r.Bed)
                .FirstOrDefault(r => r.RegID == id);

            if (reg == null) return HttpNotFound();
            if (reg.Status == "Ended" || reg.Status == "Transferred")
            {
                TempData["Error"] = "Hợp đồng này đã kết thúc, không thể gia hạn!";
                return RedirectToAction("Details", new { id = reg.RoomID });
            }
            DateTime oldEnd = reg.EndDate ?? DateTime.Now;
            DateTime startDate = new DateTime(oldEnd.Year, oldEnd.Month, 1).AddMonths(1);

            DateTime endDate = startDate.AddMonths(months).AddDays(-1);
            reg.EndDate = endDate;

            // cap nhat lại tạng thái
            reg.Status = "Active";
            if (reg.Bed != null)
            {
                reg.Bed.IsOccupied = true;
                reg.Bed.Booking = false;
            }
            Payment p = new Payment
            {
                RegID = reg.RegID,
                Amount = reg.Room.Price * months,
                Type = "Rent",
                PaymentDate = DateTime.Now,
                Status = "Unpaid"
            };
            db.Payments.Add(p);

            _adminNotificationService.SendAdminNotification("Extended", reg);
            _studentNotificationService.SendStudentNotification(reg.UserID, reg.RegID,"Extended", reg,months);
            db.SaveChanges();
            TempData["Success"] = "Gia hạn hợp đồng thành công!";
            return RedirectToAction("Details", new { id = reg.RoomID });
        }
        public ActionResult CurrentContracts(string search = "", string roomNumber = "", string status = "")
        {
            var today = DateTime.Today;
            var warning = today.AddDays(7);

            var regs = db.Registrations
                .Include(r => r.User)
                .Include(r => r.Room)
                .Include(r => r.Bed)
                .Where(r => r.Status == "Active" || r.Status == "Expiring")
                .ToList();

            // Đồng bộ trạng thái Expiring
            foreach (var reg in regs)
            {
                if (reg.Status == "Active" && reg.EndDate.HasValue && reg.EndDate.Value <= warning)
                {
                    reg.Status = "Expiring";
                    if (reg.Bed != null)
                    {
                        reg.Bed.IsOccupied = true;
                        reg.Bed.Booking = false;
                    }
                }
            }
            db.SaveChanges();

            // Áp dụng filter tìm kiếm
            if (!string.IsNullOrEmpty(search))
                regs = regs.Where(r => r.User.FullName.Contains(search)).ToList();
            if (!string.IsNullOrEmpty(roomNumber))
                regs = regs.Where(r => r.Room.RoomNumber.Contains(roomNumber)).ToList();
            if (!string.IsNullOrEmpty(status))
                regs = regs.Where(r => r.Status == status).ToList();

            // Sắp xếp theo phòng -> giường
            regs = regs.OrderBy(r => r.Room.RoomNumber)
                       .ThenBy(r => r.Bed.BedNumber)
                       .ToList();

            return View(regs);
        }

        public ActionResult ExpiredContracts()
        {
            DateTime today = DateTime.Today;

            var expiredRegs = db.Registrations
                .Include(r => r.User)
                .Include(r => r.Room)
                .Include(r => r.Bed)
                .Where(r => (r.Status == "Active" || r.Status == "Expiring") && r.EndDate < today)
                .OrderBy(r => r.EndDate)
                .ToList();

            return View(expiredRegs);
        }
        //kết thúc hợp đồng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EndContract(int regId)
        {
            var reg = db.Registrations.Include(r => r.Bed).FirstOrDefault(r => r.RegID == regId);
            if (reg == null) return HttpNotFound();

            reg.Status = "Ended";

            if (reg.Bed != null)
            {
                reg.Bed.IsOccupied = false;
                reg.Bed.Booking = true;
            }

            db.Entry(reg).State = EntityState.Modified;
            db.SaveChanges();
            _adminNotificationService.SendAdminNotification("EndContract", reg);
            _studentNotificationService.SendStudentNotification(reg.UserID, reg.RegID, "EndContract", reg);
            TempData["Success"] = $"Hợp đồng của {reg.User.FullName} đã kết thúc và cho phép đăng ký mới tại giường {reg.Bed.BedNumber}.";
            return RedirectToAction("CurrentContracts");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SendNotification(int userId, int regId, string title, string content)
        {
            var notification = new Notification
            {
                UserID = userId,
                RegID = regId,
                Title = title,
                Content = content,
                TargetRole = "Student",
                IsRead = false,
                CreatedAt = DateTime.Now,
                Url = ""
            };


            db.Notifications.Add(notification);

            db.SaveChanges();
            notification.Url = $"/Phong/ViewNotification/{notification.NotiID}";
            db.SaveChanges();

            TempData["Success"] = "Đã gửi thông báo đến sinh viên!";
            return Redirect(Request.UrlReferrer.ToString()); // Quay về lại trang hiện tại
        }

        


    }
}
