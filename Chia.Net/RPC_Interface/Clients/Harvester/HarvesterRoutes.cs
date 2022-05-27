using System;

namespace Chia.NET.Clients
{
    internal static class HarvesterRoutes
    {
        public static Uri GetPlots(string apiUrl)
            => new Uri(apiUrl + "get_plots");

        public static Uri RefreshPlots(string apiUrl)
            => new Uri(apiUrl + "refresh_plots");

        public static Uri DeletePlot(string apiUrl)
            => new Uri(apiUrl + "delete_plot");

        public static Uri AddPlotDirectory(string apiUrl)
            => new Uri(apiUrl + "add_plot_directory");

        public static Uri GetPlotDirectories(string apiUrl)
            => new Uri(apiUrl + "get_plot_directories");

        public static Uri RemovePlotDirectory(string apiUrl)
            => new Uri(apiUrl + "remove_plot_directory");
    }
}
