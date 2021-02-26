using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PhotinoNET;
using Flurl.Http;
using Bookchin.Library.API.Controllers.ViewModels;
using System.Threading;

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
                api.StartAsync();
                OpenMainWindow();
            }
        }

        private static void OpenMainWindow()
        {
            Console.WriteLine("Opening Main Window.");

            string windowTitle = "Bookchin Library";

            Action<PhotinoWindowOptions> windowConfiguration = options =>
            {
                options.WindowCreatedHandler += CreateAddInstanceEventHandler();
                options.WindowCreatedHandler += CreateCheckWindowOverlapEventHander();

                options.WindowClosingHandler += CreateRemoveInstanceEventHandler();
            };

            var mainWindow = new PhotinoWindow(windowTitle, windowConfiguration)
                .RegisterWebMessageReceivedHandler(HandleJsonWebAction)
                .Resize(1024, 768)
                .Center();
            
            OpenLoginWindow(mainWindow);

            mainWindow.WaitForClose();
        }

        private static void OpenLoginWindow(PhotinoWindow parent)
        {
            Console.WriteLine("Opening Login Window.");

            string windowTitle = "Please Login";

            Action<PhotinoWindowOptions> windowConfiguration = options =>
            {
                options.Parent = parent;

                options.WindowCreatedHandler += CreateAddInstanceEventHandler();
                options.WindowCreatedHandler += CreateCheckWindowOverlapEventHander();

                options.WindowClosingHandler += (object sender, EventArgs args) => {
                    var window = (PhotinoWindow)sender;
                    PhotinoWindow parent = window.Parent;

                    if (_jwtBearer != null)
                    {
                        parent.Load("wwwroot/index.html");
                    }
                    else
                    {
                        parent.Close();
                    }
                };

                options.WindowClosingHandler += CreateRemoveInstanceEventHandler();
            };

            // Create login window
            new PhotinoWindow(windowTitle, windowConfiguration)
                .RegisterWebMessageReceivedHandler(HandleJsonWebAction)
                .Resize(400, 500)
                .Center()
                .UserCanResize(false)
                .Load("wwwroot/login.html")
                .WaitForClose();
        }

        private static EventHandler CreateAddInstanceEventHandler()
        {
            return (object sender, EventArgs args) =>
            {
                var window = (PhotinoWindow)sender;
                _instances.Add(window);
            };
        }

        private static EventHandler CreateRemoveInstanceEventHandler()
        {
            return (object sender, EventArgs args) =>
            {
                var window = (PhotinoWindow)sender;
                _instances.Remove(window);
            };
        }

        private static EventHandler CreateCheckWindowOverlapEventHander()
        {
            return (object sender, EventArgs args) =>
            {
                var window = (PhotinoWindow)sender;

                if (_instances.Count > 0)
                {
                    int overlapsWithCount = _instances.Count(i => i.Location == window.Location);
                    
                    if (overlapsWithCount > 0)
                    {
                        int offset = 20 * overlapsWithCount;
                        window.Offset(offset, offset);
                    }
                }
            };
        }
    
        private static async void HandleJsonWebAction(object sender, string message)
        {
            var window = (PhotinoWindow)sender;

            try
            {
                PhotinoWebAction action = JsonSerializer.Deserialize<PhotinoWebAction>(message);

                Console.WriteLine($"Handling action: {action.Type}, {action.Command}");

                switch (action.Type.ToLower())
                {
                    case "window":
                        await HandleWindowCommand(window, action.Command, action.Parameters);
                        break;

                    case "message":
                        await HandleMessageCommand(window, action.Command, action.Parameters);
                        break;

                    case "user":
                        await HandleUserCommand(window, action.Command, action.Parameters);
                        break;

                    default:
                        throw new InvalidOperationException($"Action {action.Type} unknown.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                window.SendWebMessage(ex.Message);
            }
        }

        private static async Task HandleWindowCommand(PhotinoWindow window, string command, Dictionary<string, string> parameters)
        {
            switch (command.ToLower())
            {
                case "create":
                    string childWindowTitle = parameters.GetValueOrDefault("Title") ?? "New Window";

                    Action<PhotinoWindowOptions> childWindowOptions = options =>
                    {
                        options.Parent = window;

                        options.WindowCreatedHandler += CreateAddInstanceEventHandler();
                        options.WindowClosingHandler += CreateRemoveInstanceEventHandler();
                    };

                    var childWindow = new PhotinoWindow(childWindowTitle, childWindowOptions)
                        .RegisterWebMessageReceivedHandler(HandleJsonWebAction)
                        .Resize(window.Size)
                        .Center();

                    await HandleWindowActionNavigation(childWindow, parameters.GetValueOrDefault("Url"));

                    childWindow.WaitForClose();
                    break;
                
                case "open":
                    // Bug:
                    // Navigating to a new resource does not
                    // work and crashes the application.
                    //window.Title = parameters.GetValueOrDefault("Title") ?? window.Title;
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

        private static async Task HandleMessageCommand(PhotinoWindow window, string command, Dictionary<string, string> parameters)
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

        private static async Task HandleUserCommand(PhotinoWindow window, string command, Dictionary<string, string> parameters)
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
