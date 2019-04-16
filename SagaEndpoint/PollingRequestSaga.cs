using System;
using System.Threading.Tasks;
using Contracts.Commands;
using Contracts.Events;
using Newtonsoft.Json;
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
            Data.PollsCompleted = 0;
            Data.MaxPollAttempts = message.MaxPollAttempts < 3 ? 3 : message.MaxPollAttempts; //override if less than three
            Data.PollIntervalSeconds = message.PollIntervalSeconds < 2 ? 2 : message.PollIntervalSeconds; //override if less than 2 seconds

            //make request to API and collect claim check token
            var result = await _proxy.MakeInitialRequest(Data.ApiURL, Data.RequestId);
            Data.ClaimCheckToken = result.ClaimCheckToken;

            //todo: make sure this gracefully handles situation where remote endpoint is down
            //in theory nsb retries will handle it automatically, but that will not send a message back to the originating caller
            //depending on the use case, it may be appropriate to send message back to the originator to let them know that
            //the request is running into difficulty and give the originator the option to cancel the request or just wait for 
            //a later response when the process is done.

            //schedule request for results to occur in the future
            await RequestTimeout<PollingRequestTimeout>(context, TimeSpan.FromSeconds(Data.PollIntervalSeconds));

        }

        public async Task Timeout(PollingRequestTimeout state, IMessageHandlerContext context)
        {
            log.Info($"Handle Polling Request for {Data.RequestId}, Count {Data.PollsCompleted.ToString()}");

            Data.PollsCompleted++;

            if (Data.ClaimCheckToken == null)
            {
                //publish failed results of polling request
                await PublishCompletedMessage(context, false,
                    "Claim Check Token from target was null and request could not complete");

                MarkAsComplete(); //we can't continue since we don't have a claim token, something unexpected happend
                return;
            }
                

            //make callback to see if response is ready
            var result = await _proxy.MakeCallbackRequest(Data.ApiURL, Data.ClaimCheckToken);

            if (Data.PollsCompleted >= Data.MaxPollAttempts || result.Completed)
            {
                MarkAsComplete();  //this will delete the saga data from persistance with the assumption it is no longer needed
                log.Info($"Saga Complete for {Data.RequestId}");

                //publish results of polling request
                await PublishCompletedMessage(context, result.Completed, 
                    result.Completed ? "Successful": "Maximum number of callbacks attempted",
                    result.Completed ? JsonConvert.SerializeObject(result) : null);
            }
            else
            {
                //schedule timeout to make another attempt in the future
                await RequestTimeout<PollingRequestTimeout>(context, TimeSpan.FromSeconds(Data.PollIntervalSeconds));
            }

        }

        private async Task PublishCompletedMessage(IMessageHandlerContext context, bool successful, string message, string response = null)
        {
            await context.Publish<ICompletedPollingRequest>(
                messageConstructor: m =>
                {
                    m.RequestId = Data.RequestId;
                    m.RequestTime = Data.RequestTime;
                    m.Successful = successful;
                    m.Message = message;
                    m.Response = response;
                    m.NumberOfAttempts = Data.PollsCompleted;
                });
        }
    }
}
