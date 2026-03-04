using Dapper;
using MediatR;
using StargateAPI.Business.Common;
using StargateAPI.Business.Data;
using StargateAPI.Business.Dtos;
using StargateAPI.Business.Services;
using System.Net;

namespace StargateAPI.Business.Queries
{
    public class GetPersonByName : IRequest<GetPersonByNameResult>
    {
        public required string Name { get; set; } = string.Empty;
    }

    public class GetPersonByNameHandler : IRequestHandler<GetPersonByName, GetPersonByNameResult>
    {
        private readonly StargateContext _context;
        private readonly ILogService _logService;
        public GetPersonByNameHandler(StargateContext context, ILogService logService)
        {
            _context = context;
            _logService = logService;
        }

        public async Task<GetPersonByNameResult> Handle(GetPersonByName request, CancellationToken cancellationToken)
        {
            var result = new GetPersonByNameResult();

            // validate name
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                result.Success = false;
                result.Message = "Name is required.";
                result.ResponseCode = (int)HttpStatusCode.BadRequest;

                return result;
            }

            var name = request.Name.Trim().ToLower();

            const string sql = @"SELECT a.Id AS PersonId, a.Name, b.CurrentRank, 
                                 b.CurrentDutyTitle, b.CareerStartDate, b.CareerEndDate
                                 FROM [Person] a
                                 LEFT JOIN [AstronautDetail] b ON b.PersonId = a.Id
                                 WHERE LOWER(a.Name) = @Name;";

            try
            {
                var people = await _context.Connection.QueryAsync<PersonAstronautDto>(
                    new CommandDefinition(
                        sql,
                        new { Name = name },
                        cancellationToken: cancellationToken));

                var person = people.FirstOrDefault();

                result.Person = person;

                if (person == null)
                {
                    result.Success = false;
                    result.Message = $"No person found with name '{name}'.";
                    result.ResponseCode = (int)HttpStatusCode.NotFound;

                    await _logService.InfoAsync(
                        nameof(GetPersonByNameHandler),
                        $"No person found with name '{name}'.",
                        null,
                        cancellationToken);
                }
                else
                {
                    await _logService.InfoAsync(
                        nameof(GetPersonByNameHandler),
                        $"Person '{name}' retrieved successfully.",
                        null,
                        cancellationToken);
                }

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = "An error occurred while retrieving the person.";
                result.ResponseCode = (int)HttpStatusCode.InternalServerError;

                await _logService.ErrorAsync(
                    nameof(GetPersonByNameHandler),
                    "Error in GetPersonByName.",
                    ex.ToString(),
                    cancellationToken);

                return result;
            }
        }
    }

    public class GetPersonByNameResult : BaseResponse
    {
        public PersonAstronautDto? Person { get; set; }
    }
}