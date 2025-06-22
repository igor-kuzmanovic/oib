using Client.Services;
using System;

namespace Client.Commands
{
    public class ServerStatusCommand : CommandBase
    {
        public override string Name => "Check server status";
        public override string Description => "Displays information about the current server connection";

        public ServerStatusCommand(IFileServiceClient fileServiceClient) : base(fileServiceClient) { }

        public override void Execute()
        {
            string serverAddress = FileServiceClient.GetCurrentServerAddress();
            Console.WriteLine($"\nCurrently connected to: {serverAddress}");
        }
    }
}
