using Client.Services;
using Contracts.Models;
using System;
using System.Text;

namespace Client.Commands
{
    public abstract class CommandBase : ICommand
    {
        protected readonly IFileServiceClient FileServiceClient;

        public abstract string Name { get; }
        public abstract string Description { get; }

        protected CommandBase(IFileServiceClient fileServiceClient)
        {
            FileServiceClient = fileServiceClient ?? throw new ArgumentNullException(nameof(fileServiceClient));
        }

        public abstract void Execute();

        protected string GetUserInput(string prompt)
        {
            Console.Write(prompt);
            return Console.ReadLine();
        }

        protected void DisplayResult(string operation, bool success)
        {
            Console.WriteLine($"\n{operation} {(success ? "successful" : "failed")}.");
        }
    }
}
