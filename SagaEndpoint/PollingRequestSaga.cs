using System;
using System.Threading.Tasks;
using Contracts.Commands;
using Contracts.Events;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NServiceBus;
using SagaEndpoint.Commands;
using SagaEndpoint.Proxy;
using SagaEndpoint.Timeout;

namespace SagaEndpoint
{
    public class PollingRequestSaga : Saga<PollingRequestSagaData>,
                          IAmStartedByMessages<ProcessPollingRequest>,
                          IHandleTimeouts<PollingRequestTimeout>,
                          IHandleMessages<ProcessAPIRequest>
    {
        private readonly ILogger _logger;
        private readonly IApiProxy _proxy;

        public PollingRequestSaga(ILogger<PollingRequestSaga> logger, IApiProxy proxy)
        {
            _proxy = proxy;
            _logger = logger;
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<PollingRequestSagaData> mapper)
        {
            mapper.ConfigureMapping<ProcessPollingRequest>(message => message.RequestId)
             .ToSaga(sagaData => sagaData.RequestId);

            mapper.ConfigureMapping<ProcessAPIRequest>(message => message.RequestId)
             .ToSaga(sagaData => sagaData.RequestId);
        }

        public async Task Handle(ProcessPollingRequest message, IMessageHandlerContext context)
        {
            _logger.LogInformation($"Initialize Saga for {message.RequestId}");

            Data.ApiURL = message.ApiURL;
            Data.RequestTime = message.RequestTime;
            Data.PollsCompleted = 0;
            Data.MaxPollAttempts = message.MaxPollAttempts < 3 ? 3 : message.MaxPollAttempts; //override if less than three
            Data.PollIntervalSeconds = message.PollIntervalSeconds < 2 ? 2 : message.PollIntervalSeconds; //override if less than 2 seconds

            //send message to make initial call instead of handling directly, 
            //this will enable sending message to caller if the external api is down
            await context.SendLocal<ProcessAPIRequest>(m =>{ m.RequestId = Data.RequestId; });

            //suggestion: set overall request timeout? or publish message that the request is being processed?

        }

        public async Task Handle(ProcessAPIRequest message, IMessageHandlerContext context)
        {
            _logger.LogDebug("Make initial request to API for " + Data.RequestId);

            //NOTE: nsb retries will automatically handle short outages of external api, do not catch errors here

            //make request to API and collect claim check token
            var result = await _proxy.MakeInitialRequest(Data.ApiURL, Data.RequestId);
            Data.ClaimCheckToken = result.ClaimCheckToken;

            //schedule request for results to occur in the future
            await RequestTimeout<PollingRequestTimeout>(context, TimeSpan.FromSeconds(Data.PollIntervalSeconds));
        }

        public async Task Timeout(PollingRequestTimeout state, IMessageHandlerContext context)
        {
            _logger.LogInformation($"Handle Polling Request for {Data.RequestId}, Count {Data.PollsCompleted.ToString()}");

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
                _logger.LogInformation($"Saga Complete for {Data.RequestId}");

                //publish results of polling request
                await PublishCompletedMessage(context, result.Completed, 
                    result.Completed ? "Successful": "Maximum number of callbacks attempted",
                    result.Completed ? JsonConvert.SerializeObject(result) : null);
            }
            else
            {
                Data.PollsCompleted++;

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
