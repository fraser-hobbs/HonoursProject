using DataConnector.Models;
using System.Threading.Tasks;

namespace DataConnector.Interfaces;

public interface IDataConnector
{
    Task SendMessageAsync<T>(KafkaMessage<T> message);
}