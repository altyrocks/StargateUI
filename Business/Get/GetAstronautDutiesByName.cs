using MediatR;
using StargateAPI.Business.Dtos;

namespace StargateAPI.Business.Get
{
    public class GetAstronautDutiesByName : IRequest<Response<AstronautDutyDto>>
    {
        public string Name { get; set; } = string.Empty;
    }
}