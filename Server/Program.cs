using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Description;
using Contracts;
using System.IdentityModel.Policy;
using SecurityManager;
using System.IO;
using System.Threading;

namespace Server
{
    public class Program
    {
        static void Main(string[] args)
        {
            ServerManager serverManager = new ServerManager();
            serverManager.StartServer();
        }
    }
}
