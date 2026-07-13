using Microsoft.EntityFrameworkCore;
using MidnightApi.Data;
using MidnightApi.Filters;
using MidnightApi.Interfaces;
using MidnightApi.Middleware;
using MidnightApi.Auth;
using MidnightApi.Repositories;
using MidnightApi.Services;
using MidnightApi.Swagger;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ApiResponseResultFilter>();
});
builder.Services.AddSwaggerDocumentation();
builder.Services.AddJwtAuthentication(builder.Configuration);
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
builder.Services.AddSingleton<PasswordService>();
builder.Services.AddSingleton<JwtTokenService>();

var app = builder.Build();

await DatabaseInitializer.EnsureCreatedAsync(app.Services, connectionString);

app.UseGlobalExceptionHandling();
app.UseSwaggerDocumentation();
app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
