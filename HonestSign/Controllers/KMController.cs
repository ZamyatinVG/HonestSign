using System.Web.Mvc;

namespace HonestSign.Controllers
{
    public class KMController : Controller
    {
        [HttpGet]
        [ValidateInput(false)]
        public JsonResult GetStatus(string km)
        {
            return Json(Models.KM.GetStatus(km, false), JsonRequestBehavior.AllowGet);
        }
    }
}