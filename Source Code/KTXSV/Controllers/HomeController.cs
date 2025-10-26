using KTXSV.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace KTXSV.Controllers
{
    public class HomeController : Controller
    {
        KTXSVEntities db =new KTXSVEntities();
        public ActionResult Index()
        {
            Session["UserID"] = 7;
            Session["UserName"] = "Kiên Phạm";
            Session["Role"] = "Admin";

            var userId = int.Parse(Session["UserID"].ToString()); 

            var role = db.Users
                .Where(r => r.UserID == userId)
                .Select(r => r.Role)
                .FirstOrDefault();
            
            if (role == "Admin")
            {
                if (role == null || role != "Admin")
                {
                    return RedirectToAction("Login", "Account");
                }

                return RedirectToAction("PendingRegistrations", "Admin"); 
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }



        }
    }
}