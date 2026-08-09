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
                    if (type.FullName.StartsWith("HearthMirror.Objects"))
                    {
                        Console.WriteLine("Type: " + type.FullName);
                        foreach (var p in type.GetProperties())
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
