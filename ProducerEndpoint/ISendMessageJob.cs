using NServiceBus;

namespace ProducerEndpoint
{
    public interface ISendMessageJob
    {
        IEndpointInstance Endpoint { get; set; }

        void Execute();
    }
}