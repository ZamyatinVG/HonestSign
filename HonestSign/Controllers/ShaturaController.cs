using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HonestSign.Controllers
{
    public class ShaturaController : Controller
    {
        [HttpGet]
        [ValidateInput(false)]
        public JsonResult GetBarcode(string id, string nodoc)
        {
            return Json(Models.Shatura.GetBarcode(id, nodoc), JsonRequestBehavior.AllowGet);
        }
    }
}