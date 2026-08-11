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
                System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "scraper_log.txt"), "Starting WebView2...\n");
                var env = await CoreWebView2Environment.CreateAsync(null, System.IO.Path.GetTempPath());
                await _webView.EnsureCoreWebView2Async(env);
                
                System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "scraper_log.txt"), "WebView2 loaded, navigating...\n");
                _webView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
                _webView.CoreWebView2.Navigate("https://hsreplay.net/analytics/query/list_decks_by_win_rate_v2/?GameType=RANKED_STANDARD&LeagueRankRange=GOLD&Region=ALL&TimeRange=CURRENT_EXPANSION");
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "scraper_log.txt"), "Error: " + ex.ToString() + "\n");
                Application.Exit();
            }
        }

        private async void CoreWebView2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "scraper_log.txt"), "Navigation completed. Success: " + e.IsSuccess + "\n");
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
                System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "scraper_log.txt"), "JSON Length: " + jsonContent?.Length + "\n");
                
                // WebView2 returns strings wrapped in quotes
                if (!string.IsNullOrEmpty(jsonContent) && jsonContent.Length > 2)
                {
                    jsonContent = jsonContent.Substring(1, jsonContent.Length - 2);
                    // Unescape JSON (WebView2 escapes quotes)
                    jsonContent = System.Text.RegularExpressions.Regex.Unescape(jsonContent);

                    if (jsonContent.StartsWith("{"))
                    {
                        System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "scraper_log.txt"), "Valid JSON found, posting to " + _serverUrl + "\n");
                        // Valid JSON payload, POST to our backend!
                        using (var client = new HttpClient())
                        {
                            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                            var response = await client.PostAsync($"{_serverUrl.TrimEnd('/')}/api/meta/hsreplay-sync", content);
                            System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "scraper_log.txt"), "POST result: " + response.StatusCode + "\n");
                        }
                    }
                    else if (jsonContent.Contains("Just a moment") || jsonContent.Contains("Cloudflare") || jsonContent.Contains("cf-browser-verification"))
                    {
                        System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "scraper_log.txt"), "Hit Cloudflare, showing window to user...\n");
                        
                        // Show the window so user can solve the captcha!
                        this.Invoke((MethodInvoker)delegate {
                            this.Opacity = 1;
                            this.WindowState = FormWindowState.Normal;
                            this.ShowInTaskbar = true;
                            base.SetVisibleCore(true);
                            this.BringToFront();
                            MessageBox.Show(this, "HS Rival Meta: Cloudflare zablokował automatyczne pobieranie. Proszę rozwiąż captcha (zaznacz 'I am human'), abyśmy mogli zsynchronizować statystyki! Okno zamknie się automatycznie po udanej weryfikacji.", "Wymagana weryfikacja", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        });

                        // Poll every 3 seconds until JSON appears
                        for (int i = 0; i < 20; i++) 
                        {
                            await Task.Delay(3000);
                            jsonContent = await _webView.CoreWebView2.ExecuteScriptAsync("document.body.innerText;");
                            if (!string.IsNullOrEmpty(jsonContent) && jsonContent.Length > 2)
                            {
                                string unescaped = System.Text.RegularExpressions.Regex.Unescape(jsonContent.Substring(1, jsonContent.Length - 2));
                                if (unescaped.StartsWith("{"))
                                {
                                    System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "scraper_log.txt"), "Valid JSON found after manual captcha solve!\n");
                                    using (var client = new HttpClient())
                                    {
                                        var content = new StringContent(unescaped, Encoding.UTF8, "application/json");
                                        await client.PostAsync($"{_serverUrl.TrimEnd('/')}/api/meta/hsreplay-sync", content);
                                    }
                                    break;
                                }
                            }
                        }
                    }
                    else {
                        System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "scraper_log.txt"), "No valid JSON and no Cloudflare? Content starts with: " + (jsonContent.Length > 20 ? jsonContent.Substring(0, 20) : jsonContent) + "\n");
                    }
                }
            }
            catch (Exception ex) { 
                System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "scraper_log.txt"), "Exception in NavigationCompleted: " + ex.ToString() + "\n");
            }
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