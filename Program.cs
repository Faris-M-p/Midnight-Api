using Microsoft.EntityFrameworkCore;
using MidnightApi.Data;
using MidnightApi.Interfaces;
using MidnightApi.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection is missing in appsettings.");

builder.Services.AddDbContext<DbConnectionClass>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IFamilyMemberRepository, FamilyMemberRepository>();
builder.Services.AddScoped<IMarriageUnionRepository, MarriageUnionRepository>();
builder.Services.AddScoped<IMilestoneRepository, MilestoneRepository>();
builder.Services.AddScoped<IFamilyTreeRepository, FamilyTreeRepository>();

var app = builder.Build();

await DatabaseInitializer.EnsureCreatedAsync(app.Services, connectionString);

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
