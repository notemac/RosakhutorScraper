using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RosakhutorScraperLib;

namespace RosakhutorScraperClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using (CameraScraper scraper = new CameraScraper())
            {
                List<CameraInfo> cameras = new List<CameraInfo>(0);
                do
                {
                    cameras = await scraper.ParseNextAsync(withDetailedInfo: true);
                    foreach (CameraInfo camera in cameras)
                        Console.WriteLine($"{camera.Name}\n{camera.HlsUrl}\n{camera.DetailedInfoJSON}");
                } while (!scraper.LastPageParsed);
            }  
            Console.ReadKey();
        }
    }
}
