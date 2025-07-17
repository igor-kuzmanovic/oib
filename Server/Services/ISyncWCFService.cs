using Contracts.Models;
using System.ServiceModel;

namespace Server.Services
{
    [ServiceContract]
    public interface ISyncWCFService
    {
        [OperationContract]
        StorageEvent[] GetEventsSinceId(int lastId);
    }
}
