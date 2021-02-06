using System;
using System.Drawing;
using PhotinoNET;

namespace Bookchin.Library.App.PhotinoFluent
{
    public interface IPhotino
    {
        PhotinoNET.PhotinoNET Window { get; }

        Monitor Monitor { get; }
        Rectangle MonitorArea { get; }
        Rectangle WorkArea { get; }

        Photino Show();
        Photino Hide();
        Photino Close();

        Photino Resize(Size size);
        Photino Resize(int width, int height);
        Photino Minimize();
        Photino Maximize();
        Photino Fullscreen();

        Photino Move(Point location);
        Photino Move(int left, int top);
        Photino Offset(Point offset);
        Photino Offset(int left, int top);

        Photino NavigateTo(Uri uri);
        Photino NavigateTo(string path);

        Photino ShowMessage(string title, string message);

        Photino RegisterWebMessageHandler(EventHandler<string> handler);
    }
}