using System;
using System.Reflection;

class Program
{
    static void Main()
    {
        try
        {
            var asm = Assembly.LoadFile(@"C:\Users\szymo\AppData\Roaming\HearthstoneDeckTracker\Plugins\HSRivalPlugin\HSRivalPlugin.dll");
            var types = asm.GetTypes();
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
