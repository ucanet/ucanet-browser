using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Windows.Forms;
using System.IO.Compression;
using System.IO;
using Heijden.DNS;

namespace ucanet_proxy
{
    public class ucanetProxy
    {
        private HttpListener http_listener;
        private string current_dns = "";

        private string get_host(string host_name)
        {
            Resolver dns_resolver = new Resolver();
            dns_resolver.DnsServer = current_dns;
            Response dns_response = dns_resolver.Query(host_name, QType.A, QClass.ANY);
            if (dns_response.Error != "" || dns_response.RecordsA.Length == 0)
                return "";
            return dns_response.RecordsA[0].Address.ToString();
        }

        public void start_proxy(string dns_server)
        {
            current_dns = dns_server;
            http_listener = new HttpListener();
            http_listener.Prefixes.Add("http://127.0.0.1:5443/");
            http_listener.Start();
            receive_request();
        }

        private void stop_listener()
        {
            http_listener.Stop();
        }

        private void receive_request()
        {
            http_listener.BeginGetContext(new AsyncCallback(listener_callback), http_listener);
        }

        private static byte[] read_stream(Stream input_stream)
        {
            byte[] current_buffer = new byte[16 * 1024];
            using (MemoryStream memory_stream = new MemoryStream())
            {
                int current_read;
                while ((current_read = input_stream.Read(current_buffer, 0, current_buffer.Length)) > 0)
                    memory_stream.Write(current_buffer, 0, current_read);
                return memory_stream.ToArray();
            }
        }

        private void listener_callback(IAsyncResult async_result)
        {
            if (http_listener.IsListening)
            {
                receive_request();

                try
                {
                    HttpListenerContext listener_context = http_listener.EndGetContext(async_result);
                    HttpListenerRequest context_request = listener_context.Request;
                    HttpListenerResponse context_response = listener_context.Response;
                    string host_name = get_host(context_request.UserHostName);

                    if (host_name == "")
                    {
                        context_response.StatusCode = (int)HttpStatusCode.BadRequest;
                        context_response.ContentType = "text/plain";
                        context_response.OutputStream.Write(new byte[] {}, 0, 0);
                        context_response.OutputStream.Close();
                        return;
                    }

                    HttpWebRequest web_request = (HttpWebRequest)HttpWebRequest.Create(context_request.Url.AbsoluteUri);
                    web_request.Proxy = new WebProxy("http://" + host_name + ":80");
                    web_request.Method = context_request.HttpMethod;
                    web_request.CookieContainer = new CookieContainer();
                    web_request.AllowAutoRedirect = false;
                    foreach (Cookie current_cookie in context_request.Cookies)
                    {
                        Cookie new_cookie = new Cookie(current_cookie.Name, current_cookie.Value);
                        new_cookie.Domain = host_name;
                        web_request.CookieContainer.Add(new_cookie);
                    }

                    foreach (string current_header in context_request.Headers.AllKeys)
                    {
                        try
                        {
                            if (current_header.ToLower() != "content-length" && current_header.ToLower() != "transfer-encoding")
                                web_request.Headers[current_header] = context_request.Headers[current_header];
                        }
                        catch (Exception) { }
                    }

                    if (context_request.HttpMethod == "POST")
                    {
                        web_request.ContentType = context_request.ContentType;
                        web_request.ContentLength = context_request.ContentLength64;
                        using (Stream request_stream = web_request.GetRequestStream())
                        {
                            byte[] input_data = read_stream(context_request.InputStream);
                            request_stream.Write(input_data, 0, input_data.Length);
                        }
                    }

                    HttpWebResponse web_response = null;
                    try
                    {
                        web_response = (HttpWebResponse)web_request.GetResponse();
                    }
                    catch (WebException web_exception)
                    {
                        if (web_exception.Status == WebExceptionStatus.ProtocolError)
                            web_response = (HttpWebResponse)web_exception.Response;
                        else
                        {
                            context_response.StatusCode = (int)HttpStatusCode.InternalServerError;
                            context_response.ContentType = "text/plain";
                            context_response.OutputStream.Write(new byte[] { }, 0, 0);
                            context_response.OutputStream.Close();
                            return;
                        }
                    }

                    context_response.StatusCode = (int)web_response.StatusCode;
                    context_response.ContentType = web_response.ContentType;
                    context_response.Cookies = web_response.Cookies;
                    context_response.RedirectLocation = web_response.Headers["Location"];
                    
                    foreach (string current_header in context_response.Headers.AllKeys)
                    {
                        try
                        {
                            if (current_header.ToLower() != "content-length" && current_header.ToLower() != "transfer-encoding")
                                context_response.Headers[current_header] = web_response.Headers[current_header];
                        }
                        catch (Exception) { }
                    }

                    Stream response_stream = web_response.GetResponseStream();
                    if (web_response.ContentEncoding.ToLower().Contains("gzip"))
                        response_stream = new GZipStream(response_stream, CompressionMode.Decompress);
                    else if (web_response.ContentEncoding.ToLower().Contains("deflate"))
                        response_stream = new DeflateStream(response_stream, CompressionMode.Decompress);

                    byte[] response_data = read_stream(response_stream);
                    context_response.OutputStream.Write(response_data, 0, response_data.Length);
                    context_response.OutputStream.Close();
               
                }
                catch (Exception) { };
            }
        }
    }
}
