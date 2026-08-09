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
                var hmObjAss = Assembly.LoadFrom(@"C:\Users\szymo\AppData\Local\HearthstoneDeckTracker\app-1.55.3\HearthMirror.Objects.dll");
                Console.WriteLine("Loaded objects: " + hmObjAss.FullName);
                foreach (var type in hmObjAss.GetTypes())
                {
                    if (type.Name.Contains("FullCollection") || type.Name.Contains("Collection"))
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
