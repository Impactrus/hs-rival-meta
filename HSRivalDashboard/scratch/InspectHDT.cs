using System;
using System.Reflection;
using System.Linq;

class Program
{
    static void Main()
    {
        var asm = Assembly.LoadFrom(@"C:\Users\szymo\AppData\Local\HearthstoneDeckTracker\app-1.55.3\HearthstoneDeckTracker.exe");
        foreach (var type in asm.GetTypes().Where(t => t.FullName.Contains("Collection") || t.FullName.Contains("HSReplay")))
        {
            Console.WriteLine(type.FullName);
            foreach (var m in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
            {
                if (m.Name.Contains("Collection") || m.Name.Contains("Upload"))
                    Console.WriteLine("  Method: " + m.Name);
            }
        }
    }
}
