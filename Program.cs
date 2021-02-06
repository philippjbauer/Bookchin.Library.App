using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using Bookchin.Library.App.PhotinoFluent;

namespace Bookchin.Library.App
{
    class Program
    {
        private static string _apiBaseUri = "http://localhost:7645";
        private static string _apiSchema = "api://";

        private static List<Photino> _instances = new List<Photino>();

        static void Main(string[] args)
        {
            // Configure internal WebHost for API project
            string[] apiArgs = new string[] {
                "ENVIRONMENT=Development",
                $"URLS={_apiBaseUri}",
                "DefaultConnection=Data Source=../Bookchin.Library.API/Data/BookchinLibrary.db"
            };

            using (var api = Bookchin.Library.API.Program.CreateHostBuilder(apiArgs).Build()) 
            { 
                // Start Api Host
                api.StartAsync();

                // Create and configure main window
                Photino main = CreatePhotino("Bookchin Library")
                    .RegisterWebMessageHandler(HandleJsonWebAction)
                    .NavigateTo("wwwroot/index.html");

                main.Window.WaitForExit();
            }
        }

        static public Photino CreatePhotino(string title, Rectangle? rect = null)
        {
            // Create new Photino Window
            var photino = new Photino(title);

            if (rect == null)
            {
                int width = (int)Math.Round(photino.WorkArea.Width * 0.66, 0);
                int height = (int)Math.Round(photino.WorkArea.Height * 0.66, 0);

                int left = (int)Math.Round((double)((photino.WorkArea.Width - width) / 2), 0);
                int top = (int)Math.Round((double)((photino.WorkArea.Height - height) / 2), 0);

                photino
                    .Resize(width, height)
                    .Move(left, top);
            } else 
            {
                photino
                    .Resize(rect.Value.Size)
                    .Move(rect.Value.Location);
            }

            // Check if another Window sits at the configured position,
            // nudge the window a bit if it would block underlying windows.
            if (_instances.Count > 0)
            {
                bool isOverlapping = _instances.Any(i => i.Window.Location == photino.Window.Location);
                
                if (isOverlapping)
                {
                    photino.Offset(20, 20);
                }
            }

            Console.WriteLine($"Creating window with dimensions: ({photino.Window.Size}) at ({photino.Window.Location})");
            photino.Show();

            // This seems to have no effect?
            _instances.Add(photino);

            return photino;
        }
    
        public static void HandleJsonWebAction(object sender, string message)
        {
            var photino = new Photino((PhotinoNET.PhotinoNET)sender);

            PhotinoWebAction action = JsonSerializer.Deserialize<PhotinoWebAction>(message);

            switch (action.Type)
            {
                case "window":
                    HandleWindowAction(action.Command, action.Parameters);
                    break;
                default:
                    photino.ShowMessage("Error", $"Error: Action {action.Type} unknown.");
                    break;
            }
        }

        public static void HandleWindowAction(string command, Dictionary<string, string> parameters)
        {
            var photino = CreatePhotino(parameters.GetValueOrDefault("Title") ?? "New Window");

            string uriString = parameters.GetValueOrDefault("Url");
            if (uriString != null)
            {
                Uri uri;
                if (uriString.Contains("http://") || uriString.Contains("https://"))
                {
                    uri = new Uri(uriString);
                }
                else if (uriString.Contains(_apiSchema))
                {
                    uri = new Uri($"{_apiBaseUri}/{uriString.Replace(_apiSchema, "")}");
                }
                else
                {
                    uri = new Uri(uriString, UriKind.Relative);
                }

                photino.NavigateTo(uri);
                photino.Window.WaitForExit();

                return;
            }

            string file = parameters.GetValueOrDefault("file");
            if (file != null)
            {
                photino.NavigateTo(file);
                photino.Window.WaitForExit();

                return;
            }
        }
    }
}
