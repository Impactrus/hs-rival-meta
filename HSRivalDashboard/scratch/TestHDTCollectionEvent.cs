using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main()
    {
        var asm = Assembly.LoadFrom(@"C:\Users\szymo\AppData\Local\HearthstoneDeckTracker\app-1.55.3\HearthstoneDeckTracker.exe");
        var helperType = asm.GetType("Hearthstone_Deck_Tracker.Hearthstone.CollectionHelpers");
        var hsHelperField = helperType.GetProperty("Hearthstone").GetValue(null);
        
        var getCollMethod = hsHelperField.GetType().GetMethod("GetCollection");
        var task = (Task)getCollMethod.Invoke(hsHelperField, null);
        task.Wait();
        
        var resultProp = task.GetType().GetProperty("Result");
        var collObj = resultProp.GetValue(task);
        
        Console.WriteLine("Collection result obj: " + (collObj == null ? "NULL" : collObj.ToString()));
        if (collObj != null)
        {
            var cardsProp = collObj.GetType().GetProperty("Cards");
            var cardsDict = cardsProp.GetValue(collObj);
            Console.WriteLine("Cards Dict type: " + (cardsDict == null ? "NULL" : cardsDict.GetType().FullName));
            if (cardsDict != null)
            {
                var dict = cardsDict as System.Collections.IDictionary;
                Console.WriteLine("Total DBF_IDs in HDT Collection: " + (dict == null ? 0 : dict.Count));
                int sampleCount = 0;
                if (dict != null)
                {
                    foreach (System.Collections.DictionaryEntry kvp in dict)
                    {
                        if (sampleCount++ < 10)
                        {
                            var counts = kvp.Value as int[];
                            Console.WriteLine(string.Format("  DBF: {0} -> [{1}]", kvp.Key, string.Join(", ", counts)));
                        }
                    }
                }
            }
        }
    }
}
