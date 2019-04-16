using Contracts.Interfaces;
using NServiceBus;
using System;

namespace SagaEndpoint
{
    public class PollingRequestSagaData : IContainSagaData, IContainRequestInfo
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }

        public string RequestId { get; set; }
        public DateTime RequestTime { get; set; }

        public string ApiURL { get; set; }
        public string ClaimCheckToken { get; set; }

        public int PollsCompleted { get; set; }

        public int PollIntervalSeconds { get; set; }
        public int MaxPollAttempts { get; set; }
    }
}
