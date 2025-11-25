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
Phòng Quản lý KTX xác nhận đã xóa giường.<br/>
- Phòng: <strong>{roomNumber}</strong><br/>
- Giường bị xóa: <strong>{bedNumber}</strong><br/>
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
- Tên phòng: <strong>{roomNumber}</strong><br/>
- Tòa nhà: <strong>{building}</strong><br/>
- Sức chứa: <strong>{capacity}</strong> giường.<br/>
Vui lòng kiểm tra danh sách phòng.";
                    url = $"/AdminRooms/Details/{roomID}";
                    break;

                case "RoomUpdated":
                    title = "PHÒNG ĐƯỢC SỬA: Cập Nhật Thông Tin Phòng";
                    content = $@"
Phòng <strong>{roomNumber}</strong> đã được cập nhật thông tin.<br/>
- Sức chứa mới: <strong>{capacity}</strong> giường.<br/>
- Trạng thái hiện tại: <strong>{room?.Status ?? "N/A"}</strong>.<br/>
Kiểm tra chi tiết các thay đổi về giường.";
                    url = $"/AdminRooms/Details/{roomID}";
                    break;

                case "RoomDeleted":
                    title = "PHÒNG ĐƯỢC XÓA: Xóa Phòng Khỏi Hệ Thống";
                    content = $@"
