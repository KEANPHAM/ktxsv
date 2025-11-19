//using KTXSV.Models;
//using System;

//namespace KTXSV.Services
//{
//    public class StudentNotificationService : NotificationService
//    {
//        public StudentNotificationService(KTXSVEntities dbContext) : base(dbContext)
//        {
//        }

//        public void SendStudentNotification(int userId, int regId, string type, Registration reg, int months = 0)
//        {
//            string title = "";
//            string content = "";
//            string url = "/ThongBao/Index";
//            string targetRole = "Student";

//            // Lấy các giá trị an toàn từ Registration
//            DateTime startDate = reg.StartDate;
//            DateTime endDate = reg.EndDate.GetValueOrDefault(DateTime.Today.AddMonths(1));

//            // Tính toán Deadline
//            DateTime paymentDeadline = startDate.AddMonths(-1);
//            DateTime renewalDeadline = endDate.AddMonths(-1);

//            // Lấy thông tin user an toàn
//            string fullName = reg.User?.FullName ?? "Sinh viên";
//            string userIdString = reg.User?.UserID.ToString() ?? "N/A";

//            // Lấy thông tin phòng an toàn
//            string roomNumber = reg.Room?.RoomNumber ?? "N/A";
//            string bedNumber = reg.Bed?.BedNumber ?? "N/A";
//            string building = reg.Room?.Building ?? "N/A";


//            switch (type)
//            {
//                case "NewRegistration":
//                    title = "XÁC NHẬN: Đã nhận Yêu cầu Đăng ký Phòng";
//                    content = $@"
//Kính gửi: <strong>{fullName} ({userIdString})</strong><br/><br/>
//Phòng Quản lý Ký túc xá xác nhận đã nhận được **Yêu cầu đăng ký chỗ ở** của bạn.<br/><br/>
//<b>1. Thông tin đăng ký:</b><br/>
//- Phòng: <strong>{roomNumber}</strong>, Tòa <strong>{building}</strong>, Giường <strong>{bedNumber}</strong><br/>
//- Thời gian thuê: từ <strong>{startDate:dd/MM/yyyy}</strong> đến <strong>{endDate:dd/MM/yyyy}</strong><br/><br/>
//<b>2. Bước tiếp theo:</b><br/>
//- Yêu cầu của bạn đang được **xem xét và chờ phê duyệt** bởi Ban Quản lý KTX.<br/>
//- Vui lòng thường xuyên kiểm tra thông báo để nhận được quyết định cuối cùng.<br/><br/>
//Trân trọng,<br/><strong>Phòng Quản lý KTX - HUFLIT</strong>";
//                    url = "/Phong/DanhSachPhong";
//                    break;

//                case "Canceled":
//                    title = "XÁC NHẬN: Hủy Đăng ký Phòng Ký túc xá";
//                    content = $@"
//Kính gửi: <strong>{fullName} ({userIdString})</strong><br/><br/>
//Phòng Quản lý Ký túc xá xác nhận đã **hủy bỏ** yêu cầu/hợp đồng chỗ ở của bạn.<br/><br/>
//<b>1. Thông tin hủy:</b><br/>
//- Phòng: <strong>{roomNumber}</strong>, Giường <strong>{bedNumber}</strong><br/>
//- Trạng thái hợp đồng: **Đã hủy**<br/><br/>
//<b>2. Lưu ý:</b><br/>
//- Nếu bạn hủy hợp đồng đang **Active**, vui lòng liên hệ KTX để hoàn tất thủ tục trả phòng và xử lý các khoản phí liên quan (nếu có).<br/><br/>
//Trân trọng,<br/><strong>Phòng Quản lý KTX - HUFLIT</strong>";
//                    url = "/Phong/DanhSachPhong";
//                    break;

//                case "Approved":
//                    title = "THÔNG BÁO QUAN TRỌNG: Phê duyệt Đăng ký Phòng Ký túc xá";
//                    content = $@"
//Kính gửi: <strong>{fullName} ({userIdString})</strong><br/><br/>
//Phòng Quản lý Ký túc xá trân trọng thông báo:<br/>
//<strong>Yêu cầu đăng ký chỗ ở</strong> tại KTX đã được <strong>phê duyệt chính thức</strong>.<br/><br/>
//<b>1. Thông tin chỗ ở:</b><br/>
//- Phòng: <strong>{roomNumber}</strong><br/>
//- Giường: <strong>{bedNumber}</strong><br/>
//- Thời gian thuê dự kiến: từ <strong>{startDate:dd/MM/yyyy}</strong><br/><br/>
//<b>2. Yêu cầu thanh toán:</b><br/>
//Một <strong>Hóa đơn KTX</strong> đã được tạo. Vui lòng thanh toán trước <strong>{paymentDeadline:dd/MM/yyyy}</strong>.<br/><br/>
//<b>3. Hướng dẫn:</b><br/>
//- Xem chi tiết hóa đơn tại <a href='/ThanhToan/HoaDon' target='_blank'>Trang Thanh Toán</a>.<br/><br/>
//Trân trọng,<br/><strong>Phòng Quản lý KTX - HUFLIT</strong>";
//                    url = "/ThanhToan/HoaDon";
//                    break;

