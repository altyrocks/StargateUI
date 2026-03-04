using StargateAPI.Business.Data;
using Microsoft.EntityFrameworkCore;

public class AstronautDutyDomainService : IAstronautDutyDomainService
{
    private readonly StargateContext _context;

    public AstronautDutyDomainService(StargateContext context)
    {
        _context = context;
    }

    public async Task<AstronautDuty> CreateDutyAsync(
        int personId,
        string rank,
        string title,
        DateTime startDate,
        CancellationToken cancellationToken)
    {
        var dutyStart = startDate.Date;

        var person = await _context.People
            .FirstOrDefaultAsync(p => p.Id == personId, cancellationToken)
            ?? throw new InvalidOperationException("Person not found.");

        var lastDuty = await _context.AstronautDuties
            .Where(d => d.PersonId == personId)
            .OrderByDescending(d => d.DutyStartDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (lastDuty != null && dutyStart <= lastDuty.DutyStartDate)
            throw new InvalidOperationException("New duty must start after last duty.");

        if (lastDuty != null)
        {
            lastDuty.DutyEndDate = dutyStart.AddDays(-1);
        }

        var detail = await _context.AstronautDetails
            .FirstOrDefaultAsync(d => d.PersonId == personId, cancellationToken);

        if (detail == null)
        {
            detail = new AstronautDetail
            {
                PersonId = personId,
                CareerStartDate = dutyStart
            };

            _context.AstronautDetails.Add(detail);
        }

        detail.CurrentRank = rank.Trim();
        detail.CurrentDutyTitle = title.Trim();

        if (title.Equals("RETIRED", StringComparison.OrdinalIgnoreCase))
        {
            detail.CareerEndDate = dutyStart.AddDays(-1);
        }

        var newDuty = new AstronautDuty
        {
            PersonId = personId,
            Rank = rank.Trim(),
            DutyTitle = title.Trim(),
            DutyStartDate = dutyStart
        };

        _context.AstronautDuties.Add(newDuty);

        return newDuty;
    }

    public async Task UpdateDutyAsync(
        int Id,
        string rank,
        string dutyTitle,
        DateTime dutyStartDate,
        CancellationToken cancellationToken)
    {
        var allIds = await _context.AstronautDuties
            .Select(d => d.Id)
            .ToListAsync(cancellationToken);

        var duty = await _context.AstronautDuties
            .FirstOrDefaultAsync(d => d.Id == Id, cancellationToken);

        if (duty == null)
            throw new InvalidOperationException("Duty not found.");

        if (string.IsNullOrWhiteSpace(rank) || string.IsNullOrWhiteSpace(dutyTitle))
            throw new InvalidOperationException("Rank and Duty Title are required.");

        duty.Rank = rank.Trim();
        duty.DutyTitle = dutyTitle.Trim();
        duty.DutyStartDate = dutyStartDate.Date;

        await _context.SaveChangesAsync(cancellationToken);
    }
}