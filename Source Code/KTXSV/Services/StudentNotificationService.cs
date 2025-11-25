using KTXSV.Models;
using System;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using static System.Net.Mime.MediaTypeNames;

namespace KTXSV.Services
{
    public class StudentNotificationService : NotificationService
    {
        public StudentNotificationService(KTXSVEntities dbContext) : base(dbContext)
        {
        }

        public void SendStudentNotification(int userId, int regId, string type, Registration reg, int months = 0)
        {
            string title = "";
            string content = "";
            string url = "/ThongBao/Index";
            string targetRole = "Student";

            // Lấy các giá trị an toàn từ Registration
            DateTime startDate = reg.StartDate;
            DateTime endDate = reg.EndDate.GetValueOrDefault(DateTime.Today.AddMonths(1));

            // Tính toán Deadline
            DateTime paymentDeadline = startDate.AddDays(-7);
            DateTime renewalDeadline = endDate.AddMonths(-1);

            // Lấy thông tin user an toàn
            string fullName = reg.User?.FullName ?? "Sinh viên";
            string username = reg.User?.Username ?? "N/A";
            // Lấy thông tin phòng an toàn
            string bedNumber = reg.Bed != null ? reg.Bed.BedNumber.ToString() : "N/A";
            string roomNumber = reg.Room != null ? reg.Room.RoomNumber.ToString() : "N/A";
            string building = reg.Room?.Building ?? "N/A";

            var lastPayment = _db.Payments.Where(p => p.RegID == regId && p.Status == "Paid").OrderByDescending(p => p.PaymentDate).FirstOrDefault();
            string paymentAmount = lastPayment != null ? string.Format("{0:N0} VNĐ", lastPayment.Amount) : "[Số tiền N/A]";
            string paymentType = lastPayment?.Type ?? "Phí KTX";

            switch (type)
            {
                case "NewPayment":
                    title = "THÔNG BÁO: Hóa đơn mới được tạo";
                    content = $@"
Kính gửi: <strong>{fullName} {username}</strong><br/><br/>
Ban Quản lý KTX đã tạo một hóa đơn mới cho bạn.<br/><br/>
<b>Chi tiết hóa đơn:</b><br/>
- Loại: <strong>{reg.Payments.LastOrDefault()?.Type ?? "Phí KTX"}</strong><br/>
- Số tiền: <strong>{reg.Payments.LastOrDefault()?.Amount:N0} VNĐ</strong><br/>
- Trạng thái: <span class='badge bg-warning text-dark'>Chưa thanh toán</span><br/><br/>
Vui lòng truy cập trang <a href='/ThanhToan/HoaDon'>Thanh toán</a> để xem chi tiết và thực hiện thanh toán.";
                    url = "/ThanhToan/HoaDon";
                    break;
                case "EndContract":
                    title = " THÔNG BÁO QUAN TRỌNG: Kết thúc hợp đồng";
                    content = $@"
Kính gửi: <strong>{fullName} {username}</strong><br/><br/>
Hợp đồng chỗ ở của bạn tại Phòng <strong>{roomNumber}</strong>, Giường <strong>{bedNumber}</strong> đã được Ban Quản lý KTX kết thúc vào ngày <strong>{DateTime.Now:dd/MM/yyyy}</strong>.<br/>
<br/>
- Vui lòng liên hệ Văn phòng Quản lý KTX để hoàn tất thủ tục trả phòng và nhận lại các khoản phí (nếu có).";
                    url = "/SinhVien/ChiTietHopDong";
                    break;
                case "Transferred":
                    string oldRoomNumber = reg.Room?.RoomNumber ?? "N/A";
                    string oldBedNumber = reg.Bed?.BedNumber.ToString() ?? "N/A";


                    title = " XÁC NHẬN: Chuyển Phòng Thành Công";
                    content = $@"
Kính gửi: <strong>{fullName} {username}</strong><br/><br/>
Yêu cầu chuyển phòng của bạn đã được Ban Quản lý KTX thực hiện thành công.<br/>
<br/>
<b>Hợp đồng cũ:</b><br/>
- Phòng cũ: <strong>{oldRoomNumber}</strong>, Giường <strong>{oldBedNumber}</strong> đã được chấm dứt/chuyển giao.<br/>
<br/>
<b>Nơi ở mới:</b><br/>
- Vui lòng kiểm tra lại danh sách đăng ký phòng để xem thông tin hợp đồng mới (Phòng/Giường mới) và thời hạn mới (nếu có).<br/>
- Lưu ý: Bạn cần chuyển đồ sang phòng mới theo quy định trong thời gian 3 ngày kể từ khi nhận thông báo.";
                    url = "/Phong/DanhSachPhong";
                    break;

                //
                case "NewRegistration":
                    title = "XÁC NHẬN: Đăng ký phòng thành công";
                    content = $@"
Kính gửi: <strong>{fullName} {username}</strong><br/><br/>
Phòng Quản lý Ký túc xá xác nhận đã nhận được <strong>yêu cầu đăng ký</strong> phòng của bạn.<br/><br/>
<b>Thông tin đăng ký:</b><br/>
- Phòng: <strong>{roomNumber} {username}</strong>, Tòa <strong>{building}</strong>, Giường <strong>{bedNumber}</strong><br/>
- Thời gian thuê: từ <strong>{startDate:dd/MM/yyyy}</strong> đến <strong>{endDate:dd/MM/yyyy}</strong><br/><br/>
- Yêu cầu của bạn đang được <strong><em>xem xét và chờ phê duyệt</em></strong> bởi Ban Quản lý KTX.<br/>
- Vui lòng thường xuyên kiểm tra thông báo để nhận được quyết định cuối cùng.<br/><br/>
<div style=""text-align:center; font-weight:bold;"">
    Phòng Quản lý KTX - HUFLIT
</div>
";
                    url = "/Phong/DanhSachPhong";
                    break;

                case "Canceled":
                    title = "XÁC NHẬN: Hủy Đăng ký Phòng Ký túc xá";
                    content = $@"
Kính gửi: <strong>{fullName} {username}</strong><br/><br/>
Phòng Quản lý Ký túc xá xác nhận đã hủy bỏ yêu cầu/hợp đồng chỗ ở của bạn.<br/><br/>
<b>1. Thông tin hủy:</b><br/>
- Phòng: <strong>{roomNumber}</strong>, Giường <strong>{bedNumber}</strong><br/>
- Trạng thái hợp đồng: Đã hủy<br/><br/>
<b>2. Lưu ý:</b><br/>
- Vui lòng liên hệ KTX để hoàn tất thủ tục trả phòng và xử lý các khoản phí liên quan (nếu có).<br/><br/>
<div style=""text-align:center; font-weight:bold;"">
    Phòng Quản lý KTX - HUFLIT
</div>";
                    url = "/Phong/DanhSachPhong";
                    break;

                case "Approved":
                    title = "THÔNG BÁO QUAN TRỌNG: Phê duyệt đăng ký phòng";
                    content = $@"
Kính gửi: <strong>{fullName} {username}</strong><br/><br/>
Phòng Quản lý Ký túc xá trân trọng thông báo:<br/>
Yêu cầu đăng ký chỗ ở tại KTX đã được <strong>phê duyệt</strong> chính thức.<br/><br/>
<b>Thông tin chỗ ở:</b><br/>
- Phòng: <strong>{roomNumber}</strong><br/>
- Giường: <strong>{bedNumber}</strong><br/>
- Thời gian thuê dự kiến: từ <strong>{startDate:dd/MM/yyyy}</strong><br/><br/>
<b>Hóa đơn thanh toán:</b><br/>
Bạn có <strong>hóa đơn mới</strong> cần thanh toán. Vui lòng thanh toán trước <strong>{paymentDeadline:dd/MM/yyyy}</strong>.<br/><br/>
- Xem chi tiết hóa đơn tại <a href='/ThanhToan/HoaDon' target='_blank'>Trang Thanh Toán</a>.<br/><br/>
<div style=""""text-align:center; font-weight:bold;"""">
    Phòng Quản lý KTX - HUFLIT
</div>";

                    url = "/ThanhToan/HoaDon";
                    break;

                case "Rejected":
                    title = "THÔNG BÁO QUAN TRỌNG: Từ chối Đăng ký Phòng Ký túc xá";
                    content = $@"
Kính gửi: <strong>{fullName} {username}</strong><br/><br/>
Yêu cầu đăng ký phòng của bạn tại Phòng <strong>{roomNumber}</strong>, Giường <strong>{bedNumber}</strong> đã bị <strong>từ chối</strong>.<br/>
<b>Lý do:</b><br/>
- Đăng ký không hợp lệ hoặc phòng đã đầy.<br/><br/>
<b>Hướng dẫn:</b><br/>
- Đăng ký lại tại <a href='/Phong/DangKyPhong' target='_blank'>Trang Đăng ký Phòng Mới</a> nếu còn suất.<br/><br/>
<div style=""text-align:center; font-weight:bold;"">
    Phòng Quản lý KTX - HUFLIT
</div>";
                    url = "/DangKy/Index";
                    break;

                case "Extended":
                    title = "THÔNG BÁO QUAN TRỌNG: Xác nhận Gia hạn Hợp đồng KTX";
                    content = $@"
Kính gửi: <strong>{fullName} {username}</strong><br/><br/>
Gia hạn hợp đồng chỗ ở của bạn đã được thực hiện thành công.<br/><br/>
<b>Thông tin:</b><br/>
- Gia hạn thêm: <strong>{months} tháng</strong><br/>
- Hạn mới: <strong>{endDate:dd/MM/yyyy}</strong><br/>
- Chỗ ở: Phòng <strong>{roomNumber}</strong>, Giường <strong>{bedNumber}</strong><br/><br/>
<b>2. Thanh toán:</b><br/>
Hóa đơn mới đã được gửi đến bạn. Vui lòng thanh toán trước <strong>{renewalDeadline:dd/MM/yyyy}</strong>.<br/><br/>
<b>3. Hướng dẫn:</b><br/>
- Xem hóa đơn tại <a href='/ThanhToan/HoaDon' target='_blank'>Trang Thanh Toán</a>.<br/><br/>
<div style=""text-align:center; font-weight:bold;"">
    Phòng Quản lý KTX - HUFLIT
</div>";
                    url = "/ThanhToan/HoaDon";
                    break;

                case "Expiring":
                    title = "CẢNH BÁO: Hợp đồng Ký túc xá sắp hết hạn";
                    content = $@"
Kính gửi: <strong>{fullName} {username}</strong><br/><br/>
Hợp đồng phòng <strong>{roomNumber}</strong> sẽ hết hạn vào ngày <strong>{endDate:dd/MM/yyyy}</strong>.<br/><br/>
<b>Lựa chọn:</b><br/>
- <a href='/Phong/DanhSachPhong' target='_blank'>Gia hạn hợp đồng</a><br/>
- Thực hiện bàn giao phòng trước ngày hết hạn.<br/><br/>
<b>Lưu ý:</b><br/>
- Sinh viên không hoàn tất thủ tục đúng hạn sẽ bị xử lý theo quy định của KTX.<br/><br/>
<div style=""text-align:center; font-weight:bold;"">
    Phòng Quản lý KTX - HUFLIT
</div>";
                    url = "/Phong/DanhSachPhong";
                    break;
                case "PaymentPending":
                    title = "XÁC NHẬN: Thanh toán đang chờ duyệt";
                    content = $@"
Kính gửi: <strong>{fullName} {username}</strong><br/><br/>
Bạn đã xác nhận thanh toán cho hóa đơn.<br/>
- Phòng: <strong>{roomNumber}</strong>, Giường <strong>{bedNumber}</strong><br/>
- Số tiền: <strong>{reg.Payments.LastOrDefault()?.Amount:N0} VNĐ</strong><br/>
<br/>
Trạng thái hiện tại: <span class='badge bg-info fw-bold'>Chờ duyệt</span><br/>
Vui lòng chờ Admin kiểm tra và xác nhận.";
                    url = "/ThanhToan/HoaDon";
                    break;
                case "PaymentReceived":
                    title = " XÁC NHẬN: Thanh toán thành công";
                    content = $@"
Kính gửi: <strong>{fullName} {username}</strong><br/><br/>
Phòng Quản lý KTX xác nhận đã nhận được thanh toán của bạn.<br/><br/>
<b>Chi tiết thanh toán:</b><br/>
- Nội dung: <strong>{paymentType}</strong><br/>
- Số tiền: <strong>{paymentAmount}</strong><br/>
- Trạng thái hóa đơn: <span class=""badge bg-success fw-bold"">Đã thanh toán</span><br/><br/>
<b>Lưu ý:</b><br/>
- Hóa đơn của bạn đã được cập nhật. Vui lòng kiểm tra mục Lịch sử Thanh toán.";
                    url = "/ThanhToan/LichSu";
                    break;
                default:
                    title = "THÔNG BÁO CHUNG: Thông tin Ký túc xá";
                    content = $@"
Kính gửi: <strong>{fullName}</strong><br/><br/>
Phòng Quản lý KTX có thông báo: {type}<br/><br/>
<div style=""text-align:center; font-weight:bold;"">
    Phòng Quản lý KTX - HUFLIT
</div>";
                    break;
            }

            var noti = new Notification
            {
                UserID = userId,
                RegID = regId,
                Title = title,
                Content = content,
                CreatedAt = DateTime.Now,
                TargetRole = targetRole,
                IsRead = false,
                Url = url
            };

            SaveNotification(noti);
        }
        public void SendGeneralNotification(int userId, string type)
        {
            string title = "";
            string content = "";
            string url = "/StudentFiles/Index";
            string targetRole = "Student";

            var user = _db.Users.Find(userId);
            string fullName = user?.FullName ?? "Sinh viên";
            string username = user?.Username ?? "N/A";
            switch (type)
            {
                case "FileUploadSuccess":
                    title = " XÁC NHẬN: Cập nhật Hồ sơ Thành công";
                    content = $@"
Kính gửi: <strong>{fullName} {username}</strong><br/><br/>
Hồ sơ/tài liệu của bạn đã được cập nhật thành công trên hệ thống KTX.<br/>
<br/>
<b>Lưu ý:</b><br/>
- Hồ sơ đầy đủ là điều kiện tiên quyết để thực hiện đăng ký phòng. Bạn có thể tiến hành đăng ký phòng.<br/>
- Vui lòng đảm bảo các tài liệu đã tải lên là chính xác.";
                    url = "/Phong/DangKyPhong";
                    break;
                case "ProfileUpdated":
                    title = "XÁC NHẬN: Cập nhật Thông tin Cá nhân"; // Khai báo lại biến title
                    content = $@"
Kính gửi: <strong>{fullName} {username}</strong><br/><br/>
Thông tin cá nhân của bạn trên hệ thống KTX đã được cập nhật thành công vào ngày {DateTime.Now:dd/MM/yyyy}.<br/>
<br/>
<b>Lưu ý:</b><br/>
- Nếu bạn không thực hiện việc thay đổi này, vui lòng liên hệ ngay với Phòng Quản lý KTX để được hỗ trợ.";
                    url = "/SinhVien/ThongTinCaNhan";
                    break;
                default:
                    title = "THÔNG BÁO CHUNG";
                    content = $"Thông báo chung về sự kiện '{type}'.";
                    break;
            }

            var noti = new Notification
            {
                UserID = userId,
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
    }
}