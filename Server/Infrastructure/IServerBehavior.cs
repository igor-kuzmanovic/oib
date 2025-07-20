namespace Server.Infrastructure
{
    public interface IServerBehavior
    {
        string GetName();
        void Start();
        void Stop();
    }
}
