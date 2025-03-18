using System.Threading.Tasks;

namespace DataConnector.Interfaces;

public interface IDataConnector
{
    Task SendMessageAsync(object message);
}