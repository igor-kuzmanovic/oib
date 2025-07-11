using System.ServiceModel;

namespace Server.Services
{
    [ServiceContract]
    public interface ISyncWCFService
    {
        [OperationContract]
        bool Ping();
    }
}
