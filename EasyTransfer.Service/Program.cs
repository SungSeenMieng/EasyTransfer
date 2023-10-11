using EasyTransfer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTransfer.Service
{
    internal class Program
    {
        static void Main(string[] args)
        {
            int port = 0;
            if (args.Length > 0 && int.TryParse(args[0], out port))
            {

            }
            else
            {
                Console.WriteLine("Input Port:");
                string input = Console.ReadLine();
                int.TryParse(input, out port);
            }
            ETService service = new ETService(port);
            service.OnConnectRequesting += Service_OnConnectRequesting;
            service.Start();
            while(service.Port == 0)
            {
                Thread.Sleep(100);
            }
            Console.WriteLine($"Service Running at Port {service.Port}");
        }

        private static bool Service_OnConnectRequesting(string code)
        {
            Console.WriteLine($"Client Connecting With Code: {code}");
            Console.WriteLine("Accept this Connect? \r\nenter n to reject");
            string enter = Console.ReadLine();
            if (enter.Trim().ToLower() == "n")
            {
                return false;
            }
            return true;
        }
    }
}
