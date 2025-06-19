using Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.ServiceModel;
using System.Text;

namespace ClientApp
{
    public class WCFClient : ChannelFactory<IWCFService>, IWCFService, IDisposable
    {
        IWCFService factory;

        public WCFClient(NetTcpBinding binding, EndpointAddress address) : base(binding, address)
        {
            factory = CreateChannel();
        }

        public void Dispose()
        {
            if (factory != null)
            {
                factory = null;
            }

            Close();
        }

        public string[] ShowFolderContent(string path)
        {
            try
            {
                var result = factory.ShowFolderContent(path);
                Console.WriteLine("'ShowFolderContent' allowed");
                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while trying to 'ShowFolderContent': {0}", e.Message);
                return null;
            }
        }

        public FileData ReadFile(string path)
        {
            try
            {
                var result = factory.ReadFile(path);
                Console.WriteLine("'ReadFile' allowed");
                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while trying to 'ReadFile': {0}", e.Message);
                return null;
            }
        }

        public bool CreateFolder(string path)
        {
            try
            {
                var result = factory.CreateFolder(path);
                Console.WriteLine("'CreateFolder' allowed");
                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while trying to 'CreateFolder': {0}", e.Message);
                return false;
            }
        }

        public bool CreateFile(string path, FileData fileData)
        {
            try
            {
                var result = factory.CreateFile(path, fileData);
                Console.WriteLine("'CreateFile' allowed");
                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while trying to 'CreateFile': {0}", e.Message);
                return false;
            }
        }

        public bool Delete(string path)
        {
            try
            {
                var result = factory.Delete(path);
                Console.WriteLine("'Delete' allowed");
                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while trying to 'Delete': {0}", e.Message);
                return false;
            }
        }

        public bool Rename(string sourcePath, string destinationPath)
        {
            try
            {
                var result = factory.Rename(sourcePath, destinationPath);
                Console.WriteLine("'Rename' allowed");
                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while trying to 'Rename': {0}", e.Message);
                return false;
            }
        }

        public bool MoveTo(string sourcePath, string destinationPath)
        {
            try
            {
                var result = factory.MoveTo(sourcePath, destinationPath);
                Console.WriteLine("'MoveTo' allowed");
                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while trying to 'MoveTo': {0}", e.Message);
                return false;
            }
        }
    }
}
