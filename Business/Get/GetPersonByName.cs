using MediatR;
using StargateAPI.Business.Results;

namespace StargateAPI.Business.Get
{
    public class GetPersonByName : IRequest<GetPersonByNameResult>
    {
        public required string Name { get; set; } = string.Empty;
    }
}