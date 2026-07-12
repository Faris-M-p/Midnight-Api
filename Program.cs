using Microsoft.EntityFrameworkCore;
using MidnightApi.Data;
using MidnightApi.Interfaces;
using MidnightApi.Middleware;
using MidnightApi.Repositories;
using MidnightApi.Services;

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

builder.Services.AddScoped<IFamiliesRepository, FamiliesRepository>();
builder.Services.AddScoped<IMembersRepository, MembersRepository>();
builder.Services.AddScoped<IMemberAddressesRepository, MemberAddressesRepository>();
builder.Services.AddScoped<IMemberImagesRepository, MemberImagesRepository>();
builder.Services.AddScoped<IMemberEventsRepository, MemberEventsRepository>();
builder.Services.AddScoped<IMemberSocialLinksRepository, MemberSocialLinksRepository>();
builder.Services.AddScoped<IMemberNotesRepository, MemberNotesRepository>();
builder.Services.AddScoped<IUserAccountsRepository, UserAccountsRepository>();
builder.Services.AddSingleton<MemberValidationService>();

var app = builder.Build();

await DatabaseInitializer.EnsureCreatedAsync(app.Services, connectionString);

app.UseGlobalExceptionHandling();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
