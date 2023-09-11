using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Management;
using System.IO;
using Microsoft.Win32;
using System.Net.NetworkInformation;
using IEProxy;
using ucanet_proxy;

namespace ucanet_browser
{
  
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void load_website(string website_url)
        {
            try
            {
                ucanetBrowserControl.Url = new Uri(website_url);
                urlBox.Text = website_url;
            }
            catch (Exception)
            {
                try
                {
                    ucanetBrowserControl.Url = new Uri("http://" + website_url);
                    urlBox.Text = "http://" + website_url;
                }
                catch (Exception)
                {
                    MessageBox.Show("Invalid URL " + website_url, "URL Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        bool needs_reset = false;
        private void set_dns(bool set_back, string dns_server)
        {
            needs_reset = true;

            ManagementClass management_class = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection management_collection = management_class.GetInstances();

            foreach (ManagementObject management_object in management_collection)
            {
                if ((bool)management_object["IPEnabled"])
                {
                    ManagementBaseObject new_dns = management_object.GetMethodParameters("SetDNSServerSearchOrder");
                    new_dns["DNSServerSearchOrder"] = set_back ? null : new string[] { dns_server };
                    management_object.InvokeMethod("SetDNSServerSearchOrder", new_dns, null);
                }
            }

            try
            {
                if (set_back)
                {
                    Registry.SetValue("HKEY_LOCAL_MACHINE\\System\\CurrentControlSet\\Services\\VxD\\MSTCP", "EnableDNS", "0");
                    Registry.SetValue("HKEY_LOCAL_MACHINE\\System\\CurrentControlSet\\Services\\VxD\\MSTCP", "HostName", "");
                    Registry.SetValue("HKEY_LOCAL_MACHINE\\System\\CurrentControlSet\\Services\\VxD\\MSTCP", "NameServer", "");
                    Registry.SetValue("HKEY_LOCAL_MACHINE\\System\\CurrentControlSet\\Services\\VxD\\MSTCP", "SearchList", "");
                }
                else
                {
                    Registry.SetValue("HKEY_LOCAL_MACHINE\\System\\CurrentControlSet\\Services\\VxD\\MSTCP", "EnableDNS", "1");
                    Registry.SetValue("HKEY_LOCAL_MACHINE\\System\\CurrentControlSet\\Services\\VxD\\MSTCP", "HostName", "ucanet-user");
                    Registry.SetValue("HKEY_LOCAL_MACHINE\\System\\CurrentControlSet\\Services\\VxD\\MSTCP", "SearchList", "");
                    Registry.SetValue("HKEY_LOCAL_MACHINE\\System\\CurrentControlSet\\Services\\VxD\\MSTCP", "NameServer", dns_server);
                }
            }
            catch (Exception) { }
        }

        private bool try_ping(string host_name)
        {
            try
            {
                Ping ping_utility = new Ping();
                PingReply ping_reply = ping_utility.Send(host_name);
                return ping_reply.Status == IPStatus.Success;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private string simple_get(string url_text)
        {
            WebRequest web_request = WebRequest.Create(url_text);
            web_request.Method = "GET";
            WebResponse web_response = web_request.GetResponse();
            Stream response_stream = web_response.GetResponseStream();
            StreamReader stream_reader = new StreamReader(response_stream);
            return stream_reader.ReadToEnd();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (!try_ping("ucanet.mil"))
            {
                string dns_server = simple_get("http://settings.ucanet.net/config/dns");

                if (!HttpListener.IsSupported)
                {
                    if (MessageBox.Show("To use ucanet browser on this version of Windows, the DNS server must be applied system-wide. Would you like to continue? (The DNS server will be reset when you close ucanet browser)", "Important!", MessageBoxButtons.OKCancel, MessageBoxIcon.Information)
                        == DialogResult.OK)
                    {
                        set_dns(false, dns_server);
                    }
                    else
                        this.Close();
                }
                else
                {
                    ucanet_proxy.ucanetProxy current_proxy = new ucanetProxy();
                    current_proxy.start_proxy(dns_server);
                    IEProxy.WinInetInterop.SetConnectionProxy("http://localhost:5443");
                }
            }
            load_website("http://ucanet.net");
        }

        private void show_help()
        {
            MessageBox.Show(
@"ucanet browser is a free browser for exploring the web
within the ucanet ecosystem.

ucanet browser version 1.1
ucanet.net
Copyright © 2023", "ucanet browser - info");
        }

        private void label2_Click(object sender, EventArgs e)
        {
            show_help();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            show_help();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            load_website("http://ucanet.net/");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (button4.Text == "Refresh")
                ucanetBrowserControl.Refresh();
            else
            {
                progressBar1.Visible = false;
                loadingLabel.Text = "Done";

                ucanetBrowserControl.Stop();
                button4.Text = "Refresh";
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            load_website(urlBox.Text);
        }

        private void urlBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                load_website(urlBox.Text);
                e.Handled = e.SuppressKeyPress = true;
            }
        }

        private void ucanetBrowserControl_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            loadingLabel.Text = "Opening page " + e.Url.ToString();
            progressBar1.Visible = true;
    
            button4.Text = "Stop";
            urlBox.Text = ucanetBrowserControl.Url.AbsoluteUri;
        }

        private void ucanetBrowserControl_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            button4.Text = "Refresh";
            button2.Enabled = ucanetBrowserControl.CanGoBack;
            button3.Enabled = ucanetBrowserControl.CanGoForward;
            urlBox.Text = ucanetBrowserControl.Url.AbsoluteUri;

            string title_text = ucanetBrowserControl.DocumentTitle;
            this.Text = (title_text == "" ? ucanetBrowserControl.Url.AbsoluteUri : title_text) + " - ucanet browser";
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (ucanetBrowserControl.CanGoBack)
                ucanetBrowserControl.Document.Window.History.Go(-1);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (ucanetBrowserControl.CanGoForward)
                ucanetBrowserControl.Document.Window.History.Go(1);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (needs_reset)
                set_dns(true, "");
            if (HttpListener.IsSupported)
                IEProxy.WinInetInterop.RestoreSystemProxy();
        }

        private void ucanetBrowserControl_ProgressChanged(object sender, WebBrowserProgressChangedEventArgs e)
        {
            progressBar1.Maximum = (int)e.MaximumProgress;
            progressBar1.Value = (int)e.CurrentProgress;
        }

        private void ucanetBrowserControl_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            progressBar1.Visible = false;
            loadingLabel.Text = "Done";
        }
    }
}
