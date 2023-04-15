using System.Web;
using System.Web.Optimization;

namespace ShippingLabelManager
{
    public class BundleConfig
    {
        // 如需統合的詳細資訊，請瀏覽 https://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            BundleTable.EnableOptimizations = true;
            BundleTable.Bundles.UseCdn = true;

            bundles.Add(new StyleBundle("~/css/AllPage").Include(
                "~/Content/_Layout.css",
                "~/Content/Main_Edit.css",
                "~/Content/Main_Print.css",
                "~/Content/Main_Settings.css",
                "~/Content/Tables.css",
                "~/Content/svg.select.min.css",
                "~/Content/select2.min.css"));

            bundles.Add(new ScriptBundle("~/js/jQuery").Include("~/lib/jquery/jquery.min.js"));
            bundles.Add(new ScriptBundle("~/js/jsbarcode").Include("~/lib/jsbarcode/JsBarcode.all.min.js"));
            bundles.Add(new ScriptBundle("~/js/qrcode").Include("~/lib/qrcode/qrcode.min.js"));
            bundles.Add(new ScriptBundle("~/js/rulez").Include("~/lib/rulez/rulez.js"));
            bundles.Add(new ScriptBundle("~/js/select2").Include("~/lib/select2/select2.min.js"));
            bundles.Add(new ScriptBundle("~/js/svgjs").Include(
                "~/lib/svgjs/svg.min.js",
                "~/lib/svgjs/svg.select.min.js"));
            //抓圖函式庫
            bundles.Add(new ScriptBundle("~/js/canvg").Include(
                "~/lib/canvg/rgbcolor.min.js",
                "~/lib/canvg/stackblur.min.js",
                "~/lib/canvg/canvg.min.js"));
        }
    }
}
