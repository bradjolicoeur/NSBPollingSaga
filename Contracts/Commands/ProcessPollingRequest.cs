using System;
using Contracts.Interfaces;


namespace Contracts.Commands
{
    public class ProcessPollingRequest : IContainRequestInfo
    {
        public string ApiURL { get; set; }
        public string RequestId { get; set; }
        public DateTime RequestTime { get; set; }
    }
}
