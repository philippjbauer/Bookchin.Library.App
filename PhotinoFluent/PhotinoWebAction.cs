using System.Collections.Generic;

namespace Bookchin.Library.App.PhotinoFluent
{
    public class PhotinoWebAction
    {
        public string Type { get; set; }
        public string Command { get; set; }
        public Dictionary<string, string> Parameters { get; set; }
    }
}