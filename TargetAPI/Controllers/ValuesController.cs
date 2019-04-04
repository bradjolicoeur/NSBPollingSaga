using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TargetAPI.Models;
using TargetAPI.Repository;

namespace TargetAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private readonly IRequestRepository _repository;

        public ValuesController(IRequestRepository repository)
        {
            _repository = repository;
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public ActionResult<RequestState> Get(string id)
        {
            return _repository.CheckRequest(id);
        }

        // POST api/values
        [HttpPost]
        public ActionResult<RequestState> Post([FromBody] ValueRequest value)
        {
            //NOTE: for production ready code there should be validation of the data in the post before processing 
            return _repository.Insert(value);
        }

    }
}
