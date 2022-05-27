//using Chia.NET.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Chia.NET.Clients
{
    public sealed class HarvesterClient : ChiaApiClient
    {
        private new const string ApiUrl = "https://localhost:8560/";

        public HarvesterClient()
            : base("harvester", ApiUrl)
        {
        }


        /// <summary>
        /// Gets a list of plots being farmed on this harvester.
        /// </summary>
        /// <returns></returns>
        //public async Task<Plot[]> GetPlotsAsync()
        //{
        //    var result = await PostAsync<GetPlotsResult>(HarvesterRoutes.GetPlots(ApiUrl));
        //    return result.Plots;
        //}

        public async Task<string> GetPlotsAsync()
        {
            var result = await PostAsync(HarvesterRoutes.GetPlots(ApiUrl));
            return result;
        }


        /// <summary>
        /// Refreshes the plots, forces the harvester to search for and load new plots.
        /// </summary>
        /// <returns></returns>
        public Task RefreshPlotsAsync()
            => PostAsync(HarvesterRoutes.RefreshPlots(ApiUrl));

        /// <summary>
        /// Deletes a plot file and removes it from the harvester.
        /// </summary>
        /// <returns></returns>
        public Task DeletePlotAsync(string fileName)
            => PostAsync(HarvesterRoutes.DeletePlot(ApiUrl), new Dictionary<string, string>()
            {
                ["filename"] = fileName
            });

        /// <summary>
        /// Adds a plot directory (not including sub-directories) to the harvester and configuration. Plots will be loaded and farmed eventually.
        /// </summary>
        /// <returns></returns>
        public Task AddPlotDirectoryAsync(string dirPath)
            => PostAsync(HarvesterRoutes.AddPlotDirectory(ApiUrl), new Dictionary<string, string>()
            {
                ["dirname"] = dirPath
            });

        /*
        /// <summary>
        /// Returns all of the plot directoried being farmed.
        /// </summary>
        /// <returns></returns>
        public async Task<string[]> GetPlotDirectoriesAsync()
        {
            var result = await PostAsync<GetPlotDirectoriesResult>(HarvesterRoutes.GetPlotDirectories(ApiUrl));
            return result.Directories;
        }
        */

        /// <summary>
        /// Removes a plot directory from the config, does not actually delete the directory.
        /// </summary>
        /// <returns></returns>
        public Task RemovePlotDirectoryAsync(string dirPath)
            => PostAsync(HarvesterRoutes.RemovePlotDirectory(ApiUrl), new Dictionary<string, string>()
            {
                ["dirname"] = dirPath
            });


    }
}
