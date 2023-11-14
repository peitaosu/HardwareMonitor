using Microsoft.Web.WebView2.Core;
using System.Diagnostics;
using System.Reflection;
using System.Security.Policy;
using System.Text.Json;

namespace HardwareMonitor
{
    public partial class View : Form
    {
        private int _minX = 1280;
        private int _minY = 800;

        public View()
        {
            InitializeComponent();
            this.Resize += new System.EventHandler(this.ResizeForm);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MinimumSize = new System.Drawing.Size(this._minX, this._minY);
            this.Size = this.MinimumSize;
            webView.Size = this.ClientSize;
            this.CenterToScreen();
            InitializeAsync();
        }

        private void ResizeForm(object sender, EventArgs e)
        {
            webView.Size = this.ClientSize - new System.Drawing.Size(webView.Location);
        }

        async void InitializeAsync()
        {
            var options = new CoreWebView2EnvironmentOptions(
                additionalBrowserArguments: "--disable-web-security --allow-file-access-from-files --allow-file-access");
            var webView2Environment = await CoreWebView2Environment.CreateAsync(null, SettingManager.GetSetting().WebCachePath, options);
            await webView.EnsureCoreWebView2Async(webView2Environment);
            NavigateTo("UI/dashboard.html", null);
            //webView.CoreWebView2.AddHostObjectToScript("utils", new Utils());
            webView.CoreWebView2.WebMessageReceived += ProcessWebMessage;
        }

        void ProcessWebMessage(object sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            WebMessage message = JsonSerializer.Deserialize<WebMessage>(args.WebMessageAsJson);
            if (message.Type == "GET")
            {
                dynamic result = null;
                webView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(result));
                return;
            }
            return;
        }

        private void NavigateTo(string page, string data)
        {
            string page_url = "localhost";
            if (SettingManager.GetSetting().UseOnlinePage)
            {
                // TODO
            }
            else
            {
                Uri local_url = new Uri(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
                page_url = local_url.ToString() + "/" + page;
            }
            if (webView != null && webView.CoreWebView2 != null)
            {
                webView.CoreWebView2.Settings.IsZoomControlEnabled = false;
                webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                webView.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;
#if DEBUG
                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
                webView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = true;
#else
                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                webView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
#endif
                webView.CoreWebView2.Navigate(page_url);
            }

        }
    }

    class WebMessage
    {
        public string Type { get; set; }
        public string Source { get; set; }
        public string Target { get; set; }
        public dynamic Message { get; set; }
    }

}