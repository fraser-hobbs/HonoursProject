using DataConnector.Interfaces;

namespace SolarDataController.Services
{
    public class NullDataConnector : IDataConnector
    {
        public Task SendMessageAsync(object message)
        {
            // No-op in development mode, does nothing
            return Task.CompletedTask;
        }
    }
}