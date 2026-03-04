using StargateAPI.Business.Data;

public interface IAstronautDutyDomainService
{
    Task<AstronautDuty> CreateDutyAsync(
        int personId,
        string rank,
        string title,
        DateTime startDate,
        CancellationToken cancellationToken);

        Task UpdateDutyAsync(
        int Id,
        string rank,
        string dutyTitle,
        DateTime dutyStartDate,
        CancellationToken cancellationToken);
    }