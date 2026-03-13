using StargateAPI.Business.Dtos;
using StargateAPI.Business.Common;

namespace StargateAPI.Business.Results
{
    public class GetPersonByNameResult : BaseResponse
    {
        public PersonAstronautDto? Person { get; set; }
    }
}