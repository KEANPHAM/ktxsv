using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace KTXSV.Controllers
{
    public class HoTroController : Controller
    {
        // GET: HoTro
        public ActionResult YeuCauHoTro()
        {
            return View();
        }
        public ActionResult LichSu()
        {
            return View();
        }
    }
}