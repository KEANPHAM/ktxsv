# 📚 KTXSV – Quản lý Ký túc xá Sinh viên HUFLIT

## Mô tả
**KTXSV** là ứng dụng quản lý ký túc xá dành cho sinh viên HUFLIT, được phát triển bằng **ASP.NET MVC (C#)** kết hợp **Entity Framework** và **SQL Server**.  
Ứng dụng giúp quản lý toàn bộ hoạt động của sinh viên và quản trị viên trong ký túc xá một cách hiệu quả và trực quan.

## Chức năng chính

### 🎓 Sinh viên
- Đăng ký / đăng nhập tài khoản  
- Xem thông tin cá nhân và tình trạng phòng  
- Xem danh sách phòng trống và gửi yêu cầu đăng ký  
- Hủy các yêu cầu chờ duyệt (`Pending`)  
- Xem tiền phòng, hóa đơn điện, nước  
- Nhận thông báo và gửi yêu cầu hỗ trợ  

### 🛠 Quản trị viên
- Quản lý sinh viên: thêm, sửa, xóa, duyệt yêu cầu  
- Quản lý phòng: thêm, sửa, xóa, cập nhật số giường trống, trạng thái  
- Quản lý hợp đồng / đăng ký: xem danh sách, xử lý chuyển/trả phòng  
- Quản lý thanh toán: cập nhật hóa đơn, theo dõi tình trạng đóng/ chưa đóng  
- Quản lý thông báo & yêu cầu hỗ trợ  
- Thống kê số lượng sinh viên theo phòng, tòa, doanh thu  

## Công nghệ sử dụng
- Backend: ASP.NET MVC, C#  
- Database: SQL Server + Entity Framework  
- Frontend: HTML5, CSS3, Bootstrap 5  
- ORM: Entity Framework 6  

## Cấu trúc cơ sở dữ liệu
- Users: thông tin sinh viên và admin  
- Rooms: thông tin phòng KTX (tòa, số giường, sức chứa, giá, trạng thái)  
- Registrations: đăng ký phòng, hợp đồng ở  
- Payments: thanh toán tiền phòng, điện, nước  
- Notifications: thông báo gửi đến sinh viên/admin  
- SupportRequests: yêu cầu hỗ trợ của sinh viên  
- Bed: giường trong từng phòng
-a