using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace HSRivalScraper
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            string serverUrl = args.Length > 0 ? args[0] : "http://localhost:5123";
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new HiddenScraperForm(serverUrl));
        }
    }

    public class HiddenScraperForm : Form
    {
        private WebView2 _webView;
        private Timer _timeoutTimer;
        private string _serverUrl;

        public HiddenScraperForm(string serverUrl)
        {
            _serverUrl = serverUrl;
            // Keep window hidden
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Opacity = 0;
            this.Width = 800;
            this.Height = 600;

            _webView = new WebView2();
            _webView.Dock = DockStyle.Fill;
            this.Controls.Add(_webView);

            // Failsafe timeout 30s
            _timeoutTimer = new Timer();
            _timeoutTimer.Interval = 30000;
            _timeoutTimer.Tick += (s, e) => Application.Exit();

            this.Load += async (s, e) => await InitializeScraping();
        }

        private async Task InitializeScraping()
        {
            _timeoutTimer.Start();
            
            try
            {
                var env = await CoreWebView2Environment.CreateAsync(null, System.IO.Path.GetTempPath());
                await _webView.EnsureCoreWebView2Async(env);
                
                _webView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
                _webView.CoreWebView2.Navigate("https://hsreplay.net/analytics/query/list_decks_by_win_rate_v2/?GameType=RANKED_STANDARD&LeagueRankRange=GOLD&Region=ALL&TimeRange=CURRENT_EXPANSION");
            }
            catch (Exception)
            {
                Application.Exit();
            }
        }

        private async void CoreWebView2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
            {
                Application.Exit();
                return;
            }

            try
            {
                // Wait a bit for Cloudflare just in case it redirects
                await Task.Delay(2500);

                string jsonContent = await _webView.CoreWebView2.ExecuteScriptAsync("document.body.innerText;");
                
                // WebView2 returns strings wrapped in quotes
                if (!string.IsNullOrEmpty(jsonContent) && jsonContent.Length > 2)
                {
                    jsonContent = jsonContent.Substring(1, jsonContent.Length - 2);
                    // Unescape JSON (WebView2 escapes quotes)
                    jsonContent = System.Text.RegularExpressions.Regex.Unescape(jsonContent);

                    if (jsonContent.StartsWith("{"))
                    {
                        // Valid JSON payload, POST to our backend!
                        using (var client = new HttpClient())
                        {
                            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                            await client.PostAsync($"{_serverUrl.TrimEnd('/')}/api/meta/hsreplay-sync", content);
                        }
                    }
                    else if (jsonContent.Contains("Just a moment"))
                    {
                        // Hit cloudflare challenge. We should wait a few seconds and try executing script again.
                        await Task.Delay(4000);
                        jsonContent = await _webView.CoreWebView2.ExecuteScriptAsync("document.body.innerText;");
                        if (!string.IsNullOrEmpty(jsonContent) && jsonContent.Length > 2)
                        {
                            jsonContent = jsonContent.Substring(1, jsonContent.Length - 2);
                            jsonContent = System.Text.RegularExpressions.Regex.Unescape(jsonContent);
                            if (jsonContent.StartsWith("{"))
                            {
                                using (var client = new HttpClient())
                                {
                                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                                    await client.PostAsync($"{_serverUrl.TrimEnd('/')}/api/meta/hsreplay-sync", content);
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            finally
            {
                Application.Exit();
            }
        }
        
        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(false); // Force completely hidden
        }
    }
}