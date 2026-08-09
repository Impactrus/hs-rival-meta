using System;
using System.Reflection;

namespace TestPlugin
{
    class Program
    {
        static void Main()
        {
            try
            {
                var hmAss = Assembly.LoadFrom(@"C:\Users\szymo\AppData\Local\HearthstoneDeckTracker\app-1.55.3\HearthMirror.dll");
                foreach (var type in hmAss.GetTypes())
                {
                    if (type.Name.Contains("FullCollection"))
                    {
                        Console.WriteLine("Type: " + type.FullName);
                        foreach (var p in type.GetProperties())
                        {
                            Console.WriteLine("  Prop: " + p.Name + " Type: " + p.PropertyType);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex);
            }
        }
    }
}
