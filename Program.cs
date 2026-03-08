using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;
using StargateAPI.Business.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowStargateUI", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<ILogService, LogService>();

builder.Services.AddDbContext<StargateContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("StargateDb"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
        })
        .EnableDetailedErrors()
);

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(typeof(Program).Assembly);
});

builder.Services.AddScoped<IAstronautDutyDomainService, AstronautDutyDomainService>();

// shows logs in Log Stream in Azure Portal
builder.Logging.AddConsole();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StargateContext>();
    db.Database.Migrate();
}

app.UseHttpsRedirection();

app.UseCors("AllowStargateUI");

app.UseAuthorization();

app.MapControllers();

app.Run();