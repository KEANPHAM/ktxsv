using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KTXSV.Models
{
    public class RegisterViewModel
    {
        public int UserID { get; set; }

        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        public string PasswordHash { get; set; }

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
        [Compare("PasswordHash", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Họ tên không được để trống")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn giới tính")]
        public string Gender { get; set; }  

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "CCCD không được để trống")]
        public string CCCD { get; set; }

        [Required(ErrorMessage = "Ngày sinh không được để trống")]
        public DateTime? BirthDate { get; set; }

        public string Role { get; set; } = "Student";

        public DateTime? CreatedAt { get; set; } = DateTime.Now;
    }
}
