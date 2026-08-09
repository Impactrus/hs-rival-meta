using System;
using System.IO;

class Program
{
    static void Main()
    {
        var baseUri = new Uri(@"C:\Users\szymo\AppData\Local\HearthstoneDeckTracker\app-1.55.3\");
        var fileUri = new Uri(@"C:\Users\szymo\AppData\Roaming\HearthstoneDeckTracker\Plugins\HSRivalPlugin\HSRivalPlugin.dll");
        string relative = baseUri.MakeRelativeUri(fileUri).ToString();
        Console.WriteLine("Exact RelativeFilePath: " + relative);
    }
}
