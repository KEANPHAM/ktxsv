using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace KTXSV.Controllers
{
    public class SinhVienController : Controller
    {
        // GET: SinhVien
        public ActionResult ChinhSuaThongTin()
        {
            return View();
        }
        public ActionResult ThongTinCaNhan()
        {
            return View();
        }
    }
}