using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace HSRivalInstaller
{
    internal class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string hdtDir = Path.Combine(appData, "HearthstoneDeckTracker");
                string pluginDir = Path.Combine(hdtDir, "Plugins", "HSRivalPlugin");

                if (!Directory.Exists(pluginDir))
                {
                    Directory.CreateDirectory(pluginDir);
                }

                string dllPath = Path.Combine(pluginDir, "HSRivalPlugin.dll");

                // Extract embedded HSRivalPlugin.dll
                var assembly = Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream("HSRivalInstaller.HSRivalPlugin.dll"))
                {
                    if (stream != null)
                    {
                        using (var fileStream = File.Create(dllPath))
                        {
                            stream.CopyTo(fileStream);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Nie odnaleziono pliku wtyczki w instalatorze.", "Błąd instalacji", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                // Auto-enable plugin in HDT plugins.xml config
                try
                {
                    string pluginsXmlPath = Path.Combine(hdtDir, "plugins.xml");
                    string xmlContent = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
                        "<ArrayOfPluginSettings xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">\r\n" +
                        "  <PluginSettings>\r\n" +
                        "    <FileName>HSRivalPlugin\\HSRivalPlugin.dll</FileName>\r\n" +
                        "    <IsEnabled>true</IsEnabled>\r\n" +
                        "    <Name>HS Rival Meta Sync</Name>\r\n" +
                        "  </PluginSettings>\r\n" +
                        "</ArrayOfPluginSettings>";
                    File.WriteAllText(pluginsXmlPath, xmlContent);
                }
                catch { }

                MessageBox.Show(
                    "✅ Wtyczka HS Rival Meta została pomyślnie zainstalowana i włączona w Twoim Hearthstone Deck Trackerze!\n\n" +
                    "Zrestartuj oficjalny Hearthstone Deck Tracker, aby wczytać wtyczkę.",
                    "Instalacja Wtyczki Zakończona",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd instalacji wtyczki: " + ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
