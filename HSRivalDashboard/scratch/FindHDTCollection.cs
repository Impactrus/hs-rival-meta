using System;
using System.Reflection;

namespace Test
{
    class Program
    {
        static void Main()
        {
            try
            {
                var ass = Assembly.LoadFrom(@"C:\Users\szymo\AppData\Local\HearthstoneDeckTracker\app-1.55.3\HearthstoneDeckTracker.exe");
                Console.WriteLine("Loaded HDT Assembly!");

                foreach (var type in ass.GetTypes())
                {
                    if (type.Name.Contains("Collection") || type.Name.Contains("HsReplay"))
                    {
                        Console.WriteLine("Type: " + type.FullName);
                        foreach (var m in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
                        {
                            if (m.Name.Contains("Collection") || m.Name.Contains("Get") || m.Name.Contains("Update"))
                            {
                                Console.WriteLine("  Method: " + m.Name);
                            }
                        }
                        foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
                        {
                            Console.WriteLine("  Prop: " + p.Name);
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
