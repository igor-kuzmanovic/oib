using ClientApp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            NetTcpBinding binding = new NetTcpBinding();
            string address = "net.tcp://localhost:9999/WCFService";

            using (WCFClient proxy = new WCFClient(binding, new EndpointAddress(new Uri(address))))
            {
                // TODO
            }

            Console.ReadLine();
        }
    }
}
