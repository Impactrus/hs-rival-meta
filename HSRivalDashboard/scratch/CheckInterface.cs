using System;
using System.Reflection;

class Program
{
    static void Main()
    {
        try
        {
            var hdtAsm = Assembly.LoadFile(@"C:\Users\szymo\AppData\Local\HearthstoneDeckTracker\app-1.55.3\HearthstoneDeckTracker.exe");
            var pluginAsm = Assembly.LoadFile(@"C:\Users\szymo\My project (1)\HSRivalPlugin\bin\Debug\net48\HSRivalPlugin.dll");
            
            var pTypeInterface = hdtAsm.GetType("Hearthstone_Deck_Tracker.Plugins.IPlugin");
            if (pTypeInterface != null)
                Console.WriteLine("Interface Type: " + pTypeInterface.FullName);

            foreach (var type in pluginAsm.GetTypes())
            {
                if (!type.IsPublic || type.IsAbstract) continue;
                
                var typeInterface = type.GetInterface(pTypeInterface.ToString(), true);
                if (typeInterface != null)
                {
                    Console.WriteLine("FOUND PLUGIN: " + type.FullName);
                    bool isAssignable = pTypeInterface.IsAssignableFrom(type);
                    Console.WriteLine("IsAssignableFrom: " + isAssignable);
                    
                    try
                    {
                        var instance = Activator.CreateInstance(type);
                        Console.WriteLine("Instance created: " + (instance != null));
                        bool isIPlugin = pTypeInterface.IsInstanceOfType(instance);
                        Console.WriteLine("Instance is IPlugin: " + isIPlugin);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Activator.CreateInstance threw: " + ex.ToString());
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
    }
}
