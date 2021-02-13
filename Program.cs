using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PhotinoNET;
using Flurl.Http;
using Bookchin.Library.API.Controllers.ViewModels;

namespace Bookchin.Library.App
{
    class Program
    {
        private static string _apiBaseUri = "http://localhost:7645";
        private static string _apiSchema = "api://";
        private static string _assetSchema = "asset://";

        private static List<PhotinoWindow> _instances = new List<PhotinoWindow>();

        private static string _jwtBearer;

        [STAThread]
        static void Main(string[] args)
        {
            // Configure internal WebHost for API project
            string[] apiArgs = new string[] {
                "ENVIRONMENT=Development",
                $"URLS={_apiBaseUri}",
                "ConnectionStrings:DefaultConnection=Data Source=../Bookchin.Library.API/Data/BookchinLibrary.db"
            };

            using (var api = Bookchin.Library.API.Program.CreateHostBuilder(apiArgs).Build())
            { 
                // Start Api Host
                api.StartAsync();

                // Create and configure main window
                PhotinoWindow mainWindow = CreatePhotinoWindow("Bookchin Library")
                    .RegisterWebMessageReceivedHandler(HandleJsonWebAction)
                    .Load("wwwroot/pre-login.html");

                // Create login window
                int loginWindowWidth = 400;
                int loginWindowHeight = 500;
                var loginWindowRect = new Rectangle(
                    (mainWindow.MainMonitor.WorkArea.Width / 2) - (loginWindowWidth / 2),
                    (mainWindow.MainMonitor.WorkArea.Height / 2) - (loginWindowHeight / 2),
                    loginWindowWidth,
                    loginWindowHeight
                );
                
                CreatePhotinoWindow("Please Login", loginWindowRect, mainWindow)
                    .IsResizable(false)
                    .RegisterWebMessageReceivedHandler(HandleJsonWebAction)
                    .RegisterWindowClosingHandler((window, args) => {
                        if (_jwtBearer != null)
                        {
                            mainWindow.Load("wwwroot/index.html");
                        }
                        else
                        {
                            // Bug:
                            // The event is not fired when user
                            // clicks on the window chrome close
                            // button and thus does not trigger
                            // the Dispose() method of the 
                            // PhotinoWindow instance.
                            mainWindow.Close();
                        }
                    })
                    .Load("wwwroot/login.html")
                    .WaitforClose();

                // Wait for program end
                mainWindow.WaitforClose();
            }
        }


        public static PhotinoWindow CreatePhotinoWindow(string title, Rectangle? rect = null, PhotinoWindow parent = null)
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

            return photino;
        }
    
        public static async void HandleJsonWebAction(object sender, string message)
        {
            PhotinoWindow photino = (PhotinoWindow)sender;

            try
            {
                PhotinoWebAction action = JsonSerializer.Deserialize<PhotinoWebAction>(message);

                Console.WriteLine($"Handling action: {action.Type}, {action.Command}");

                switch (action.Type.ToLower())
                {
                    case "window":
                        await HandleWindowCommand(photino, action.Command, action.Parameters);
                        break;

                    case "message":
                        await HandleMessageCommand(photino, action.Command, action.Parameters);
                        break;

                    case "user":
                        await HandleUserCommand(photino, action.Command, action.Parameters);
                        break;

                    default:
                        throw new InvalidOperationException($"Action {action.Type} unknown.");
                }
            }
            catch (Exception ex)
            {
                photino.SendWebMessage(ex.Message);
            }
        }

        public static async Task HandleWindowCommand(PhotinoWindow window, string command, Dictionary<string, string> parameters)
        {
            switch (command.ToLower())
            {
                case "create":
                    var photino = CreatePhotinoWindow(parameters.GetValueOrDefault("Title") ?? "New Window", null, window);
                    await HandleWindowActionNavigation(photino, parameters.GetValueOrDefault("Url"));
                    photino.WaitforClose();
                    break;
                
                case "open":
                    // Bug:
                    // Navigating to a new resource does not
                    // work and crashes the application.
                    window.Title = parameters.GetValueOrDefault("Title") ?? window.Title;
                    await HandleWindowActionNavigation(window, parameters.GetValueOrDefault("Url"));
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
                    throw new InvalidOperationException($"Window command {command} unknown.");
            }
        }

        public static async Task HandleMessageCommand(PhotinoWindow window, string command, Dictionary<string, string> parameters)
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
                    throw new InvalidOperationException($"Window command {command} unknown.");
            }
        }

        public static async Task HandleUserCommand(PhotinoWindow window, string command, Dictionary<string, string> parameters)
        {
            switch (command.ToLower())
            {
                case "authenticate":
                    string username = parameters.GetValueOrDefault("Username");
                    string password = parameters.GetValueOrDefault("Password");

                    if (username == null || password == null)
                    {
                        throw new ArgumentNullException("Please enter both username and password.");
                    }

                    try
                    {
                        var jwtBearer = await $"{_apiBaseUri}/Authenticate"
                            .PostJsonAsync(new
                            {
                                username = username,
                                password = password
                            })
                            .ReceiveJson<JwtTokenViewModel>();

                        _jwtBearer = jwtBearer.Token;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        window.SendWebMessage("Invalid user credentials.");
                        break;
                    }

                    window.Close();

                    break;

                default:
                    throw new InvalidOperationException($"User command {command} unknown.");
            }
        }

        private static async Task HandleWindowActionNavigation(PhotinoWindow photino, string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("No path to navigate to.");
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
