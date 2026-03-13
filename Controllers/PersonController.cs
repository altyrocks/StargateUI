using MediatR;
using System.Net;
using StargateAPI.Business.Get;
using Microsoft.AspNetCore.Mvc;
using StargateAPI.Business.Update;
using StargateAPI.Business.Common;
using StargateAPI.Business.Commands;

namespace StargateAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PersonController(IMediator mediator) : ControllerBase
    {
        private readonly IMediator _mediator = mediator;

        [HttpGet("")]
        public async Task<IActionResult> GetPeople()
        {
            try
            {
                var result = await _mediator.Send(new GetPeople());

                return this.GetResponse(result);
            }
            catch (Exception ex)
            {
                return this.GetResponse(new BaseResponse
                {
                    Message = ex.Message,
                    Success = false,
                    ResponseCode = (int)HttpStatusCode.InternalServerError
                });
            }
        }

        [HttpGet("{name}")]
        public async Task<IActionResult> GetPersonByName(string name)
        {
            try
            {
                var result = await _mediator.Send(new GetPersonByName
                {
                    Name = name
                });

                return this.GetResponse(result);
            }
            catch (Exception ex)
            {
                return this.GetResponse(new BaseResponse
                {
                    Message = ex.Message,
                    Success = false,
                    ResponseCode = (int)HttpStatusCode.InternalServerError
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePerson(int id, [FromBody] UpdatePerson request)
        {
            request.Id = id;

            var result = await _mediator.Send(request);

            return this.GetResponse(result);
        }

        [HttpPut("")]
        public async Task<IActionResult> UpsertPerson([FromBody] UpsertPerson request)
        {
            var result = await _mediator.Send(request);

            return this.GetResponse(result);
        }

        [HttpPost("")]
        public async Task<IActionResult> CreatePerson([FromBody] CreatePerson request)
        {
            try
            {
                var result = await _mediator.Send(request);

                return this.GetResponse(result);
            }
            catch (Exception ex)
            {
                return this.GetResponse(new BaseResponse
                {
                    Message = ex.Message,
                    Success = false,
                    ResponseCode = (int)HttpStatusCode.InternalServerError
                });
            }
        }
    }
}