using KTXSV.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace KTXSV.Controllers
{
    public class ThanhToanController : Controller
    {
        // GET: ThanhToan
        public ActionResult HoaDon()
        {
            using (var db=new KTXSVEntities())
            {
                var payments=db.Payments.ToList(); //get all hoa don
                return View(payments);

            }
        }
    }
}