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
    public class AstronautDutyController(IMediator mediator) : ControllerBase
    {
        private readonly IMediator _mediator = mediator;

        [HttpGet("{name}")]
        public async Task<IActionResult> GetAstronautDutiesByName(string name)
        {
            try
            {
                var result = await _mediator.Send(new GetAstronautDutiesByName
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

        [HttpPost("")]
        public async Task<IActionResult> CreateAstronautDuty([FromBody] CreateAstronautDuty request)
        {
            var result = await _mediator.Send(request);
            
            return this.GetResponse(result);           
        }

        [HttpPut("")]
        public async Task<IActionResult> UpdateDuty([FromBody] UpdateAstronautDuty command)
        {
            var result = await _mediator.Send(command);

            return this.GetResponse(result);
        }
    }
}