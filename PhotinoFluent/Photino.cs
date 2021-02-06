using System;
using System.Drawing;
using System.Linq;
using PhotinoNET;

namespace Bookchin.Library.App.PhotinoFluent
{
    public class Photino : IPhotino
    {
        private PhotinoNET.PhotinoNET _window;
        private Size _lastSize;
        private Point _lastLocation;

        public PhotinoNET.PhotinoNET Window => _window;

        public Monitor Monitor => this.Window.Monitors.First();
        public Rectangle MonitorArea => this.Monitor.MonitorArea;
        public Rectangle WorkArea => this.Monitor.WorkArea;

        public Photino(string title)
        {
            _window = new PhotinoNET.PhotinoNET(title, configure => { });
        }

        public Photino(PhotinoNET.PhotinoNET window)
        {
            _window = window;
            _lastSize = window.Size;
            _lastLocation = window.Location;
        }

        public Photino Show()
        {
            Console.WriteLine("Executing: Photino.Show()");
            
            this.Window.Show();

            return this;
        }

        public Photino Hide()
        {
            Console.WriteLine("Executing: Photino.Hide()");
            
            throw new NotImplementedException("Hide is not yet implemented in PhotinoNET.");
        }

        public Photino Close()
        {
            Console.WriteLine("Executing: Photino.Close()");
            
            throw new NotImplementedException("Close is not yet implemented in PhotinoNET.");
        }

        public Photino Resize(Size size)
        {
            Console.WriteLine("Executing: Photino.Resize(Size size)");
            Console.WriteLine($"Current size: {this.Window.Size}");
            Console.WriteLine($"New size: {size}");

            // Save last size
            _lastSize = this.Window.Size;

            this.Window.Size = size;

            return this;
        }

        public Photino Resize(int width, int height)
        {
            Console.WriteLine("Executing: Photino.Resize(int width, int height)");
            
            return this.Resize(new Size(width, height));
        }

        public Photino Minimize()
        {
            Console.WriteLine("Executing: Photino.Minimize()");
            
            throw new NotImplementedException("Minimize is not yet implemented in PhotinoNET.");
        }

        public Photino Maximize()
        {
            Console.WriteLine("Executing: Photino.Maximize()");
            
            return this
                .Move(0, 0)
                .Resize(this.WorkArea.Width, this.WorkArea.Height);
        }

        public Photino Fullscreen()
        {
            Console.WriteLine("Executing: Photino.Fullscreen()");
            
            throw new NotImplementedException("Fullscreen is not yet implemented in PhotinoNET.");
        }

        public Photino Restore()
        {
            Console.WriteLine("Executing: Photino.Restore()");
            Console.WriteLine($"Last location: {_lastLocation}");
            Console.WriteLine($"Last size: {_lastSize}");
            
            bool isRestorable = _lastSize.IsEmpty;

            if (isRestorable == false)
            {
                return this;
            }

            return this
                .Move(_lastLocation.X, _lastLocation.Y)
                .Resize(_lastSize.Width, _lastSize.Height);
        }

        public Photino Move(Point location)
        {
            Console.WriteLine("Executing: Photino.Move(Point location)");
            Console.WriteLine($"Current location: {this.Window.Location}");
            Console.WriteLine($"New location: {location}");
            
            // Save last location
            _lastLocation = this.Window.Location;

            // Bug:
            // For some reason the vertical position is not handled correctly.
            // Whenever a positive value is set, the window appears at the
            // very bottom of the screen and the only visible thing is the
            // application window title bar. As a workaround we make a 
            // negative value out of the vertical position to "pull" the window up.
            location.Y = location.Y >= 0
                ? location.Y - this.WorkArea.Height
                : location.Y;

            this.Window.Location = location;

            return this;
        }

        public Photino Move(int left, int top)
        {
            Console.WriteLine("Executing: Photino.Move(int left, int top)");
            
            return this.Move(new Point(left, top));
        }

        public Photino Offset(Point offset)
        {
            Console.WriteLine("Executing: Photino.Offset(Point offset)");
            
            Point location = this.Window.Location;

            int left = location.X + offset.X;
            int top = location.Y + offset.Y;

            return this.Move(left, top);
        }

        public Photino Offset(int left, int top)
        {
            Console.WriteLine("Executing: Photino.Offset(int left, int top)");
            
            return this.Offset(new Point(left, top));
        }

        public Photino NavigateTo(Uri uri)
        {
            Console.WriteLine("Executing: Photino.NavigateTo(Uri uri)");
            
            // ––––––––––––––––––––––
            // SECURITY RISK!
            // This needs validation!
            // ––––––––––––––––––––––
            this.Window.NavigateToUrl(uri.ToString());

            return this;
        }

        public Photino NavigateTo(string path)
        {
            Console.WriteLine("Executing: Photino.NavigateTo(string path)");
            
            // ––––––––––––––––––––––
            // SECURITY RISK!
            // This needs validation!
            // ––––––––––––––––––––––
            this.Window.NavigateToLocalFile(path);

            return this;
        }

        public Photino ShowMessage(string title, string message)
        {
            Console.WriteLine("Executing: Photino.ShowMessage(string title, string message)");
            
            // Bug:
            // Closing the message shown with the ShowMessage
            // method closes the sender window as well.
            this.Window.ShowMessage(title, message);

            return this;
        }

        public Photino RegisterWebMessageHandler(EventHandler<string> handler)
        {
            Console.WriteLine("Executing: Photino.RegisterWebMessageHandler(EventHandler<string> handler)");
            
            this.Window.OnWebMessageReceived += handler;

            return this;
        }
    }
}