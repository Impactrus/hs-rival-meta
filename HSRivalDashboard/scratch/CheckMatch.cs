using System;
using System.IO;
using System.Linq;
using System.Reflection;

class Program
{
    static void Main()
    {
        try
        {
            var baseDir = @"C:\Users\szymo\AppData\Local\HearthstoneDeckTracker\app-1.55.3\";
            var file = @"C:\Users\szymo\AppData\Roaming\HearthstoneDeckTracker\Plugins\HSRivalPlugin\HSRivalPlugin.dll";

            var baseUri = new Uri(baseDir);
            var fileUri = new Uri(file);
            string relativePath = baseUri.MakeRelativeUri(fileUri).ToString();

            Console.WriteLine("Computed RelativeFilePath: '" + relativePath + "'");

            string settingFileName = "../../../Roaming/HearthstoneDeckTracker/Plugins/HSRivalPlugin/HSRivalPlugin.dll";
            string settingName = "HS Rival Meta Sync";

            bool pathMatch = relativePath == settingFileName;
            Console.WriteLine("Path match: " + pathMatch);

            var pluginAsm = Assembly.LoadFile(file);
            var type = pluginAsm.GetType("HSRivalPlugin.Plugin");
            var pluginObj = Activator.CreateInstance(type);
            var nameProp = type.GetProperty("Name");
            string pluginName = (string)nameProp.GetValue(pluginObj, null);

            Console.WriteLine("Plugin.Name: '" + pluginName + "'");
            bool nameMatch = pluginName == settingName;
            Console.WriteLine("Name match: " + nameMatch);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.ToString());
        }
    }
}
