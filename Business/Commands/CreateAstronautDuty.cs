using Dapper;
using MediatR;
using MediatR.Pipeline;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;
using StargateAPI.Business.Services;
using StargateAPI.Controllers;
using System.Net;

namespace StargateAPI.Business.Commands
{
    public class CreateAstronautDuty : IRequest<CreateAstronautDutyResult>
    {
        public int PersonId { get; set; }

        public required string Rank { get; set; }

        public required string DutyTitle { get; set; }

        public DateTime DutyStartDate { get; set; }
    }

    public class CreateAstronautDutyPreProcessor : IRequestPreProcessor<CreateAstronautDuty>
    {
        private readonly StargateContext _context;

        public CreateAstronautDutyPreProcessor(StargateContext context)
        {
            _context = context;
        }

        public Task Process(CreateAstronautDuty request, CancellationToken cancellationToken)
        {
            var person = _context.People
                .AsNoTracking()
                .FirstOrDefault(z => z.Id == request.PersonId);

            if (person is null)
                throw new BadHttpRequestException($"Person with id '{request.PersonId}' not found.");

            var verifyNoPreviousDuty = _context.AstronautDuties
                .FirstOrDefault(z =>
                    z.PersonId == request.PersonId &&
                    z.DutyTitle == request.DutyTitle &&
                    z.DutyStartDate == request.DutyStartDate);

            if (verifyNoPreviousDuty is not null)
                throw new BadHttpRequestException(
                    $"Duty '{request.DutyTitle}' already exists for person {request.PersonId} with start date {request.DutyStartDate:yyyy-MM-dd}.");

            return Task.CompletedTask;
        }
    }

    public class CreateAstronautDutyHandler
    : IRequestHandler<CreateAstronautDuty, CreateAstronautDutyResult>
    {
        private readonly StargateContext _context;
        private readonly ILogService _logService;

        public CreateAstronautDutyHandler(StargateContext context, ILogService logService)
        {
            _context = context;
            _logService = logService;
        }

        public async Task<CreateAstronautDutyResult> Handle(
            CreateAstronautDuty request,
            CancellationToken cancellationToken)
        {
            var result = new CreateAstronautDutyResult();

            try
            {
                // basic validation
                if (request.PersonId <= 0 ||
                    string.IsNullOrWhiteSpace(request.Rank) ||
                    string.IsNullOrWhiteSpace(request.DutyTitle))
                {
                    result.Success = false;
                    result.Message = "PersonId, Rank, and DutyTitle are required.";
                    result.ResponseCode = (int)HttpStatusCode.BadRequest;
                    return result;
                }

                var dutyStart = request.DutyStartDate.Date;

                // make sure the person exists
                var person = await _context.People
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == request.PersonId, cancellationToken);

                if (person is null)
                {
                    result.Success = false;
                    result.Message = $"Person with id '{request.PersonId}' not found.";
                    result.ResponseCode = (int)HttpStatusCode.BadRequest;
                    return result;
                }

                // prevent duplicate duty for same person / title / start date
                var verifyNoPreviousDuty = await _context.AstronautDuties
                    .FirstOrDefaultAsync(d =>
                        d.PersonId == request.PersonId &&
                        d.DutyTitle == request.DutyTitle &&
                        d.DutyStartDate == dutyStart,
                        cancellationToken);

                if (verifyNoPreviousDuty is not null)
                {
                    result.Success = false;
                    result.Message =
                        $"Duty '{request.DutyTitle}' already exists for person {request.PersonId} with start date {dutyStart:yyyy-MM-dd}.";
                    result.ResponseCode = (int)HttpStatusCode.BadRequest;
                    return result;
                }

                // load or create AstronautDetail for this person
                var astronautDetail = await _context.AstronautDetails
                    .FirstOrDefaultAsync(d => d.PersonId == person.Id, cancellationToken);

                if (astronautDetail == null)
                {
                    astronautDetail = new AstronautDetail
                    {
                        PersonId = person.Id,
                        CurrentDutyTitle = request.DutyTitle.Trim(),
                        CurrentRank = request.Rank.Trim(),
                        CareerStartDate = dutyStart
                    };
                    _context.AstronautDetails.Add(astronautDetail);
                }
                else
                {
                    astronautDetail.CurrentDutyTitle = request.DutyTitle.Trim();
                    astronautDetail.CurrentRank = request.Rank.Trim();
                    _context.AstronautDetails.Update(astronautDetail);
                }

                // if RETIRED, set career end date to day before retired duty starts
                if (request.DutyTitle.Equals("RETIRED", StringComparison.OrdinalIgnoreCase))
                {
                    astronautDetail.CareerEndDate = dutyStart.AddDays(-1);
                }

                // get last duty (most recent) for this person
                var lastDuty = await _context.AstronautDuties
                    .Where(d => d.PersonId == person.Id)
                    .OrderByDescending(d => d.DutyStartDate)
                    .FirstOrDefaultAsync(cancellationToken);

                if (lastDuty != null)
                {
                    // new start must be after last start
                    if (dutyStart <= lastDuty.DutyStartDate.Date)
                    {
                        result.Success = false;
                        result.Message = "New duty start date must be after last duty start date.";
                        result.ResponseCode = (int)HttpStatusCode.BadRequest;
                        return result;
                    }

                    // guard against MinValue edge case
                    var endSource = dutyStart == DateTime.MinValue
                        ? lastDuty.DutyStartDate.Date
                        : dutyStart;

                    lastDuty.DutyEndDate = endSource.AddDays(-1);
                    _context.AstronautDuties.Update(lastDuty);
                }

                // create new current duty (EndDate = null)
                var duty = new AstronautDuty
                {
                    PersonId = request.PersonId,
                    Rank = request.Rank.Trim(),
                    DutyTitle = request.DutyTitle.Trim(),
                    DutyStartDate = dutyStart,
                    DutyEndDate = null
                };

                _context.AstronautDuties.Add(duty);
                await _context.SaveChangesAsync(cancellationToken);

                result.Success = true;
                result.ResponseCode = (int)HttpStatusCode.Created;
                result.Id = duty.Id;
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                result.ResponseCode = (int)HttpStatusCode.InternalServerError;
                return result;
            }
        }
    }

    public class CreateAstronautDutyResult : BaseResponse
    {
        public int? Id { get; set; }
    }
}