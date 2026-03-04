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

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddDbContext<StargateContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("StargateDb")));
builder.Services.AddMediatR(cfg => {cfg.RegisterServicesFromAssemblies(typeof(Program).Assembly);});
builder.Services.AddScoped<IAstronautDutyDomainService, AstronautDutyDomainService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowStargateUI");

app.UseAuthorization();

app.MapControllers();

app.Run();