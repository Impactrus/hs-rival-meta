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
                string pluginDir = Path.Combine(appData, "HearthstoneDeckTracker", "Plugins", "HSRivalPlugin");

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

                MessageBox.Show(
                    "✅ Wtyczka HS Rival Meta została pomyślnie zainstalowana w Twoim Hearthstone Deck Trackerze!\n\n" +
                    "Wystarczy, że uruchomisz oficjalny Hearthstone Deck Tracker oraz naszą stronę w przeglądarce — połączą się automatycznie!",
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
