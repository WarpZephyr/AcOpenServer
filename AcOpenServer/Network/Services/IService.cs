using System;
using System.Threading.Tasks;

namespace AcOpenServer.Network.Services
{
    public interface IService : IDisposable
    {
        public Task ListenAsync();
    }
}
