using System;
using System.Drawing;
using System.Linq;
using PhotinoNET;

namespace Bookchin.Library.App.PhotinoFluent
{
    public class Photino : IPhotino
    {
        private PhotinoNET.PhotinoNET _window;

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
        }

        public Photino Show()
        {
            this.Window.Show();

            return this;
        }

        public Photino Hide()
        {
            Console.WriteLine("Hide is not implemented on Photino Window yet.");

            return this;
        }

        public Photino Close()
        {
            Console.WriteLine("Close is not implemented on Photino Window yet.");

            return this;
        }

        public Photino Resize(Size size)
        {
            this.Window.Size = size;

            return this;
        }

        public Photino Resize(int width, int height)
        {
            return this.Resize(new Size(width, height));
        }

        public Photino Minimize()
        {
            Console.WriteLine("Minimize is not implemented on Photino Window yet.");

            return this;
        }

        public Photino Maximize()
        {
            return this.Resize(this.WorkArea.Width, this.WorkArea.Height);
        }

        public Photino Fullscreen()
        {
            Console.WriteLine("Close is not implemented on Photino Window yet.");

            return this;
        }

        public Photino Move(Point location)
        {
            // Bug:
            // For some reason the vertical position is not handled correctly.
            // Whenever a positive value is set, the window appears at the
            // very bottom of the screen and the only visible thing is the
            // application window title bar. As a workaround we make a 
            // negative value out of the vertical position to "pull" the window up.
            location.Y = location.Y > 0
                ? location.Y - this.WorkArea.Height
                : location.Y;

            this.Window.Location = location;

            return this;
        }

        public Photino Move(int left, int top)
        {
            return this.Move(new Point(left, top));
        }

        public Photino Offset(Point offset)
        {
            Point location = this.Window.Location;

            int left = location.X + offset.X;
            int top = location.Y + offset.Y;

            return this.Move(left, top);
        }

        public Photino Offset(int left, int top)
        {
            return this.Offset(new Point(left, top));
        }

        public Photino NavigateTo(Uri uri)
        {
            // ––––––––––––––––––––––
            // SECURITY RISK!
            // This needs validation!
            // ––––––––––––––––––––––
            this.Window.NavigateToUrl(uri.ToString());

            return this;
        }

        public Photino NavigateTo(string path)
        {
            // ––––––––––––––––––––––
            // SECURITY RISK!
            // This needs validation!
            // ––––––––––––––––––––––
            this.Window.NavigateToLocalFile(path);

            return this;
        }

        public Photino ShowMessage(string title, string message)
        {
            // Bug:
            // Closing the message shown with the ShowMessage
            // method closes the sender window as well.
            this.Window.ShowMessage(title, message);

            return this;
        }

        public Photino RegisterWebMessageHandler(EventHandler<string> handler)
        {
            this.Window.OnWebMessageReceived += handler;

            return this;
        }
    }
}