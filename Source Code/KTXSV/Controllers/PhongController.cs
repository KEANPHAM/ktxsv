using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace KTXSV.Controllers
{
    public class PhongController : Controller
    {
        // GET: Phong
        public ActionResult DangKyPhong()
        {
            return View();
        }
        public ActionResult DanhSachPhong()
        {
            return View();
        }
    }
}