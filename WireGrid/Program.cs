using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WireGrid
{
    class Program
    {
        static void Main(string[] args)
        {
            new Program().Initialize();
        }

        private void Initialize()
        {
            Settings.Initialize();
            new WEBserver().Initialize();
            new FTPserver().Initialize();
            ConsoleThread();
        }

        private void ConsoleThread()
        {
            while (true)
            {

            }
        }
    }
}
