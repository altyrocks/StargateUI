using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;
using StargateAPI.Business.Dtos;
using StargateAPI.Business.Queries;
using StargateAPI.Business.Services;
using StargateAPI.Controllers;

public class GetAstronautDutiesByNameHandlerTests
{
    private StargateContext CreateInMemoryContext()
    {
        // Unique in-memory SQLite database per test run
        var connection = new SqliteConnection($"DataSource=file:{Guid.NewGuid()};Mode=Memory;Cache=Shared");
        connection.Open();

        var options = new DbContextOptionsBuilder<StargateContext>()
            .UseSqlite(connection)
            .Options;

        var context = new StargateContext(options);
        context.Database.EnsureCreated();

        // Seed Person + AstronautDetail
        var person = new Person { Name = "John Doe" }; // do NOT set Id
        context.People.Add(person);
        context.SaveChanges(); // person.Id now assigned

        var detail = new AstronautDetail
        {
            PersonId = person.Id,
            CurrentDutyTitle = "Commander",
            CurrentRank = "1LT",
            CareerStartDate = new DateTime(2026, 2, 1)
        };
        context.AstronautDetails.Add(detail);
        context.SaveChanges();

        // Use Dapper via the same connection to insert duties
        const string dutySql = @"INSERT INTO AstronautDuty
(PersonId, Rank, DutyTitle, DutyStartDate, DutyEndDate)
VALUES (@PersonId, @Rank, @DutyTitle, @DutyStartDate, @DutyEndDate);";

        // Older duty
        context.Connection.Execute(dutySql, new
        {
            PersonId = person.Id,
            Rank = "2LT",
            DutyTitle = "Pilot",
            DutyStartDate = new DateTime(2026, 2, 10),
            DutyEndDate = new DateTime(2026, 2, 20)
        });

        // Latest duty
        context.Connection.Execute(dutySql, new
        {
            PersonId = person.Id,
            Rank = "1LT",
            DutyTitle = "Commander",
            DutyStartDate = new DateTime(2026, 3, 1),
            DutyEndDate = (DateTime?)null
        });

        return context;
    }

    private ILogService CreateFakeLogService() => new FakeLogService();

    [Fact]
    public async Task GetAstronautDutiesByName_ReturnsLatestDutyDto()
    {
        // arrange
        var context = CreateInMemoryContext();
        var log = CreateFakeLogService();
        var handler = new GetAstronautDutiesByNameHandler(context, log);

        var request = new GetAstronautDutiesByName
        {
            Name = "john doe" // should still match, handler normalizes
        };

        // act
        var result = await handler.Handle(request, CancellationToken.None);

        // assert
        Assert.True(result.Success);
        Assert.Equal(200, result.ResponseCode);
        Assert.NotNull(result.Data);

        var dto = result.Data!;
        Assert.Equal("John Doe", dto.Name);
        Assert.Equal("Commander", dto.Assignment);
        Assert.Equal("1LT", dto.Rank);
        Assert.Equal(new DateTime(2026, 3, 1), dto.LastUpdated.Date);
    }

    [Fact]
    public async Task GetAstronautDutiesByName_WhenPersonNotFound_ReturnsNotFound()
    {
        // arrange
        var context = CreateInMemoryContext();
        var log = CreateFakeLogService();
        var handler = new GetAstronautDutiesByNameHandler(context, log);

        var request = new GetAstronautDutiesByName
        {
            Name = "Nonexistent Person"
        };

        // act
        var result = await handler.Handle(request, CancellationToken.None);

        // assert
        Assert.False(result.Success);
        Assert.Equal(404, result.ResponseCode);
        Assert.Null(result.Data);
        Assert.Contains("No person found", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetAstronautDutiesByName_WithEmptyName_ReturnsBadRequest()
    {
        // arrange
        var context = CreateInMemoryContext();
        var log = CreateFakeLogService();
        var handler = new GetAstronautDutiesByNameHandler(context, log);

        var request = new GetAstronautDutiesByName
        {
            Name = "   "
        };

        // act
        var result = await handler.Handle(request, CancellationToken.None);

        // assert
        Assert.False(result.Success);
        Assert.Equal(400, result.ResponseCode);
        Assert.Null(result.Data);
        Assert.Equal("Name is required.", result.Message);
    }
}