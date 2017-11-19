using System.Web.Optimization;

namespace WebApp.App_Start
{
    public class BundleConfig
    {
        // For more information on Bundling, visit http://go.microsoft.com/fwlink/?LinkId=254725
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                "~/Scripts/jslib/jquery-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryui").Include(
                "~/Scripts/jslib/jquery-ui-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                "~/Scripts/jslib/jquery.unobtrusive*",
                "~/Scripts/jslib/jquery.validate*"));

            bundles.Add(new ScriptBundle("~/bundles/knockout").Include(
                "~/Scripts/jslib/knockout-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/ajaxlogin").Include(
                "~/Scripts/app/ajaxlogin.js"));

            bundles.Add(new ScriptBundle("~/bundles/mixed").Include(
                "~/Scripts/app/mixed.rules.js",
                "~/Scripts/app/mixed.qqplot.js",
                "~/Scripts/app/mixed.datacontext.js",
                "~/Scripts/app/mixed.bindings.js",
                "~/Scripts/app/mixed.model.js",
                "~/Scripts/app/mixed.viewmodel.js",
                "~/Scripts/app/mixed.charts.js"));

            bundles.Add(new ScriptBundle("~/bundles/flot").Include(
                "~/Scripts/jslib/flot/jquery.flot.js",
                "~/Scripts/jslib/flot/jquery.flot.errorbars.js",
                "~/Scripts/jslib/flot/jquery.flot.orderbars.js",
                "~/Scripts/jslib/flot/jquery.flot.dashes.js",
                "~/Scripts/jslib/flot/jquery.flot.categories.js"));

            bundles.Add(new ScriptBundle("~/bundles/datatables").Include(
                "~/Scripts/jslib/datatable/jquery.dataTables.js"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                "~/Scripts/jslib/modernizr-*"));

            bundles.Add(new StyleBundle("~/Content/css").Include(
                "~/Content/Site.css",
                "~/Content/TodoList.css"));

            bundles.Add(new StyleBundle("~/bundles/datatables/css").Include(
                "~/Scripts/jslib/datatable/css/jquery.dataTables.css",
                "~/Scripts/jslib/datatable/css/jquery.dataTables_themeroller.css"/*,
                "~/Scripts/jslib/datatable/images/*.png",
                "~/Scripts/jslib/datatable/images/*.psd",
                "~/Scripts/jslib/datatable/images/*.ico"*/));

            bundles.Add(new StyleBundle("~/Content/themes/base/css").Include(
                "~/Content/themes/base/jquery.ui.core.css",
                "~/Content/themes/base/jquery.ui.resizable.css",
                "~/Content/themes/base/jquery.ui.selectable.css",
                "~/Content/themes/base/jquery.ui.accordion.css",
                "~/Content/themes/base/jquery.ui.autocomplete.css",
                "~/Content/themes/base/jquery.ui.button.css",
                "~/Content/themes/base/jquery.ui.dialog.css",
                "~/Content/themes/base/jquery.ui.slider.css",
                "~/Content/themes/base/jquery.ui.tabs.css",
                "~/Content/themes/base/jquery.ui.datepicker.css",
                "~/Content/themes/base/jquery.ui.progressbar.css",
                "~/Content/themes/base/jquery.ui.theme.css"));
        }
    }
}