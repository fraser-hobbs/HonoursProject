using DataConnector.Interfaces;

namespace DataConnector.Services
{
    public class NullDataConnector : IDataConnector
    {
        public Task SendMessageAsync(object message)
        {
            return Task.CompletedTask;
        }
    }
}