using System;
using System.Collections.Generic;
using System.Text;

namespace SagaEndpoint.Models
{
    public class ApiResponse
    {
        public bool Completed { get; set; }
        public string Message { get; set; }
        public bool Success { get; set; }
        public string ClaimCheckToken { get; set; }
    }
}
