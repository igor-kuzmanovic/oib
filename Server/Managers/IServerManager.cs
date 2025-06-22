namespace Server.Managers
{
    public interface IServerManager
    {
        void StartServer();
        void ShutdownServer();
        string GetServerAddress();
    }
}
