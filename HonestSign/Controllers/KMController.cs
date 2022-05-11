using System.Web.Mvc;
using NLog;

namespace HonestSign.Controllers
{
    public class KMController : Controller
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        [HttpGet]
        [ValidateInput(false)]
        public JsonResult GetStatus(string km, string filial)
        {
            logger.Info($"Вызов метода KMController.GetStatus: km = {km}, filial = {filial}");
            return Json(Models.KM.GetStatus(km, false, filial ?? "td"), JsonRequestBehavior.AllowGet);
        }
    }
}