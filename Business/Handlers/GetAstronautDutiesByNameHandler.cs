using Dapper;
using MediatR;
using System.Net;
using StargateAPI.Business.Get;
using StargateAPI.Business.Data;
using StargateAPI.Business.Dtos;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Services;

namespace StargateAPI.Business.Handlers
{
    public class GetAstronautDutiesByNameHandler(StargateContext context, ILogService logService) : IRequestHandler<GetAstronautDutiesByName, Response<AstronautDutyDto>>
    {
        private readonly StargateContext _context = context;
        private readonly ILogService _logService = logService;

        public async Task<Response<AstronautDutyDto>> Handle(
            GetAstronautDutiesByName request,
            CancellationToken cancellationToken)
        {
            var result = new Response<AstronautDutyDto>();

            try
            {
                 if (string.IsNullOrWhiteSpace(request.Name))
                {
                    result.Success = false;
                    result.Message = "Name is required.";
                    result.ResponseCode = (int)HttpStatusCode.BadRequest;
                    result.Data = null;
                    return result;
                }

                var normalizedName = request.Name.Trim();

                var person = await _context.People
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        p => p.Name.ToLower() == normalizedName.ToLower(),
                        cancellationToken);

                if (person is null)
                {
                    result.Success = false;
                    result.Message = $"No person found with name '{normalizedName}'.";
                    result.ResponseCode = (int)HttpStatusCode.NotFound;
                    result.Data = null;

                    await _logService.InfoAsync(
                        nameof(GetAstronautDutiesByNameHandler),
                        $"No person found with name '{normalizedName}'.",
                        null,
                        cancellationToken);

                    return result;
                }

                const string dutySql = @"
                                        SELECT *
                                        FROM [AstronautDuty]
                                        WHERE PersonId = @PersonId
                                        ORDER BY DutyStartDate DESC;";

                var connection = _context.Database.GetDbConnection();

                if (connection.State != System.Data.ConnectionState.Open)
                    await connection.OpenAsync(cancellationToken);

                var latestDuty = await connection.QueryFirstOrDefaultAsync<AstronautDuty>(
                    dutySql,
                    new { PersonId = person.Id });

                if (latestDuty is null)
                {
                    result.Success = true;
                    result.Message = $"No duty assigned for '{normalizedName}'.";
                    result.ResponseCode = (int)HttpStatusCode.OK;
                    result.Data = null;

                    await _logService.InfoAsync(
                        nameof(GetAstronautDutiesByNameHandler),
                        $"No duty assigned for '{normalizedName}'.",
                        null,
                        cancellationToken);

                    return result;
                }

                result.Data = new AstronautDutyDto
                {
                    Id =latestDuty.Id,
                    Name = person.Name,
                    Assignment = latestDuty.DutyTitle ?? string.Empty,
                    Rank = latestDuty.Rank ?? string.Empty,
                    LastUpdated = latestDuty.DutyStartDate
                };

                result.Success = true;
                result.Message = "Astronaut duties retrieved successfully.";
                result.ResponseCode = (int)HttpStatusCode.OK;

                await _logService.InfoAsync(
                    nameof(GetAstronautDutiesByNameHandler),
                    $"Retrieved latest duty for '{person.Name}'.",
                    null,
                    cancellationToken);

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = "An error occurred while retrieving astronaut duties.";
                result.ResponseCode = (int)HttpStatusCode.InternalServerError;
                result.Data = null;

                await _logService.ErrorAsync(
                    nameof(GetAstronautDutiesByNameHandler),
                    "Error in GetAstronautDutiesByName.",
                    ex.ToString(),
                    cancellationToken);

                return result;
            }    
        }
    }
}