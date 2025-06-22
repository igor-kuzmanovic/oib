using System.ServiceModel;

namespace Server.Infrastructure
{
    public interface IServerHost
    {
        void Start();
        void Stop();
        bool IsRunning { get; }
        string Address { get; }
    }
}
