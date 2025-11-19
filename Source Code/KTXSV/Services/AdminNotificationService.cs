using KTXSV.Models;
using System;

namespace KTXSV.Services
{
    public class AdminNotificationService : NotificationService
    {
        // ID của Admin hệ thống để gửi thông báo tập trung
        private const int SystemAdminID = 1;

        public AdminNotificationService(KTXSVEntities dbContext) : base(dbContext)
        {
        }

        public void SendAdminNotification(string type, string roomNumber, int? bedNumber, string building)
        {
            string title = "";
            string content = "";
            string url = "/AdminRooms/Index";
            string targetRole = "Admin";

            if (type == "BedDeleted")
            {
                title = "GIƯỜNG ĐƯỢC XÓA: Xóa Giường Khỏi Hệ Thống";
                content = $@"
Phòng Quản lý KTX xác nhận đã **xóa giường**.<br/>
- Phòng: **{roomNumber}** ({building})<br/>
- Giường bị xóa: **{bedNumber}**<br/>
- Sức chứa phòng đã được cập nhật.";
                url = $"/AdminRooms/Index";
            }
            else
            {
                title = "THÔNG BÁO HỆ THỐNG CHUNG";
                content = $"Thông báo chung về sự kiện '{type}' không liên quan đến đăng ký.";
            }

            var noti = new Notification
            {
                UserID = SystemAdminID,
                RegID = null,
                Title = title,
                Content = content,
                CreatedAt = DateTime.Now,
                TargetRole = targetRole,
                IsRead = false,
                Url = url
            };

            SaveNotification(noti);
        }
        public void SendAdminNotification(string type, Room room)
        {
            string title = "";
            string content = "";
            string url = "/AdminRooms/Index";
            string targetRole = "Admin";

            // Lấy thông tin an toàn
            string roomNumber = room?.RoomNumber ?? "N/A";
            string building = room?.Building ?? "N/A";
            string capacity = room?.Capacity.ToString() ?? "N/A";
            string roomID = room?.RoomID.ToString() ?? "N/A";

            switch (type)
            {
                case "RoomCreated":
                    title = " PHÒNG MỚI: Tạo Phòng Thành Công";
                    content = $@"
Phòng Quản lý KTX xác nhận đã tạo phòng mới thành công.<br/>
- Tên phòng: **{roomNumber}**<br/>
- Tòa nhà: <strong>{building}</strong><br/>
- Sức chứa: <strong>{capacity}</strong> giường.<br/>
Vui lòng kiểm tra danh sách phòng.";
                    url = $"/AdminRooms/Details/{roomID}";
                    break;

                case "RoomUpdated":
                    title = "PHÒNG ĐƯỢC SỬA: Cập Nhật Thông Tin Phòng";
                    content = $@"
Phòng **{roomNumber} ({building})** đã được cập nhật thông tin.<br/>
- Sức chứa mới: <strong>{capacity}</strong> giường.<br/>
- Trạng thái hiện tại: <strong>{room?.Status ?? "N/A"}</strong>.<br/>
Kiểm tra chi tiết các thay đổi về giường.";
                    url = $"/AdminRooms/Details/{roomID}";
                    break;

                case "RoomDeleted":
                    title = "PHÒNG ĐƯỢC XÓA: Xóa Phòng Khỏi Hệ Thống";
                    content = $@"
Phòng **{roomNumber} ({building})** (ID: {roomID}) đã bị **xóa** khỏi hệ thống KTX.<br/>
- Thao tác xóa được thực hiện do phòng không còn sinh viên ở.";
                    url = "/AdminRooms/Index"; // Quay về danh sách chung
                    break;

                default:
                    title = "THÔNG BÁO HỆ THỐNG CHUNG";
                    content = $"Thông báo chung về sự kiện '{type}' không liên quan đến đăng ký.";
                    break;
            }

            var noti = new Notification
            {
                UserID = SystemAdminID,
                RegID = null, // Không có RegID liên quan
                Title = title,
                Content = content,
                CreatedAt = DateTime.Now,
                TargetRole = targetRole,
                IsRead = false,
                Url = url
            };

            SaveNotification(noti);
        }
        public void SendAdminNotification(string type, Registration reg)
        {
            string title = "";
            string content = "";
            string url = "/AdminRooms/Index"; // Default URL cho Admin
            string targetRole = "Admin";

            // Lấy thông tin an toàn
            string fullName = reg.User?.FullName ?? "Sinh viên [N/A]";
            string userIdString = reg.User?.UserID.ToString() ?? "N/A";
            string roomNumber = reg.Room?.RoomNumber ?? "N/A";
            string bedNumber = reg.Bed?.BedNumber.ToString() ?? "N/A";
            string building = reg.Room?.Building ?? "N/A";

            // Lấy ngày an toàn (nếu cần)
            string startDate = reg.StartDate.ToString("dd/MM/yyyy") ?? "N/A";
            string endDate = reg.EndDate?.ToString("dd/MM/yyyy") ?? "N/A";

            switch (type)
            {
                case "EndContract":
                    title = " HỢP ĐỒNG KẾT THÚC: Chấm dứt Hợp đồng bởi quản trị viên.";
                    content = $@"
Hợp đồng của sinh viên **{fullName} ({userIdString})** tại Phòng **{roomNumber}**, Giường **{bedNumber}** đã được **chấm dứt** bởi Admin vào ngày **{DateTime.Now:dd/MM/yyyy}**.<br/>
- Giường đã được **mở** cho đăng ký mới.<br/>
- Vui lòng đảm bảo sinh viên đã hoàn tất thủ tục trả phòng.";
                    url = $"/AdminRooms/Details/{reg.RoomID}";
                    break;

                
                case "Transferred":
                    // reg ở đây là đăng ký cũ đã chuyển trạng thái thành "Transferred"
                    string oldRoomNumber = reg.Room?.RoomNumber ?? "N/A";
                    string oldBedNumber = reg.Bed?.BedNumber.ToString() ?? "N/A";
                    string oldEndDate = reg.EndDate?.ToString("dd/MM/yyyy") ?? "N/A";

                  
                    title = " THÔNG BÁO: Chuyển Phòng Đã Xảy Ra";
                    content = $@"
Sinh viên **{fullName} ({userIdString})** đã được chuyển phòng.<br/>
- Hợp đồng cũ (Phòng <strong>{oldRoomNumber}</strong>, Giường <strong>{oldBedNumber}</strong>) đã kết thúc/chuyển giao vào ngày **{DateTime.Now:dd/MM/yyyy}**.<br/>
- **Chỗ ở mới:** Một đăng ký mới đã được tạo cho sinh viên này (RegID mới: [RegID mới nếu biết]).<br/>
- Vui lòng kiểm tra phòng mới và thu tiền chuyển phòng (nếu có).";
                    url = $"/AdminRooms/Details/{reg.RoomID}"; // Quay về phòng cũ để Admin kiểm tra

                    break;
                case "NewRegistration":
                    title = "CẦN PHÊ DUYỆT: Yêu cầu Đăng ký Phòng Mới";
                    content = $@"
Sinh viên **{fullName} ({userIdString})** đã gửi yêu cầu đăng ký chỗ ở.<br/>
- Phòng: <strong>{roomNumber}</strong>, Giường <strong>{bedNumber}</strong><br/>
- Thời gian: {startDate} - {endDate}<br/>
- Trạng thái: **Chờ phê duyệt**<br/><br/>
Vui lòng truy cập trang Quản lý Đăng ký để xử lý.";
                    url = $"/AdminRooms/RegistrationDetails/{reg.RegID}";
                    break;

                case "Canceled":
                    title = "CẢNH BÁO: Sinh viên Hủy Đăng ký/Hợp đồng";
                    content = $@"
Sinh viên **{fullName} ({userIdString})** đã **hủy** đăng ký/hợp đồng chỗ ở.<br/>
- Phòng: <strong>{roomNumber}</strong>, Giường <strong>{bedNumber}</strong><br/>
- Trạng thái: Đã Hủy. Giường đã được **mở lại** cho đăng ký mới.<br/>
- Vui lòng kiểm tra các khoản phí bồi thường (nếu có).";
                    url = $"/AdminRooms/Details/{reg.RoomID}";
                    break;

                case "Extended":
                    title = " XÁC NHẬN: Sinh viên Gia hạn Hợp đồng";
                    content = $@"
Sinh viên **{fullName} ({userIdString})** đã **gia hạn hợp đồng** thành công.<br/>
- Phòng: <strong>{roomNumber}</strong>, Giường <strong>{bedNumber}</strong><br/>
- Hạn mới: <strong>{endDate}</strong><br/>
- Hóa đơn gia hạn đã được tạo, cần theo dõi thanh toán.";
                    url = $"/AdminRooms/CurrentContracts";
                    break;

                case "Approved":
                    title = " THÔNG BÁO: Yêu cầu Đăng ký Đã được Duyệt";
                    content = $@"
Yêu cầu đăng ký phòng của sinh viên **{fullName} ({userIdString})** đã được **phê duyệt**.<br/>
- Phòng: <strong>{roomNumber}</strong>, Giường <strong>{bedNumber}</strong><br/>
- Sinh viên cần hoàn tất thanh toán trước khi vào ở.";
                    url = $"/AdminRooms/CurrentContracts";
                    break;

                case "Rejected":
                    title = " THÔNG BÁO: Yêu cầu Đăng ký Đã bị Từ chối";
                    content = $@"
Yêu cầu đăng ký phòng của sinh viên **{fullName} ({userIdString})** đã **bị từ chối**.<br/>
- Vui lòng đảm bảo các thông báo từ chối đã được gửi đến sinh viên liên quan.";
                    url = $"/AdminRooms/PendingRegistrations";
                    break;

                case "PaymentReceived":
                    title = " THANH TOÁN: Sinh viên Đã đóng tiền KTX";
                    // Giả sử có Payment Object hoặc thông tin payment đi kaèm
                    content = $@"
Hệ thống ghi nhận sinh viên **{fullName} ({userIdString})** đã đóng tiền.<br/>
- Số tiền: [Amount] (Chèn số tiền nếu có)<br/>
- Vui lòng kiểm tra và xác nhận hóa đơn trên hệ thống.";
                    url = $"/AdminPayment/Details/{reg.RegID}";
                    break;

                case "Expiring":
                    title = " CẢNH BÁO: Hợp đồng sắp hết hạn (Hệ thống)";
                    content = $@"
Hợp đồng của sinh viên **{fullName} ({userIdString})** tại Phòng **{roomNumber}** sẽ hết hạn vào ngày <strong>{endDate}</strong>.<br/>
- Vui lòng theo dõi tình trạng gia hạn/trả phòng.";
                    url = $"/AdminRooms/CurrentContracts?status=Expiring";
                    break;

                // Trường hợp chung khi Admin muốn gửi thông báo cho chính mình (ví dụ: dùng trong SendNotification action)
                case "AdminBroadcast":
                    title = " THÔNG BÁO CHUNG";
                    content = $@"
**Thông báo hệ thống:** {reg.Note} (Thông báo này được gửi từ chức năng Admin).";
                    url = $"/Admin/Notifications";
                    break;

                default:
                    title = "THÔNG BÁO HỆ THỐNG: Thông tin KTX";
                    content = $"Thông báo chung về sự kiện '{type}' liên quan đến sinh viên {fullName}.";
                    break;
            }

            var noti = new Notification
            {
                UserID = SystemAdminID, // Gửi đến Admin
                RegID = reg.RegID,
                Title = title,
                Content = content,
                CreatedAt = DateTime.Now,
                TargetRole = targetRole,
                IsRead = false,
                Url = url
            };

            SaveNotification(noti);
        }
    }
}