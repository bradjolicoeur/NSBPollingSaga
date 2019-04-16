using Contracts.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts.Events
{
    public interface ICompletedPollingRequest : IContainRequestInfo
    {
        //was the request completed successfully
        bool Successful { get; set; }
        string Message { get; set; }
        string Response { get; set; } //this is a good candidate for databus
        int NumberOfAttempts { get; set; }
    }
}
