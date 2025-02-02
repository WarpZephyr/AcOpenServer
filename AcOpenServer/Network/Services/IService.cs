using System;
using System.Threading.Tasks;

namespace AcOpenServer.Network.Services
{
    public interface IService : IDisposable
    {
        public bool Start();
        public bool End();
        public Task UpdateAsync();
    }
}
