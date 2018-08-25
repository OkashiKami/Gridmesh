using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace WireGrid
{
    class WEBserver
    {
        private readonly string[] _indexFiles =
        {
            "index.html",
            "index.htm",
            "index.php",
            "default.html",
            "default.htm",
            "default.php",
        };
        private static IDictionary<string, string> _mimeTypeMappings = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
        {
            #region extension to MIME type list
        {".asf", "video/x-ms-asf"},
        {".asx", "video/x-ms-asf"},
        {".avi", "video/x-msvideo"},
        {".bin", "application/octet-stream"},
        {".cco", "application/x-cocoa"},
        {".crt", "application/x-x509-ca-cert"},
        {".css", "text/css"},
        {".deb", "application/octet-stream"},
        {".der", "application/x-x509-ca-cert"},
        {".dll", "application/octet-stream"},
        {".dmg", "application/octet-stream"},
        {".ear", "application/java-archive"},
        {".eot", "application/octet-stream"},
        {".exe", "application/octet-stream"},
        {".flv", "video/x-flv"},
        {".gif", "image/gif"},
        {".hqx", "application/mac-binhex40"},
        {".htc", "text/x-component"},
        {".htm", "text/html"},
        {".html", "text/html"},
        {".ico", "image/x-icon"},
        {".img", "application/octet-stream"},
        {".iso", "application/octet-stream"},
        {".jar", "application/java-archive"},
        {".jardiff", "application/x-java-archive-diff"},
        {".jng", "image/x-jng"},
        {".jnlp", "application/x-java-jnlp-file"},
        {".jpeg", "image/jpeg"},
        {".jpg", "image/jpeg"},
        {".js", "application/x-javascript"},
        {".mml", "text/mathml"},
        {".mng", "video/x-mng"},
        {".mov", "video/quicktime"},
        {".mp3", "audio/mpeg"},
        {".mpeg", "video/mpeg"},
        {".mpg", "video/mpeg"},
        {".msi", "application/octet-stream"},
        {".msm", "application/octet-stream"},
        {".msp", "application/octet-stream"},
        {".pdb", "application/x-pilot"},
        {".pdf", "application/pdf"},
        {".php", "text/html"},
        {".pem", "application/x-x509-ca-cert"},
        {".pl", "application/x-perl"},
        {".pm", "application/x-perl"},
        {".png", "image/png"},
        {".prc", "application/x-pilot"},
        {".ra", "audio/x-realaudio"},
        {".rar", "application/x-rar-compressed"},
        {".rpm", "application/x-redhat-package-manager"},
        {".rss", "text/xml"},
        {".run", "application/x-makeself"},
        {".sea", "application/x-sea"},
        {".shtml", "text/html"},
        {".sit", "application/x-stuffit"},
        {".swf", "application/x-shockwave-flash"},
        {".tcl", "application/x-tcl"},
        {".tk", "application/x-tcl"},
        {".txt", "text/plain"},
        {".war", "application/java-archive"},
        {".wbmp", "image/vnd.wap.wbmp"},
        {".wmv", "video/x-ms-wmv"},
        {".xml", "text/xml"},
        {".xpi", "application/x-xpinstall"},
        {".zip", "application/zip"},
        #endregion
        };
        private Thread _serverThread;
        private HttpListener _listener;
        public string Name
        {
            get
            {
                var name = Settings.Get<string>("WEBSERVER", "Name");
                if (string.IsNullOrEmpty(name)) name = "Default Network";
                return name;
            }
            private set { Settings.Set("WEBSERVER", "Name", value); }
        }
        public int Port
        {
            get { return Settings.Get<int>("WEBSERVER", "Port"); }
            private set { Settings.Set("WEBSERVER", "Port", value); }
        }
        public IPAddress Address
        {
            get
            {
                var address = Settings.Get<IPAddress>("WEBSERVER", "IPAddress");
                if (address.ToString() == IPAddress.Any.ToString())
                    address = IPAddress.Parse("127.0.0.1");
                return address;
            }
            private set { Settings.Set("WEBSERVER", "IPAddress", value); }
        }

        public void Initialize()
        {
            if (!Settings.Got("WEBSERVER", "Name")) Settings.Set("WEBSERVER", "Name", "WEB");
            if (!Settings.Got("WEBSERVER", "IPAddress")) Settings.Set("WEBSERVER", "IPAddress", IPAddress.Loopback);
            if(!Settings.Got("WEBSERVER", "OpenInBrowser")) Settings.Set("WEBSERVER", "OpenInBrowser", false);
            if(!Settings.Got("WEBSERVER", "Port")) Settings.Set("WEBSERVER", "Port", 88);
            if (!Settings.Got("WEBSERVER", "Root"))
            {
                Settings.Set("WEBSERVER", "Root", Settings.path + "root\\web\\");
                Directory.CreateDirectory(Settings.path + "root\\web\\");
            }
            foreach(string d in Directory.GetDirectories(Settings.Get<string>("WEBSERVER", "Root")))
            {
                var fs = File.Create(d + "\\.htaccess");
                StreamWriter sr = new StreamWriter(fs);
                sr.WriteLine("Options +Indexes");
                sr.Close();
                fs.Close();
            }

            //get an empty port
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();

            _serverThread = new Thread(this.Listen);
            _serverThread.Start();

            if(Settings.Get<bool>("WEBSERVER", "OpenInBrowser")) Console.WriteLine(string.Format("{0} Server Started,  URL: http://{1}:{2}/", Name, Address, Port));

        }

        /// <summary>
        /// Stop server and dispose all functions.
        /// </summary>
        public void Stop()
        {
            _serverThread.Abort();
            _listener.Stop();
        }
        private void Listen()
        {
            _listener = new HttpListener();
            PrefixAllow();
            _listener.Prefixes.Add(string.Format("http://{0}:{1}/", Address, Port));

            _listener.Start();
            while (true)
            {
                try
                {
                    HttpListenerContext context = _listener.GetContext();
                    Process(context);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("\n" + ex + "\n");
                }
            }
        }
        private void PrefixAllow()
        {
            try
            {
                Process p = new Process();
                p.StartInfo = new ProcessStartInfo()
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    Arguments = string.Format("/C netsh http add urlacl url=http://{0}:{1}/ user=Everyone", Address, Port)
                };
                p.OutputDataReceived += (a, b) => { Console.WriteLine(a + " : " + b.Data); };
                p.Start();
                
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n" + ex + "\n");
            }
        }
        private void Process(HttpListenerContext context)
        {
            string filename = context.Request.Url.AbsolutePath;
            var wdir = Settings.Get<string>("WEBSERVER", "Root");
            Console.WriteLine(filename);
            filename = filename.Substring(1);

            if(!filename.Contains("."))
            {
                wdir = wdir + filename;
                filename = string.Empty;  
            }
            if (string.IsNullOrEmpty(filename))
            {
                foreach (string indexFile in _indexFiles)
                {
                    if (File.Exists(Path.Combine(wdir, indexFile)))
                    {
                        filename = indexFile;
                        break;
                    }
                }
            }


            filename = Path.Combine(wdir, filename);

            if (File.Exists(filename))
            {
                try
                {
                    Stream input = new FileStream(filename, FileMode.Open);

                    //Adding permanent http response headers
                    string mime;
                    context.Response.ContentType = _mimeTypeMappings.TryGetValue(Path.GetExtension(filename), out mime) ? mime : "application/x-directory";
                    context.Response.ContentLength64 = input.Length;
                    context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
                    context.Response.AddHeader("Last-Modified", File.GetLastWriteTime(filename).ToString("r"));

                    byte[] buffer = new byte[1024 * 16];
                    int nbytes;
                    while ((nbytes = input.Read(buffer, 0, buffer.Length)) > 0)
                        context.Response.OutputStream.Write(buffer, 0, nbytes);
                    input.Close();

                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    context.Response.OutputStream.Flush();
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    Console.WriteLine("\n" + ex + "\n");
                }

            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }

            context.Response.OutputStream.Close();
        }

    }
}