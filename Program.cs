using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using PhotinoNET;

namespace Bookchin.Library.App
{
    class Program
    {
        private static string _apiBaseUri = "http://localhost:7645";
        private static string _apiSchema = "api://";
        private static string _assetSchema = "asset://";

        private static List<PhotinoWindow> _instances = new List<PhotinoWindow>();

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
                PhotinoWindow mainWindow = CreatePhotinoWindow("Bookchin Library")                
                    .RegisterWebMessageReceivedHandler(HandleJsonWebAction)
                    .Load("wwwroot/index.html");
                
                mainWindow.WaitforClose();
            }
        }

        static public PhotinoWindow CreatePhotinoWindow(string title, Rectangle? rect = null, PhotinoWindow parent = null)
        {
            // Create new PhotinoWindow Window
            var photino = new PhotinoWindow(title, options => {
                    options.Parent = parent;
                    
                    options.WindowCreatingHandler = (object sender, EventArgs args) => {
                        var windowInCreating = (PhotinoWindow)sender;
                        Console.WriteLine("Creating new Window");
                    };
                    
                    options.WindowCreatedHandler = (object sender, EventArgs args) =>
                    {
                        var windowCreated = (PhotinoWindow)sender;
                        Console.WriteLine($"Created new window \"{windowCreated.Title}\" with Dimensions ({windowCreated.Size}) at ({windowCreated.Location})");
                    };
                });

            if (rect == null)
            {
                Size workArea = photino.MainMonitor.WorkArea.Size;

                int width = (int)Math.Round(workArea.Width * 0.66, 0);
                int height = (int)Math.Round(workArea.Height * 0.66, 0);

                int left = (int)Math.Round((double)((workArea.Width - width) / 2), 0);
                int top = (int)Math.Round((double)((workArea.Height - height) / 2), 0);

                rect = new Rectangle(new Point(left, top), new Size(width, height));
            }

            photino
                .Resize(rect.Value.Size)
                .MoveTo(rect.Value.Location);

            // Check if another window sits at the configured position,
            // nudge the window a bit if it would block underlying windows.
            if (_instances.Count > 0)
            {
                bool isOverlapping = _instances.Any(i => i.Location == photino.Location);
                
                if (isOverlapping)
                {
                    photino.Offset(20 * _instances.Count, 20 * _instances.Count);
                }
            }

            _instances.Add(photino);

            photino.Show();

            return photino;
        }
    
        public static void HandleJsonWebAction(object sender, string message)
        {
            PhotinoWindow photino = (PhotinoWindow)sender;
            PhotinoWebAction action = JsonSerializer.Deserialize<PhotinoWebAction>(message);

            Console.WriteLine($"Handling action: {action.Type}, {action.Command}");

            switch (action.Type.ToLower())
            {
                case "window":
                    HandleWindowCommand(photino, action.Command, action.Parameters);
                    break;

                case "message":
                    HandleMessageCommand(photino, action.Command, action.Parameters);
                    break;

                default:
                    photino.OpenAlertWindow("Error", $"Action {action.Type} unknown.");
                    break;
            }
        }

        public static void HandleWindowCommand(PhotinoWindow window, string command, Dictionary<string, string> parameters)
        {
            switch (command.ToLower())
            {
                case "create":
                    var photino = CreatePhotinoWindow(parameters.GetValueOrDefault("Title") ?? "New Window", null, window);
                    HandleWindowActionNavigation(photino, parameters.GetValueOrDefault("Url"));
                    photino.WaitforClose();
                    break;
                
                case "open":
                    // Bug:
                    // Navigating to a new resource does not
                    // work and crashes the application.
                    window.Title = parameters.GetValueOrDefault("Title") ?? window.Title;
                    HandleWindowActionNavigation(window, parameters.GetValueOrDefault("Url"));
                    break;

                case "maximize":
                    window.Maximize();
                    break;

                case "restore":
                    window.Restore();
                    break;

                case "close":
                    window.Close();
                    break;

                default:
                    window.OpenAlertWindow("Error", $"Window command {command} unknown.");
                    break;
            }
        }

        public static void HandleMessageCommand(PhotinoWindow window, string command, Dictionary<string, string> parameters)
        {
            switch (command.ToLower())
            {
                case "send":
                    List<Guid> windowIds = new List<Guid>();

                    string recipients = parameters.GetValueOrDefault("Recipients");
                    string message = parameters.GetValueOrDefault("Message");

                    // Look for recipients
                    if (recipients.ToLower() == "all")
                    {
                        windowIds = _instances
                            .Select(i => i.Id)
                            .ToList();
                    }
                    // else
                    // {
                    //     windowIds = (List<string>)recipients.Select((string)r => {
                    //         Guid windowId;
                    //         Guid.TryParse(r, windowId);
                    //     });
                    // }

                    // Send message
                    _instances
                        .Where(i => windowIds.Contains(i.Id))
                        .ToList()
                        .ForEach(i => i.SendWebMessage(message));

                    break;

                default:
                    window.OpenAlertWindow("Error", $"Window command {command} unknown.");
                    break;
            }
        }

        private static void HandleWindowActionNavigation(PhotinoWindow photino, string path)
        {
            if (path == null)
            {
                photino.OpenAlertWindow("Error", "No valid path to navigate to.");
                return;
            }

            if (path.Contains("http://") || path.Contains("https://"))
            {
                Uri uri = new Uri(path);
                photino.Load(uri);
            }
            else if (path.Contains(_apiSchema))
            {
                Uri uri = new Uri($"{_apiBaseUri}/{path.Replace(_apiSchema, "")}");
                photino.Load(uri);
            }
            else if (path.Contains(_assetSchema))
            {
                photino.Load(path.Replace(_assetSchema, "wwwroot/"));
            }
            else
            {
                Uri uri = new Uri(path, UriKind.Relative);
                photino.Load(uri);
            }

            return;
        }
    }
}