//                case "Rejected":
//                    title = "THÔNG BÁO QUAN TRỌNG: Từ chối Đăng ký Phòng Ký túc xá";
//                    content = $@"
//Kính gửi: <strong>{fullName} ({userIdString})</strong><br/><br/>
//Yêu cầu đăng ký phòng của bạn tại Phòng <strong>{roomNumber}</strong>, Giường <strong>{bedNumber}</strong> đã bị <strong>từ chối</strong>.<br/>
//<b>1. Lý do (Ví dụ):</b><br/>
//- Đăng ký không hợp lệ hoặc phòng đã đầy.<br/><br/>
//<b>2. Hướng dẫn:</b><br/>
//- Đăng ký lại tại <a href='/DangKy/Index' target='_blank'>Trang Đăng ký Phòng</a> nếu còn suất.<br/><br/>
//Trân trọng,<br/><strong>Phòng Quản lý KTX - HUFLIT</strong>";
//                    url = "/DangKy/Index";
//                    break;

//                case "Extended":
//                    title = "THÔNG BÁO QUAN TRỌNG: Xác nhận Gia hạn Hợp đồng KTX";
//                    content = $@"
//Kính gửi: <strong>{fullName} ({userIdString})</strong><br/><br/>
//Gia hạn hợp đồng chỗ ở của bạn đã được thực hiện thành công.<br/><br/>
//<b>1. Thông tin mới:</b><br/>
//- Gia hạn thêm: <strong>{months} tháng</strong><br/>
//- Hạn mới: <strong>{endDate:dd/MM/yyyy}</strong><br/>
//- Chỗ ở: Phòng <strong>{roomNumber}</strong>, Giường <strong>{bedNumber}</strong><br/><br/>
//<b>2. Thanh toán:</b><br/>
//Hóa đơn gia hạn đã được tạo. Vui lòng thanh toán trước <strong>{renewalDeadline:dd/MM/yyyy}</strong>.<br/><br/>
//<b>3. Hướng dẫn:</b><br/>
//- Xem hóa đơn tại <a href='/ThanhToan/HoaDon' target='_blank'>Trang Thanh Toán</a>.<br/><br/>
//Trân trọng,<br/><strong>Phòng Quản lý KTX - HUFLIT</strong>";
//                    url = "/ThanhToan/HoaDon";
//                    break;

//                case "Expiring":
//                    title = "CẢNH BÁO: Hợp đồng Ký túc xá sắp hết hạn";
//                    content = $@"
//Kính gửi: <strong>{fullName} ({userIdString})</strong><br/><br/>
//Hợp đồng phòng <strong>{roomNumber}</strong> sẽ hết hạn vào ngày <strong>{endDate:dd/MM/yyyy}</strong>.<br/><br/>
//<b>1. Lựa chọn:</b><br/>
//- <a href='/GiaHan/Index' target='_blank'>Gia hạn hợp đồng</a><br/>
//- Thực hiện bàn giao phòng trước ngày hết hạn.<br/><br/>
//<b>2. Hướng dẫn:</b><br/>
//- Sinh viên không hoàn tất thủ tục đúng hạn sẽ bị xử lý theo quy định KTX.<br/><br/>
//Trân trọng,<br/><strong>Phòng Quản lý KTX - HUFLIT</strong>";
//                    url = "/GiaHan/Index";
//                    break;

//                default:
//                    title = "THÔNG BÁO CHUNG: Thông tin Ký túc xá";
//                    content = $@"
//Kính gửi: <strong>{fullName} ({userIdString})</strong><br/><br/>
//Phòng Quản lý KTX có thông báo: {type}<br/><br/>
//Trân trọng,<br/><strong>Phòng Quản lý KTX - HUFLIT</strong>";
//                    break;
//            }

//            var noti = new Notification
//            {
//                UserID = userId,
//                RegID = regId,
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