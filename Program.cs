using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using MidnightApi.Auth;
using MidnightApi.DataAccess;
using MidnightApi.DataAccess.Interfaces;
using MidnightApi.Filters;
using MidnightApi.Middleware;
using MidnightApi.Options;
using MidnightApi.Repositories;
using MidnightApi.Repositories.Interfaces;
using MidnightApi.Services;
using MidnightApi.Services.Interfaces;
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
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.SectionName));
builder.Services.Configure<OtpOptions>(builder.Configuration.GetSection(OtpOptions.SectionName));
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
builder.Services.AddScoped<IAccountOtpsRepository, AccountOtpsRepository>();
builder.Services.AddScoped<IAccessTokensRepository, AccessTokensRepository>();
builder.Services.AddScoped<IAccessAuthorizationService, AccessAuthorizationService>();
builder.Services.AddScoped<IAccountOtpService, AccountOtpService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddSingleton<ICommonService, CommonService>();
builder.Services.AddSingleton<IPasswordService, PasswordService>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<IFamilyCodeGenerator, FamilyCodeGenerator>();
builder.Services.AddSingleton<IAccessTokenSecretGenerator, AccessTokenSecretGenerator>();
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
