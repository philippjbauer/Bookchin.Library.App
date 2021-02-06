using System;
using System.Drawing;
using System.Linq;
using PhotinoNET;

namespace Bookchin.Library.App
{
    class Program
    {
        static void Main(string[] args)
        {
            // Configure internal WebHost for API project
            string baseAddress = "http://localhost:7645";

            string[] apiArgs = new string[] {
                "ENVIRONMENT=Development",
                $"URLS={baseAddress}",
                "DefaultConnection=Data Source=../Bookchin.Library.API/Data/BookchinLibrary.db"
            };

            foreach (string arg in apiArgs) {
                Console.WriteLine(arg);
            }

            using (var api = Bookchin.Library.API.Program.CreateHostBuilder(apiArgs).Build()) 
            { 
                // Start Api Host
                api.StartAsync();

                // Create new Photino Window
                var window = new PhotinoNET.PhotinoNET("Bookchin Library", configure => { });

                // Set the initial window size 
                // relative to the screen
                Monitor monitor = window.Monitors.First();

                int width = (int) Math.Round(monitor.WorkArea.Width * 0.66, 0, MidpointRounding.AwayFromZero);
                int height = (int) Math.Round(monitor.WorkArea.Height * 0.66, 0, MidpointRounding.AwayFromZero);
                int left = (int)Math.Round((double)((monitor.WorkArea.Width - width) / 2), 0);
                int top = (int) Math.Round((double) ((monitor.WorkArea.Height - height) / 2), 0);

                Console.WriteLine($"Creating Window with dimensions: ({width}, {height}) at ({left}, {top})");

                window.Size = new Size(width, height);
                window.Left = left;

                window.NavigateToUrl($"{baseAddress}/swagger/index.html");
                window.WaitForExit();
            }
        }
    }
}
