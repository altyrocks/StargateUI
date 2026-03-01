using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Commands;
using StargateAPI.Business.Data;
using StargateAPI.Business.Services;

public class CreateAstronautDutyHandlerTests
{
    private StargateContext CreateInMemoryContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<StargateContext>()
            .UseSqlite(connection)
            .Options;

        var context = new StargateContext(options);
        context.Database.EnsureCreated();

        // Seed Person Id = 3 with a current duty detail
        var person = new Person { Id = 3, Name = "Steve Doe" };
        context.People.Add(person);

        var detail = new AstronautDetail
        {
            PersonId = 3,
            CurrentDutyTitle = "Commander",
            CurrentRank = "1LT",
            CareerStartDate = new DateTime(2026, 2, 1)
        };

        context.AstronautDetails.Add(detail);
        context.SaveChanges();

        return context;
    }

    private ILogService CreateFakeLogService() => new FakeLogService();

    [Fact]
    public async Task AddNewDuty_CreatesNewCurrentDuty()
    {
        // arrange
        var context = CreateInMemoryContext();
        var log = CreateFakeLogService();
        var handler = new CreateAstronautDutyHandler(context, log);

        var request = new CreateAstronautDuty
        {
            PersonId = 3,
            Rank = "1LT",
            DutyTitle = "Commander 2",
            DutyStartDate = new DateTime(2026, 3, 1)
        };

        // act
        var result = await handler.Handle(request, CancellationToken.None);

        // assert
        Assert.True(result.Success);
        Assert.Equal(201, result.ResponseCode);
        Assert.NotNull(result.Id);

        var newDuty = await context.AstronautDuties.FindAsync(result.Id);
        Assert.NotNull(newDuty);
        Assert.Equal("Commander 2", newDuty!.DutyTitle);
        Assert.Equal(new DateTime(2026, 3, 1), newDuty.DutyStartDate);
        Assert.Null(newDuty.DutyEndDate);
    }

    [Fact]
    public async Task AddNewDuty_SetsPreviousDutyEndDateToDayBeforeNewStart()
    {
        // arrange
        var context = CreateInMemoryContext();
        var log = CreateFakeLogService();

        // Seed an existing duty that starts earlier than the new duty
        var existingDuty = new AstronautDuty
        {
            PersonId = 3,
            Rank = "1LT",
            DutyTitle = "Commander",
            DutyStartDate = new DateTime(2026, 2, 1),
            DutyEndDate = null
        };
        context.AstronautDuties.Add(existingDuty);
        context.SaveChanges();

        var handler = new CreateAstronautDutyHandler(context, log);

        var request = new CreateAstronautDuty
        {
            PersonId = 3,
            Rank = "CPT",
            DutyTitle = "Flight Lead",
            DutyStartDate = new DateTime(2026, 3, 1)
        };

        // act
        var result = await handler.Handle(request, CancellationToken.None);

        // assert
        Assert.True(result.Success);
        Assert.Equal(201, result.ResponseCode);

        var updatedPrevious = await context.AstronautDuties
            .FirstAsync(d => d.Id == existingDuty.Id);
        Assert.Equal(new DateTime(2026, 2, 28), updatedPrevious.DutyEndDate);

        var newDuty = await context.AstronautDuties.FindAsync(result.Id);
        Assert.NotNull(newDuty);
        Assert.Null(newDuty!.DutyEndDate);
    }

    [Fact]
    public async Task AddNewDuty_WithRetiredTitle_SetsCareerEndDate()
    {
        // arrange
        var context = CreateInMemoryContext();
        var log = CreateFakeLogService();
        var handler = new CreateAstronautDutyHandler(context, log);

        var request = new CreateAstronautDuty
        {
            PersonId = 3,
            Rank = "COL",
            DutyTitle = "RETIRED",
            DutyStartDate = new DateTime(2030, 1, 1)
        };

        // act
        var result = await handler.Handle(request, CancellationToken.None);

        // assert
        Assert.True(result.Success);
        Assert.Equal(201, result.ResponseCode);

        var detail = await context.AstronautDetails
            .FirstAsync(d => d.PersonId == 3);
        Assert.Equal(new DateTime(2029, 12, 31), detail.CareerEndDate);

        var retiredDuty = await context.AstronautDuties.FindAsync(result.Id);
        Assert.NotNull(retiredDuty);
        Assert.Equal("RETIRED", retiredDuty!.DutyTitle);
        Assert.Null(retiredDuty.DutyEndDate);
    }

    [Fact]
    public async Task AddNewDuty_WithMissingRequiredFields_ReturnsBadRequest()
    {
        // arrange
        var context = CreateInMemoryContext();
        var log = CreateFakeLogService();
        var handler = new CreateAstronautDutyHandler(context, log);

        var request = new CreateAstronautDuty
        {
            PersonId = 0,           // invalid person id
            Rank = "",              // invalid
            DutyTitle = "  ",       // invalid
            DutyStartDate = new DateTime(2026, 3, 1)
        };

        // act
        var result = await handler.Handle(request, CancellationToken.None);

        // assert
        Assert.False(result.Success);
        Assert.Equal(400, result.ResponseCode);
        Assert.Equal("PersonId, Rank, and DutyTitle are required.", result.Message);
        Assert.Null(result.Id);
    }
}