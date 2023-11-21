using Microsoft.Web.WebView2.Core;
using System.Reflection;
using System.Text.Json;

namespace HardwareMonitor
{
    public partial class View : Form
    {
        private int _minX = 1280;
        private int _minX_stack = 480;
        private int _minY = 800;
        private double _opacity = 1.0;
        private double _opacity_changed = 0.8;
        private bool _dragging = false;
        private Point _dragcursor;
        private Point _dragfrom;
        private Database db;

        public View()
        {
            InitializeComponent();
            this.Resize += new System.EventHandler(this.ResizeForm);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MinimumSize = new System.Drawing.Size(this._minX, this._minY);
            this.Size = this.MinimumSize;
            this.Opacity = this._opacity;
            webView.Size = this.ClientSize;
            this.CenterToScreen();
            InitializeAsync();
            db = new Database(SettingManager.GetSetting().DatabasePath);
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
            dynamic result = null;
            long machine_id = 0;
            
            switch (message.Type)
            {
                case "GET":
                    switch (message.Target)
                    {
                        case "Hardware":
                            if (message.Message.ContainsKey("MachineID") && message.Message["MachineID"].GetInt64() > 0)
                            {
                                machine_id = message.Message["MachineID"].GetInt64();
                            }
                            else
                            {
                                dynamic machine = Hardware.Instance.GetMachine();
                                machine_id = db.GetMachine(machine.MachineName, machine.URI);
                                if (machine_id == 0)
                                    db.SaveMachine(machine.MachineName, machine.URI, Utils.GetUTCTimestamp());
                                machine_id = db.GetMachine(machine.MachineName, machine.URI);
                            }
                            if (message.Message.ContainsKey("Event"))
                            {
                                if (message.Message["Event"].GetString() == "Start")
                                {
                                    Monitor.Instance.Stop();
                                    Monitor.Instance.MonitorHardware(db, machine_id, 5);
                                    Monitor.Instance.Start();
                                    break;
                                }
                                if (message.Message["Event"].GetString() == "Fetch")
                                {
                                    if (message.Message.ContainsKey("Last"))
                                    {
                                        webView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new { Type = "Hardware", Data = Monitor.Instance.FetchHardware(machine_id, message.Message["Last"].GetInt32()), Machine = machine_id }));
                                        break;
                                    }
                                }
                            }
                            break;
                        case "Machine":
                            Task.Factory.StartNew(() =>
                            {
                                result = Hardware.Instance.GetMachine();
                                machine_id = db.GetMachine(result.MachineName, result.URI);
                                if (machine_id == 0)
                                    db.SaveMachine(result.MachineName, result.URI, Utils.GetUTCTimestamp());
                                machine_id = db.GetMachine(result.MachineName, result.URI);
                            }).ContinueWith(task =>
                            {
                                webView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new { Type = "Machine", Data = result, Machine = machine_id }));
                            }, TaskScheduler.FromCurrentSynchronizationContext());
                            break;
                        case "SMBios":
                            Task.Factory.StartNew(() => 
                            {
                                result = Hardware.Instance.GetSMBios();
                                if (!message.Message.ContainsKey("MachineID"))
                                {
                                    //TODO - handle it
                                }
                                machine_id = message.Message["MachineID"].GetInt64();
                                db.SaveBIOS(JsonSerializer.Serialize(result), machine_id, Utils.GetUTCTimestamp());
                            }).ContinueWith(task =>
                            {
                                webView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new { Type = "SMBios", Data = result, Machine = machine_id }));
                            }, TaskScheduler.FromCurrentSynchronizationContext());
                            break;
                        default:
                            break;

                    }
                    break;
                case "POST":
                    break;
                case "CLICK":
                    switch (message.Target)
                    {
                        case "Max":
                            this.WindowState = FormWindowState.Maximized;
                            break;
                        case "Center":
                            this.MinimumSize = new System.Drawing.Size(this._minX, this._minY);
                            this.Size = new System.Drawing.Size(this._minX, this._minY);
                            this.WindowState = FormWindowState.Normal;
                            break;
                        case "Stack":
                            this.MinimumSize = new System.Drawing.Size(this._minX_stack, this._minY);
                            this.Size = new System.Drawing.Size(this._minX_stack, this._minY);
                            this.WindowState = FormWindowState.Normal;
                            break;
                        case "Opacity":
                            if (this.Opacity == this._opacity)
                            {
                                this.Opacity = this._opacity_changed;
                            }
                            else
                            {
                                this.Opacity = this._opacity;
                            }
                            break;
                        case "Pin":
                            if (this.TopMost)
                            {
                                this.TopMost = false;
                            }
                            else
                            {
                                this.TopMost = true;
                            }
                            break;
                        case "Min":
                            this.WindowState = FormWindowState.Minimized;
                            break;
                        case "X":
                            Application.Exit();
                            break;
                        case "Nav":
                            if (message.Message.ContainsKey("Double"))
                            {
                                if (message.Message["Double"].GetBoolean())
                                {
                                    if (this.WindowState == FormWindowState.Normal)
                                    {
                                        this.WindowState = FormWindowState.Maximized;
                                    }
                                    else
                                    {
                                        this.WindowState = FormWindowState.Normal;
                                    }
                                }
                            }
                            break;
                        default:
                            break;
                    }
                    break;
                case "MOUSEDOWN":
                    switch (message.Target)
                    {
                        case "main_navbar":
                            this._dragging = true;
                            this._dragcursor = Cursor.Position;
                            this._dragfrom = this.Location;
                            break;
                        default:
                            break;
                    }
                    break;
                case "MOUSEUP":
                    switch (message.Target)
                    {
                        case "main_navbar":
                            this._dragging = false;
                            break;
                        default:
                            break;
                    }
                    break;
                case "MOUSEMOVE":
                    if (this._dragging && message.Target == "body")
                    {
                        Point dif = Point.Subtract(Cursor.Position, new Size(this._dragcursor));
                        this.Location = Point.Add(this._dragfrom, new Size(dif));
                    }
                    break;
                default:
                    break;
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
        public Dictionary<string, JsonElement> Message { get; set; }
    }

}