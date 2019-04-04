using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts.Interfaces
{
    public interface IContainRequestInfo
    {
          string RequestId { get; set; }
          DateTime RequestTime { get; set; }
    }
}
