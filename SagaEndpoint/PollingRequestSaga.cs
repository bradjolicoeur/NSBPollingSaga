using System;
using System.Threading.Tasks;
using Contracts.Commands;
using Contracts.Events;
using NServiceBus;
using NServiceBus.Logging;
using SagaEndpoint.Timeout;

namespace SagaEndpoint
{
    public class PollingRequestSaga : Saga<PollingRequestSagaData>,
                          IAmStartedByMessages<ProcessPollingRequest>,
                          IHandleTimeouts<PollingRequestTimeout>
    {
        static ILog log = LogManager.GetLogger<PollingRequestSaga>();

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<PollingRequestSagaData> mapper)
        {
            mapper.ConfigureMapping<ProcessPollingRequest>(message => message.RequestId)
             .ToSaga(sagaData => sagaData.RequestId);
        }

        public Task Handle(ProcessPollingRequest message, IMessageHandlerContext context)
        {
            log.Info($"Initialize Saga for {message.RequestId}");

            Data.ApiURL = message.ApiURL;
            Data.RequestTime = message.RequestTime;
            Data.TimeoutCount = 0;

            //TODO: make request to API and collect claim check token

            return RequestTimeout<PollingRequestTimeout>(context, TimeSpan.FromSeconds(1));
        }

        public Task Timeout(PollingRequestTimeout state, IMessageHandlerContext context)
        {
            log.Info($"Handle Timeout for {Data.RequestId}, Count {Data.TimeoutCount.ToString()}");

            Data.TimeoutCount++;

            //TODO: put request in here

            if (Data.TimeoutCount > 3)
            {
                MarkAsComplete();  //this will delete the saga data from persistance with the assumption it is no longer needed
                log.Info($"Saga Complete for {Data.RequestId}");

                return context.Publish<ICompletedPollingRequest>(
                    messageConstructor: m =>
                    {
                        m.RequestId = Data.RequestId;
                        m.RequestTime = Data.RequestTime;
                    });
            }

            return RequestTimeout<PollingRequestTimeout>(context, TimeSpan.FromSeconds(10));
        }
    }
}
