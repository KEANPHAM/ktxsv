using System.ComponentModel.DataAnnotations;

namespace KTXSV.Models
{
    [MetadataType(typeof(RoomMetadata))]
    public partial class Room
    {
    }

    public class RoomMetadata
    {
        [Required(ErrorMessage = "Vui lòng nhập giới tính!")]
        public string Gender { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số phòng!")]
        public string RoomNumber { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tòa nhà!")]
        public string Building { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập sức chứa!")]
        [Range(1, 20, ErrorMessage = "Sức chứa từ 1 đến 20!")]
        public int Capacity { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá phòng!")]
        [Range(0, 999999999, ErrorMessage = "Giá không hợp lệ!")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Vui lòng cập nhật trạng thái!")]
        public string Status { get; set; }
    }
}
