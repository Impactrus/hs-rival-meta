using System;
using System.Windows;

namespace HSRivalPlugin
{
    public partial class SettingsWindow : Window
    {
        private readonly PluginConfig _config;

        public SettingsWindow(PluginConfig config)
        {
            InitializeComponent();
            _config = config;

            TxtUserToken.Text = _config.UserToken;
            TxtServerUrl.Text = string.IsNullOrWhiteSpace(_config.ServerUrl) ? "https://hs-rival-meta.onrender.com" : _config.ServerUrl;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            _config.UserToken = TxtUserToken.Text.Trim();
            _config.ServerUrl = string.IsNullOrWhiteSpace(TxtServerUrl.Text) ? "https://hs-rival-meta.onrender.com" : TxtServerUrl.Text.Trim();
            _config.Save();
            DialogResult = true;
            Close();
        }

        private async void BtnSyncNow_Click(object sender, RoutedEventArgs e)
        {
            TxtStatus.Text = "Synchronizowanie kolekcji...";
            BtnSyncNow.IsEnabled = false;

            try
            {
                _config.UserToken = TxtUserToken.Text.Trim();
                _config.ServerUrl = string.IsNullOrWhiteSpace(TxtServerUrl.Text) ? "https://hs-rival-meta.onrender.com" : TxtServerUrl.Text.Trim();
                _config.Save();

                string msg = await Plugin.SyncCollectionAsync();
                TxtStatus.Text = msg;
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"Błąd synchronizacji: {ex.Message}";
            }
            finally
            {
                BtnSyncNow.IsEnabled = true;
            }
        }
    }
}
