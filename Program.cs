using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using MidnightApi.Auth;
using MidnightApi.DataAccess;
using MidnightApi.Filters;
using MidnightApi.Interfaces;
using MidnightApi.Middleware;
using MidnightApi.Options;
using MidnightApi.Repositories;
using MidnightApi.Services;
using MidnightApi.Swagger;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ApiResponseResultFilter>();
});
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});
builder.Services.Configure<DatabaseTraceOptions>(builder.Configuration.GetSection(DatabaseTraceOptions.SectionName));
builder.Services.AddSwaggerDocumentation();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<DatabaseTraceWriter>();
builder.Services.AddScoped<IDataAccessDapper, DataAccessDapper>();
builder.Services.AddScoped<IFamiliesRepository, FamiliesRepository>();
builder.Services.AddScoped<IMembersRepository, MembersRepository>();
builder.Services.AddScoped<IUserAccountsRepository, UserAccountsRepository>();
builder.Services.AddSingleton<CommonService>();
builder.Services.AddSingleton<PasswordService>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddSingleton<IImageFileService, ImageFileService>();
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 6 * 1024 * 1024;
});

var app = builder.Build();

// Force construction so a fresh DatabaseTrace.log is created when Enabled=true.
_ = app.Services.GetRequiredService<DatabaseTraceWriter>();

app.UseGlobalExceptionHandling();
app.UseSwaggerDocumentation();
app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
