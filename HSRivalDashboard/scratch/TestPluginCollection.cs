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
                Console.WriteLine("Loaded HearthMirror: " + hmAss.FullName);

                foreach (var type in hmAss.GetTypes())
                {
                    if (type.Name.Contains("Reflection") || type.Name.Contains("Client"))
                    {
                        Console.WriteLine("Type: " + type.FullName);
                        foreach (var m in type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
                        {
                            if (m.Name.Contains("Collection"))
                            {
                                Console.WriteLine("  Method: " + m.Name + " Params: " + m.GetParameters().Length);
                            }
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
