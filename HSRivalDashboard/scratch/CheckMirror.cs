using System;
using System.Reflection;

class Program
{
    static void Main()
    {
        try
        {
            var hdtAsm = Assembly.LoadFile(@"C:\Users\szymo\AppData\Local\HearthstoneDeckTracker\app-1.55.3\HearthstoneDeckTracker.exe");
            var mirrorAsm = Assembly.LoadFile(@"C:\Users\szymo\AppData\Local\HearthstoneDeckTracker\app-1.55.3\HearthMirror.dll");
            
            var reflectionType = mirrorAsm.GetType("HearthMirror.Reflection");
            if (reflectionType != null)
            {
                var clientType = reflectionType.GetNestedType("Client");
                if (clientType != null)
                {
                    var getFullCollMethod = clientType.GetMethod("GetFullCollection");
                    if (getFullCollMethod != null)
                    {
                        var fullColl = getFullCollMethod.Invoke(null, null);
                        Console.WriteLine("GetFullCollection result: " + (fullColl != null ? fullColl.ToString() : "null"));
                        if (fullColl != null)
                        {
                            var cardsProp = fullColl.GetType().GetProperty("Cards");
                            var cards = cardsProp != null ? cardsProp.GetValue(fullColl, null) as System.Collections.IList : null;
                            Console.WriteLine("Cards count: " + (cards != null ? cards.Count : 0));
                        }
                    }

                    var getCollMethod = clientType.GetMethod("GetCollection");
                    if (getCollMethod != null)
                    {
                        var coll = getCollMethod.Invoke(null, null) as System.Collections.IList;
                        Console.WriteLine("GetCollection count: " + (coll != null ? coll.Count : 0));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.ToString());
        }
    }
}