Phòng <strong>{roomNumber}</strong> (ID: {roomID}) đã bị xóa khỏi hệ thống KTX.<br/>
- Thao tác xóa được thực hiện khi phòng không còn sinh viên ở.";
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
        public void SendAdminNotification(string type, Registration reg, Registration newReg = null)
        {
            string title = "";
            string content = "";
            string url = "/AdminRooms/Index"; // Default URL cho Admin
            string targetRole = "Admin";

            // Lấy thông tin an toàn
            string fullName = reg.User?.FullName ?? "Sinh viên [N/A]";
            string username = reg.User?.Username ?? "N/A";
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
Hợp đồng của sinh viên  <strong>{fullName} {username}</strong> tại Phòng <strong>{roomNumber}</strong>, Giường <strong>{bedNumber}</strong> đã được chấm dứt bởi Admin vào ngày <strong>{DateTime.Now:dd/MM/yyyy}</strong>.<br/>
- Giường đã được mở cho phép đăng ký mới.<br/>
- Vui lòng đảm bảo sinh viên đã hoàn tất thủ tục trả phòng.";
                    url = $"/AdminRooms/Details/{reg.RoomID}";
                    break;


                case "Transferred":
                    string oldRoomNumber = reg.Room?.RoomNumber ?? "N/A";
                    string oldBedNumber = reg.Bed?.BedNumber.ToString() ?? "N/A";

                    string newRoomNumber = newReg?.Room?.RoomNumber ?? "N/A";
                    string newBedNumber = newReg?.Bed?.BedNumber.ToString() ?? "N/A";
                    string newEndDate = newReg?.EndDate?.ToString("dd/MM/yyyy") ?? "N/A";

                    string finalUrl = newReg != null ? $"/AdminRooms/RegistrationDetails/{newReg.RegID}" : $"/AdminRooms/Details/{reg.RoomID}";

                    title = "THÔNG BÁO: Chuyển Phòng Đã Xảy Ra";
                    content = $@"
Sinh viên <strong>{fullName} {username}</strong> đã được chuyển phòng.<br/>
<br/>
<b>Thông tin Chuyển giao:</b><br/>
- Phòng Cũ: <strong>{oldRoomNumber}</strong>, Giường <strong>{oldBedNumber}</strong> (Đã kết thúc).<br/>
- Phòng Mới: <strong>{newRoomNumber}</strong>, Giường <strong>{newBedNumber}</strong> (Hạn Hợp đồng: {newEndDate}).<br/>
<br/>
<b>Lưu ý</b><br/>
- Vui lòng truy cập đăng ký mới để theo dõi.<br/>
- Kiểm tra các khoản phí phát sinh do chuyển phòng (nếu có).";

                    url = finalUrl;
                    break;
                case "NewRegistration":
                    title = "CẦN PHÊ DUYỆT: Yêu cầu Đăng ký Phòng Mới";
                    content = $@"
Sinh viên <strong>{fullName} {username}</strong> đã gửi yêu cầu đăng ký chỗ ở.<br/>
- Phòng: <strong>{roomNumber}</strong>, Giường <strong>{bedNumber}</strong><br/>
- Thời gian: {startDate} - {endDate}<br/><br/>
Vui lòng truy cập trang phê duyệt để xử lý.";
                    url = $"/AdminRooms/RegistrationDetails/{reg.RegID}";
                    break;

                case "Canceled":
                    title = "CẢNH BÁO: Sinh viên Hủy Đăng ký/Hợp đồng";
                    content = $@"
Sinh viên  <strong>{fullName} {username}</strong> đã hủy đăng ký phòng mới.<br/>
- Phòng: <strong>{roomNumber}</strong>, Giường <strong>{bedNumber}</strong><br/>";
                    url = $"/AdminRooms/Details/{reg.RoomID}";
                    break;

                case "Extended":
                    title = " XÁC NHẬN: Sinh viên Gia hạn Hợp đồng";
                    content = $@"
Sinh viên <strong>{fullName} {username}</strong> đã gia hạn hợp đồng thành công.<br/>
- Phòng: <strong>{roomNumber}</strong>, Giường <strong>{bedNumber}</strong><br/>
- Hạn mới: <strong>{endDate}</strong><br/>
- Hóa đơn mới đã được tạo, cần theo dõi thanh toán.";
                    url = $"/AdminRooms/CurrentContracts";
                    break;

                case "Approved":
                    title = " THÔNG BÁO: Yêu cầu Đăng ký Đã được Duyệt";
                    content = $@"
Yêu cầu đăng ký phòng của sinh viên <strong>{fullName} {username}</strong>  đã được phê duyệt.<br/>
- Phòng: <strong>{roomNumber}</strong>, Giường <strong>{bedNumber}</strong><br/>
- Sinh viên cần hoàn tất thanh toán trước khi vào ở.";
                    url = $"/AdminRooms/CurrentContracts";
                    break;

                case "Rejected":
                    title = " THÔNG BÁO: Yêu cầu Đăng ký Đã bị Từ chối";
                    content = $@"
Yêu cầu đăng ký phòng của sinh viên <strong>{fullName} {username}</strong>  đã bị từ chối.<br/>
- Vui lòng đảm bảo các thông báo từ chối đã được gửi đến sinh viên liên quan.";
                    url = $"/AdminRooms/PendingRegistrations";
                    break;

                case "PaymentReceived":
                    title = " THANH TOÁN: Sinh viên Đã đóng tiền KTX";
                    content = $@"
Hệ thống ghi nhận sinh viên <strong>{fullName} {username}</strong> đã đóng tiền.<br/>
- Số tiền: [Amount] (Chèn số tiền nếu có)<br/>
- Vui lòng kiểm tra và xác nhận hóa đơn trên hệ thống.";
                    url = $"/AdminPayment/Details/{reg.RegID}";
                    break;

                case "Expiring":
                    title = " CẢNH BÁO: Hợp đồng sắp hết hạn (Hệ thống)";
                    content = $@"
Hợp đồng của sinh viên <strong>{fullName} {username}</strong> tại Phòng <strong>{roomNumber}</strong> sẽ hết hạn vào ngày <strong>{endDate}</strong>.<br/>
- Vui lòng theo dõi tình trạng gia hạn/trả phòng.";
                    url = $"/AdminRooms/CurrentContracts?status=Expiring";
                    break;

                // Trường hợp chung khi Admin muốn gửi thông báo cho chính mình (ví dụ: dùng trong SendNotification action)
                case "AdminBroadcast":
                    title = " THÔNG BÁO CHUNG";
                    content = $@"
Thông báo hệ thống: {reg.Note} (Thông báo này được gửi từ Admin).";
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
        public void SendAdminNotification(string type, Payment payment)
        {
            string title = "";
            string content = "";
            string url = "/PaymentAdmin/Index";
            string targetRole = "Admin";

            switch (type)
            {
                case "PaymentPending":
                    title = "THANH TOÁN: Cần duyệt hóa đơn";
                    content = $@"
Sinh viên <strong>{payment.Registration.User.FullName} {payment.Registration.User.Username}</strong> đã xác nhận thanh toán.<br/>
- Mã hóa đơn: <strong>{payment.PaymentID}</strong><br/>
- Số tiền: <strong>{payment.Amount:N0} VNĐ</strong><br/>
- Trạng thái hiện tại: <span class='badge bg-info'>Chờ duyệt</span><br/><br/>
<b>Thao tác:</b> <a href='/PaymentAdmin/DetailsEdit/{payment.PaymentID}'>Nhấn vào đây để duyệt hóa đơn</a>.";
                    break;

            }

            var noti = new Notification
            {
                UserID = SystemAdminID,
                RegID = payment.RegID,
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