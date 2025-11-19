//using KTXSV.Models;
//using System;

//namespace KTXSV.Services
//{
//    public class AdminNotificationService : NotificationService
//    {
//        // Giả sử có một ID Admin chung để gửi thông báo hệ thống đến, hoặc có thể là 0
//        private const int SystemAdminID = 1;

//        public AdminNotificationService(KTXSVEntities dbContext) : base(dbContext)
//        {
//        }

//        public void SendAdminNotification(string type, Registration reg)
//        {
//            string title = "";
//            string content = "";
//            string url = "/AdminRooms/PendingRegistrations"; // URL Admin để xử lý
//            string targetRole = "Admin";
//            int? userId = reg.UserID; // ID của sinh viên liên quan

//            // Lấy thông tin an toàn
//            string fullName = reg.User?.FullName ?? "Sinh viên";
//            string userIdString = reg.User?.UserID.ToString() ?? "N/A";
//            string roomNumber = reg.Room?.RoomNumber ?? "N/A";
//            string bedNumber = reg.Bed.BedNumber ?? "N/A";
//            string building = reg.Room?.Building ?? "N/A";

//            switch (type)
//            {
//                case "NewRegistration":
//                    title = "CẦN PHÊ DUYỆT: Yêu cầu Đăng ký Phòng Mới";
//                    content = $@"
//**Sinh viên {fullName} ({userIdString})** đã gửi yêu cầu đăng ký chỗ ở.<br/>
//- Phòng: <strong>{roomNumber}</strong>, Giường <strong>{bedNumber}</strong><br/>
//- Trạng thái: **Chờ phê duyệt**<br/><br/>
//Vui lòng truy cập hệ thống để xem xét và xử lý.";
//                    url = $"/AdminRooms/RegistrationDetails/{reg.RegID}";
//                    break;

//                case "Canceled":
//                    title = "CẢNH BÁO: Sinh viên Hủy Đăng ký/Hợp đồng";
//                    content = $@"
//**Sinh viên {fullName} ({userIdString})** đã **hủy** đăng ký/hợp đồng chỗ ở.<br/>
//- Giường <strong>{bedNumber}</strong> của Phòng <strong>{roomNumber}</strong> đã được mở lại.<br/>
//- Vui lòng kiểm tra lại trạng thái phòng và các vấn đề tài chính liên quan.";
//                    url = $"/AdminRooms/Details/{reg.RoomID}";
//                    break;

//                // Thêm các trường hợp thông báo cho Admin khác nếu cần (ví dụ: thanh toán bị trễ)
//                // case "LatePayment": ...

//                default:
//                    title = "THÔNG BÁO HỆ THỐNG: Thông tin KTX";
//                    content = $"Thông báo chung về {type} liên quan đến sinh viên {fullName}.";
//                    break;
//            }

//            var noti = new Notification
//            {
//                UserID = SystemAdminID, // Gửi đến Admin
//                RegID = reg.RegID,
//                Title = title,
//                Content = content,
//                CreatedAt = DateTime.Now,
//                TargetRole = targetRole,
//                IsRead = false,
//                Url = url
//            };

//            SaveNotification(noti);
//        }
//    }
//}