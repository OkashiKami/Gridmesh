using System;
using System.IO;
using System.Net;

namespace WireGrid
{
    internal class FTPserver
    {
        public string Name
        {
            get
            {
                var name = Settings.Get<string>("FTPSERVER", "Name");
                if (string.IsNullOrEmpty(name)) name = "Default Network";
                return name;
            }
            private set { Settings.Set("FTPSERVER", "Name", value); }
        }
        public int Port
        {
            get { return Settings.Get<int>("FTPSERVER", "Port"); }
            private set { Settings.Set("FTPSERVER", "Port", value); }
        }
        public IPAddress Address
        {
            get
            {
                var address = Settings.Get<IPAddress>("FTPSERVER", "IPAddress");
                if (address.ToString() == IPAddress.Any.ToString())
                    address = IPAddress.Parse("127.0.0.1");
                return address;
            }
            private set { Settings.Set("FTPSERVER", "IPAddress", value); }
        }
        internal void Initialize()
        {
            if (!Settings.Got("FTPSERVER", "Name")) Settings.Set("FTPSERVER", "Name", "FTP");
            if (!Settings.Got("FTPSERVER", "IPAddress")) Settings.Set("FTPSERVER", "IPAddress", IPAddress.Loopback);
            if (!Settings.Got("FTPSERVER", "OpenInBrowser")) Settings.Set("FTPSERVER", "OpenInBrowser", false);
            if (!Settings.Got("FTPSERVER", "Port")) Settings.Set("FTPSERVER", "Port", 21);
            if (!Settings.Got("FTPSERVER", "Root"))
            {
                Settings.Set("FTPSERVER", "Root", Settings.path + "root\\ftp\\");
                Directory.CreateDirectory(Settings.path + "root\\ftp\\");
            }

            if (Settings.Get<bool>("FTPSERVER", "OpenInBrowser")) Console.WriteLine(string.Format("{0} Server Started, URL: http://{1}:{2}/", Name, Address, Port));
        }
    }
}