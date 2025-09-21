using Presentation.Validators;
using Application.Contracts;
using Application.Mapping;
using Application.Services;
using Application.Services.Messaging;
using AutoMapper;
using Serilog;
using Domain.Interfaces;
using FluentValidation;
using Hellang.Middleware.ProblemDetails;
using Infrastructure;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Presentation.Middlewares;
using Presentation.ProfileMapper;
using Presentation.Utilities;
using System.Reflection;
using System.Security.Claims;
using System.Text;


var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Host.UseSerilog((ctx, services, cfg) =>
{
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .ReadFrom.Services(services)
       .Enrich.FromLogContext();
});

builder.Services.AddDistributedMemoryCache();

builder.Services
    .AddControllers(options =>
    {
        options.ReturnHttpNotAcceptable = true;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.SuppressModelStateInvalidFilter = true;
    });

JsonConvert.DefaultSettings = () => new JsonSerializerSettings
{
    NullValueHandling = NullValueHandling.Ignore,
    ContractResolver = new CamelCasePropertyNamesContractResolver(),
    DefaultValueHandling = DefaultValueHandling.Include,
    DateFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.fff'Z'"
};

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Navigation Platform API",
        Version = "v1",
        Description = "API documentation for Navigation Platform project"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.EnableAnnotations();

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });

    // XML comments (if file exists)
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.MigrationsAssembly("Infrastructure")));

builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<UserProfile>();
    cfg.AddProfile<UserProfileApi>();
    cfg.AddProfile<JourneyProfile>();
    cfg.AddProfile<JourneyProfileApi>();
});

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblies(AppDomain.CurrentDomain.GetAssemblies()));

builder.Services.AddProblemDetailsMappingOptions();
builder.Services.AddProblemDetails(options =>
{
    options.IncludeExceptionDetails = (ctx, ex) => false;
});
builder.Services.AddRateLimiter(_ => _
    .AddFixedWindowLimiter("login", o =>
    {
        o.PermitLimit = 5;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueLimit = 0;
    }));

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.SaveToken = true;
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            RoleClaimType = ClaimTypes.Role
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async ctx =>
            {
                var userRepo = ctx.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
                var userIdClaim = ctx.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdClaim, out var userId))
                {
                    ctx.Fail("Invalid token");
                    return;
                }

                var user = await userRepo.GetAsyncById(userId);
                if (user is null)
                {
                    ctx.Fail("User not found");
                    return;
                }

                if (user.Status == "Suspended" || user.Status == "Deactivated")
                {
                    ctx.Fail("Account is not active");
                }
            }
        };
    });

builder.Services.AddAuthorization();

// Validators
builder.Services.AddScoped<IValidator<DTO.WebApiDTO.User.RegisterRequestDtoApi>, RegisterRequestDtoApiValidator>();
builder.Services.AddScoped<IValidator<DTO.WebApiDTO.User.LoginRequestDtoApi>, LoginRequestDtoApiValidator>();
builder.Services.AddScoped<IValidator<DTO.WebApiDTO.User.AdminChangeUserStatusRequestDtoApi>, AdminChangeUserStatusRequestDtoApiValidator>();
builder.Services.AddScoped<IValidator<DTO.WebApiDTO.Journey.JourneyShareRequestDtoApi>, JourneyShareRequestDtoApiValidator>();
builder.Services.AddScoped<IValidator<DTO.WebApiDTO.Journey.JourneyFilterRequestDtoApi>, JourneyFilterRequestDtoApiValidator>();
builder.Services.AddScoped<IValidator<DTO.WebApiDTO.Journey.MonthlyRouteDistanceDtoApi>, MonthlyRouteDistanceDtoApiValidator>();
builder.Services.AddScoped<IValidator<DTO.WebApiDTO.Journey.JourneyUnshareRequestDtoApi>, JourneyUnshareRequestDtoApiValidator>();
builder.Services.AddScoped<IValidator<DTO.WebApiDTO.Journey.AddJourneyRequestDtoApi>, AddJourneyRequestDtoApiValidator>();

// RabbitMQ
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddScoped<IEventPublisher, RabbitMqEventPublisher>();
builder.Services.AddHostedService<DailyGoalWorker>();

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IUserStatusChangeRepository, UserStatusChangeRepository>();
builder.Services.AddScoped<IJourneyShareRepository, JourneyShareRepository>();
builder.Services.AddScoped<IJourneyRepository, JourneyRepository>();
builder.Services.AddScoped<IJourneyPublicLinkRepository, JourneyPublicLinkRepository>();
builder.Services.AddScoped<IDailyGoalBadgeRepository, DailyGoalBadgeRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();

// Domain/Application services
builder.Services.AddScoped<IUserServices, UserServices>();
builder.Services.AddScoped<IJWTUtilities, JWTUtilities>();
builder.Services.AddScoped<IJourneyServices, JourneyServices>();
builder.Services.AddScoped<IJwtBlacklistServices, JwtBlacklistService>();

builder.Services.AddHealthChecks()
    .AddCheck<SqlConnectionHealthCheck>("db")
    .AddCheck<RabbitMqHealthCheck>("rabbit");

var app = builder.Build();

// ------------------ Pipeline ------------------
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Navigation Platform API v1");
        c.RoutePrefix = "swagger";
    });
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseProblemDetails();
app.UseSerilogRequestLogging(opts =>
{
    opts.EnrichDiagnosticContext = (diag, http) =>
    {
        diag.Set("CorrelationId", http.Response.Headers["X-Correlation-ID"].ToString());
        diag.Set("Path", http.Request.Path);
        diag.Set("Method", http.Request.Method);
        diag.Set("StatusCode", http.Response.StatusCode);
    };
});
app.Use(async (context, next) =>
{
    const string header = "X-Correlation-ID";
    if (!context.Request.Headers.TryGetValue(header, out var cid) || string.IsNullOrWhiteSpace(cid))
    {
        cid = Guid.NewGuid().ToString("n");
        context.Request.Headers[header] = cid;
    }
    context.Response.Headers[header] = cid!;
    using (Serilog.Context.LogContext.PushProperty("CorrelationId", cid.ToString()))
    {
        await next();
    }
});

app.MapHealthChecks("/healthz");
app.MapHealthChecks("/readyz");
app.UseRouting();
app.UseMiddleware<JwtBlacklistMiddleware>();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

