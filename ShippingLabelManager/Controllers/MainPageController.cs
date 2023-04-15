using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ShippingLabelManager.Controllers
{
    public class MainPageController : Controller
    {
        public ActionResult Inquery()
        {
            string customerNo = Request.QueryString["no2"]; //客戶代號

            ViewData.Add("customerNo", customerNo);

            return View();
        }

        public ActionResult Edit()
        {
            return View();
        }

        public ActionResult Print()
        {
            string orderNo = Request.QueryString["no"];     //訂單單號
            string customerNo = Request.QueryString["no2"]; //客戶代號
            string moNo = Request.QueryString["ta"];        //製令單號

            ViewData.Add("orderNo", orderNo);
            ViewData.Add("customerNo", customerNo);
            ViewData.Add("moNo", moNo);

            return View();
        }

        public ActionResult Settings()
        {
            return View();
        }
    }
}
