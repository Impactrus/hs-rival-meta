using System;
using System.Reflection;

class Program
{
    static void Main()
    {
        try
        {
            var hdtAsm = Assembly.LoadFile(@"C:\Users\szymo\AppData\Local\HearthstoneDeckTracker\app-1.55.3\HearthstoneDeckTracker.exe");
            var helperType = hdtAsm.GetType("Hearthstone_Deck_Tracker.Hearthstone.CollectionHelpers");
            Console.WriteLine("CollectionHelpers Type: " + (helperType != null ? helperType.FullName : "null"));
            if (helperType != null)
            {
                var hearthstoneProp = helperType.GetProperty("Hearthstone", BindingFlags.Public | BindingFlags.Static);
                Console.WriteLine("Hearthstone Prop: " + (hearthstoneProp != null ? hearthstoneProp.Name : "null"));
                if (hearthstoneProp != null)
                {
                    var instance = hearthstoneProp.GetValue(null, null);
                    Console.WriteLine("Hearthstone Instance: " + (instance != null ? instance.ToString() : "null"));
                    if (instance != null)
                    {
                        var getCollMethod = instance.GetType().GetMethod("GetCollection");
                        if (getCollMethod != null)
                        {
                            var task = getCollMethod.Invoke(instance, null) as System.Threading.Tasks.Task;
                            if (task != null)
                            {
                                task.Wait();
                                var resultProp = task.GetType().GetProperty("Result");
                                var collResult = resultProp != null ? resultProp.GetValue(task, null) : null;
                                Console.WriteLine("Collection Result: " + (collResult != null ? collResult.ToString() : "null"));
                                if (collResult != null)
                                {
                                    var cardsProp = collResult.GetType().GetProperty("Cards");
                                    var cardsObj = cardsProp != null ? cardsProp.GetValue(collResult, null) as System.Collections.IDictionary : null;
                                    Console.WriteLine("Cards Dict Count: " + (cardsObj != null ? cardsObj.Count : 0));
                                    var dustProp = collResult.GetType().GetProperty("Dust");
                                    Console.WriteLine("Dust: " + (dustProp != null ? dustProp.GetValue(collResult, null) : 0));
                                }
                            }
                        }
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
