using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Web;

namespace Code.Models
{
    // 
    public class User
    {
        public int UserId { get; set; }
        public string UserName { get; set; } // Tên hiển thị của người dùng 
        public string FullName { get; set; } // Họ và tên đầy đủ
        public string Gender { get; set; }   // Giới tính
        public DateTime? DateOfBirth { get; set; }  // Ngày sinh
        public string Phone { get; set; } // Số điện thoại
        public string Email { get; set; }   //Email dùng đăng nhập
        public string Password { get; set; } // Mật khẩu
        public string Role { get; set; } = "Student";    // Vai trò (Admin, sinh viên)
        public DateTime CreatedAt { get; set; } = DateTime.Now; // Ngày tạo tài khoản
    }
}