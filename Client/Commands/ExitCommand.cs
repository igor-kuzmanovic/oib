using Client.Services;
using System;

namespace Client.Commands
{
    public class ExitCommand : CommandBase
    {
        public override string Name => "Exit";
        public override string Description => "Exits the application";

        public ExitCommand(IFileServiceClient fileServiceClient) : base(fileServiceClient) { }

        public override void Execute()
        {

        }
    }
}
