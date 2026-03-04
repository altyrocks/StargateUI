using Dapper;
using MediatR;
using StargateAPI.Business.Common;
using StargateAPI.Business.Data;
using StargateAPI.Business.Dtos;
using StargateAPI.Business.Services;
using System.Net;

namespace StargateAPI.Business.Queries
{
    public class GetPeople : IRequest<GetPeopleResult>
    {

    }

    public class GetPeopleHandler : IRequestHandler<GetPeople, GetPeopleResult>
    {
        private readonly StargateContext _context;
        private readonly ILogService _logService;

        public GetPeopleHandler(StargateContext context, ILogService logService)
        {
            _context = context;
            _logService = logService;
        }

        public async Task<GetPeopleResult> Handle(
            GetPeople request,
            CancellationToken cancellationToken)
        {
            var result = new GetPeopleResult();

            try
            {
                const string sql = "SELECT Id, Name FROM [Person];";

                var people = await _context.Connection.QueryAsync<Person>(
                    new CommandDefinition(
                        sql,
                        cancellationToken: cancellationToken));

                result.Data = people.ToList();
                result.Success = true;
                result.Message = "People retrieved successfully.";
                result.ResponseCode = (int)HttpStatusCode.OK;

                await _logService.InfoAsync(
                    nameof(GetPeopleHandler),
                    $"Retrieved {result.Data.Count} people.",
                    null,
                    cancellationToken);

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = "An error occurred while retrieving people.";
                result.ResponseCode = (int)HttpStatusCode.InternalServerError;

                await _logService.ErrorAsync(
                    nameof(GetPeopleHandler),
                    "Error in GetPeople.",
                    ex.ToString(),
                    cancellationToken);

                return result;
            }
        }
    }

    public class GetPeopleResult : BaseResponse
    {
        public List<Person>? Data { get; set; }
    }
}