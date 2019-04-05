using System;
using System.Threading.Tasks;
using Contracts.Commands;
using Contracts.Events;
using NServiceBus;
using NServiceBus.Logging;
using SagaEndpoint.Proxy;
using SagaEndpoint.Timeout;

namespace SagaEndpoint
{
    public class PollingRequestSaga : Saga<PollingRequestSagaData>,
                          IAmStartedByMessages<ProcessPollingRequest>,
                          IHandleTimeouts<PollingRequestTimeout>
    {
        private static ILog log = LogManager.GetLogger<PollingRequestSaga>();
        private readonly IApiProxy _proxy;

        public PollingRequestSaga(IApiProxy proxy)
        {
            _proxy = proxy;
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<PollingRequestSagaData> mapper)
        {
            mapper.ConfigureMapping<ProcessPollingRequest>(message => message.RequestId)
             .ToSaga(sagaData => sagaData.RequestId);
        }

        public async Task Handle(ProcessPollingRequest message, IMessageHandlerContext context)
        {
            log.Info($"Initialize Saga for {message.RequestId}");

            Data.ApiURL = message.ApiURL;
            Data.RequestTime = message.RequestTime;
            Data.TimeoutCount = 0;

            //make request to API and collect claim check token
            var result = await _proxy.MakeInitialRequest(Data.ApiURL, Data.RequestId);
            Data.ClaimCheckToken = result.ClaimCheckToken;

            //todo: make sure this gracefully handles situation where remote endpoint is down
            //in theory nsb retries will handle it automatically, but that will not send a message back to the originating caller
            //depending on the use case, it may be appropriate to send message back to the originator to let them know that
            //the request is running into difficulty and give the originator the option to cancel the request or just wait for 
            //a later response when the process is done.

            await RequestTimeout<PollingRequestTimeout>(context, TimeSpan.FromSeconds(1));

        }

        public async Task Timeout(PollingRequestTimeout state, IMessageHandlerContext context)
        {
            log.Info($"Handle Timeout for {Data.RequestId}, Count {Data.TimeoutCount.ToString()}");

            Data.TimeoutCount++;

            if (Data.ClaimCheckToken == null)
            {
                //todo: this should communicate back to the caller that the saga could not be completed
                //need to reorganize this handler so it can communicate status better
                MarkAsComplete();//we can't continue since we don't have a claim token, something unexpected happend
                return;
            }
                

            //check to see if done
            var result = await _proxy.MakeCallbackRequest(Data.ApiURL, Data.ClaimCheckToken);

            if (Data.TimeoutCount > 3 || result.Completed || Data.ClaimCheckToken == null)
            {
                MarkAsComplete();  //this will delete the saga data from persistance with the assumption it is no longer needed
                log.Info($"Saga Complete for {Data.RequestId}");

                await context.Publish<ICompletedPollingRequest>(
                    messageConstructor: m =>
                    {
                        m.RequestId = Data.RequestId;
                        m.RequestTime = Data.RequestTime;
                    });
            }

            await RequestTimeout<PollingRequestTimeout>(context, TimeSpan.FromSeconds(10));
        }
    }
}
