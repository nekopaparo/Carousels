using System.Web.Mvc;

namespace Carousels.Controllers
{
    public class WatchController : Controller
    {
        [HttpGet]
        public ActionResult Index()
        {
            return View();
        }


        [HttpGet]
        public ActionResult Setting()
        {
            return View();
        }
    }
}