using MediatR;
using StargateAPI.Business.Commands;
using StargateAPI.Business.Data;
using StargateAPI.Business.Results;
using StargateAPI.Business.Services;

namespace StargateAPI.Business.Handlers
{
    public class CreateAstronautDutyHandler(StargateContext context, ILogService logService, IAstronautDutyDomainService domainService) : IRequestHandler<CreateAstronautDuty, CreateAstronautDutyResult>
    {
        private readonly ILogService _logService = logService;
        private readonly StargateContext _context = context;
        private readonly IAstronautDutyDomainService _domainService = domainService;

        public async Task<CreateAstronautDutyResult> Handle(
            CreateAstronautDuty request,
            CancellationToken cancellationToken)
        {
            var result = new CreateAstronautDutyResult();

            try
            {
                if (request.PersonId <= 0 ||
                    string.IsNullOrWhiteSpace(request.Rank) ||
                    string.IsNullOrWhiteSpace(request.DutyTitle))
                {
                    result.Success = false;
                    result.Message = "PersonId, Rank, and DutyTitle are required.";
                    result.ResponseCode = 400;

                    return result;
                }

                var duty = await _domainService.CreateDutyAsync(
                    request.PersonId,
                    request.Rank,
                    request.DutyTitle,
                    request.DutyStartDate,
                    cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);

                await _logService.InfoAsync(
                    source: nameof(CreateAstronautDutyHandler),
                    message: $"Created duty '{request.DutyTitle}' for PersonId {request.PersonId}",
                    cancellationToken: cancellationToken);

                result.Success = true;
                result.ResponseCode = 201;
                result.Id = duty.Id;
                result.Message = "Duty created successfully.";
            }
            catch (InvalidOperationException ex)
            {
                result.Success = false;
                result.ResponseCode = 400;
                result.Message = ex.Message;
            }
            catch (Exception ex)
            {
                await _logService.ErrorAsync(
                    source: nameof(CreateAstronautDutyHandler),
                    message: "Unexpected error creating astronaut duty.",
                    details: ex.ToString(),
                    cancellationToken: cancellationToken);

                result.Success = false;
                result.ResponseCode = 500;
                result.Message = "An unexpected error occurred.";
            }

            return result;
        }
    }
}