using System;
using System.Reflection;

class Program
{
    static void Main()
    {
        try
        {
            var hdtAsm = Assembly.LoadFile(@"C:\Users\szymo\AppData\Local\HearthstoneDeckTracker\app-1.55.3\HearthstoneDeckTracker.exe");
            var pluginAsm = Assembly.LoadFile(@"C:\Users\szymo\My project (1)\HSRivalPlugin\bin\Debug\net472\HSRivalPlugin.dll");
            var types = pluginAsm.GetTypes();
            Console.WriteLine("Loaded types: " + types.Length);
        }
        catch (ReflectionTypeLoadException ex)
        {
            Console.WriteLine("ReflectionTypeLoadException!");
            foreach (var loaderEx in ex.LoaderExceptions)
            {
                Console.WriteLine(" - " + loaderEx.Message);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Other Error: " + ex.Message);
        }
    }
}
