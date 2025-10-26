using KTXSV.Models;
using System.Linq;
using System.Web.Mvc;

namespace KTXSV.Controllers
{
    public class HomeController : Controller
    {
        private readonly KTXSVEntities db = new KTXSVEntities();

        public ActionResult Index()
        {
           
            return View();
        }
    }
}
