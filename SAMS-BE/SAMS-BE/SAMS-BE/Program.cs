using Microsoft.EntityFrameworkCore;
using SAMS_BE.Models;
using SAMS_BE.Repositories;
using SAMS_BE.Services;
using SAMS_BE.Mappers;
using SAMS_BE.Interfaces.IRepository;
using SAMS_BE.Interfaces.IService;
using SAMS_BE.Interfaces;
using SAMS_BE.Helpers;
using System.Text.Json.Serialization;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SAMS_BE.Config.Backchannel;
using SAMS_BE.Config.Auth;
using SAMS_BE.Config.Downstream;
using SAMS_BE.Tenant;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.OpenApi.Models;
using System.Text.Json;
using System.Security.Claims;
using Hangfire;
using Hangfire.SqlServer;
using SAMS_BE.Config;
using SAMS_BE.Infrastructure.Persistence.Global;
using SAMS_BE.Interfaces.IRepository.GlobalAdmin;
using SAMS_BE.Repositories.GlobalAdmin;
using SAMS_BE.Interfaces.IService.GlobalAdmin;
using SAMS_BE.Services.GlobalAdmin;
using SAMS_BE.Interfaces.IService.Keycloak;
using SAMS_BE.Services.Keycloak;
using SAMS_BE.Config.SendGrid;
using SAMS_BE.Services.Mail;
using SAMS_BE.Interfaces.IMail;
using Microsoft.AspNetCore.Mvc;
using SAMS_BE.Interfaces.IRepository.Building;
using SAMS_BE.Repositories.Building;
using SAMS_BE.Interfaces.IService.IBuilding;
using SAMS_BE.Services.Building;
using SAMS_BE.MappingProfiles;
using SAMS_BE.Interfaces.IRepository.Resident;
using SAMS_BE.Services.Resident;
using SAMS_BE.Interfaces.IService.IResident;
using SAMS_BE.Repositories.Resident;
using Microsoft.AspNetCore.Authorization;
using SAMS_BE.Security;
using SAMS_BE.Utils.HanldeException;



var builder = WebApplication.CreateBuilder(args);
// Đăng ký DbContext (ưu tiên lấy từ cấu hình)

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

// Add services
builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    // Add converters for DateOnly and TimeOnly
    o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    o.JsonSerializerOptions.Converters.Add(new SAMS_BE.Helpers.JsonDateOnlyConverter());
    o.JsonSerializerOptions.Converters.Add(new SAMS_BE.Helpers.JsonNullableDateOnlyConverter());
    o.JsonSerializerOptions.Converters.Add(new SAMS_BE.Helpers.JsonTimeOnlyConverter());
    o.JsonSerializerOptions.Converters.Add(new SAMS_BE.Helpers.JsonNullableTimeOnlyConverter());
});

// Cho phép controller tự xử lý ModelState để trả message tùy biến
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

builder.Services.Configure<SendGridOptions>(
    builder.Configuration.GetSection("SendGrid"));

builder.Services.AddScoped<IEmailSender, SendGridEmailSender>();

builder.Services.AddScoped<ITenantContextAccessor, TenantContextAccessor>();

builder.Services.AddDbContext<GlobalDirectoryContext>(opt =>
    opt.UseSqlServer((builder.Configuration.GetConnectionString("DefaultConnection"))));

builder.Services.AddDbContext<BuildingManagementContext>((sp, options) =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.ReplaceService<IModelCacheKeyFactory, TenantModelCacheKeyFactory>();
});


builder.Services.AddScoped<IModelCacheKeyFactory, TenantModelCacheKeyFactory>();

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.Clear();

builder.Services
  .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(o =>
  {
      o.MapInboundClaims = false;

      var kc = builder.Configuration.GetSection("Keycloak");

      o.Authority = kc["Authority"];
      o.Audience = kc["Audience"];
      o.RequireHttpsMetadata = kc.GetValue<bool>("RequireHttpsMetadata");

      o.TokenValidationParameters = new TokenValidationParameters
      {
          ValidateIssuer = true,
          ValidateAudience = true,
          ValidAudiences = new[] { kc["Audience"], "account" },

          ValidateLifetime = true,
          ClockSkew = TimeSpan.Zero,

          NameClaimType = "preferred_username",
          RoleClaimType = ClaimTypes.Role,
      };

      o.Events = new JwtBearerEvents
      {
          OnTokenValidated = ctx =>
          {
              var identity = (ClaimsIdentity)ctx.Principal.Identity!;

              var access = ctx.Principal.FindFirst("resource_access")?.Value;
              if (!string.IsNullOrEmpty(access))
              {
                  using var doc = JsonDocument.Parse(access);
                  if (doc.RootElement.TryGetProperty("backend", out var backend) &&
                      backend.TryGetProperty("roles", out var rolesEl) &&
                      rolesEl.ValueKind == JsonValueKind.Array)
                  {
                      foreach (var r in rolesEl.EnumerateArray())
                      {
                          var role = r.GetString();
                          if (!string.IsNullOrWhiteSpace(role))
                              identity.AddClaim(new Claim(ClaimTypes.Role, role!));
                      }
                  }
              }

              var buildingId = ctx.Principal.FindFirst("building_id")?.Value;
              if (!string.IsNullOrEmpty(buildingId))
              {
                  identity.AddClaim(new Claim("building_id", buildingId));
              }

              return Task.CompletedTask;
          }
      };
  });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", p => p.RequireRole("admin"));
    options.AddPolicy("RequireManagerOrAdmin", p => p.RequireRole("manager", "admin"));
});

builder.Services.AddSingleton<IAuthorizationHandler, SAMS_BE.Security.PermissionAuthorizationHandler>();

builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();

builder.Services.AddSingleton<IKeycloakTokenService, KeycloakTokenService>();
builder.Services.AddTransient<BearerTokenHandler>();
builder.Services.AddHttpClient<ICoreServiceClient, CoreServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Downstream:CoreServiceBaseUrl"]!);
})
.AddHttpMessageHandler<BearerTokenHandler>();

builder.Services.AddEndpointsApiExplorer();

// Register AutoMapper
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<FloorProfile>();
    cfg.AddProfile<ApartmentProfile>();
    cfg.AddProfile<AnnouncementProfile>();
    cfg.AddProfile<GlobalMappingProfile>();
    cfg.AddProfile<StaffMappingProfile>();
    cfg.AddProfile(typeof(BuildingProfile));
});

// Register repositories
builder.Services.AddScoped<IFloorRepository, FloorRepository>();
builder.Services.AddScoped<IApartmentRepository, ApartmentRepository>();
builder.Services.AddScoped<IAnnouncementRepository, AnnouncementRepository>();
builder.Services.AddScoped<IAmenityRepository, AmenityRepository>();
builder.Services.AddScoped<IAmenityPackageRepository, AmenityPackageRepository>();
builder.Services.AddScoped<IAmenityBookingRepository, AmenityBookingRepository>();
builder.Services.AddScoped<IAmenityCheckInRepository, AmenityCheckInRepository>();
builder.Services.AddScoped<IAccessCardRepository, AccessCardRepository>();
builder.Services.AddScoped<ICardHistoryRepository, CardHistoryRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<SAMS_BE.Interfaces.IRepository.IVoucherRepository, VoucherRepository>();
builder.Services.AddScoped<IAssetRepository, AssetRepository>();
builder.Services.AddScoped<IAdminUserRepository, AdminUserRepository>();
builder.Services.AddScoped<IBuildingRepository, BuildingRepository>();
builder.Services.AddScoped<IStaffRepository, StaffRepository>();
builder.Services.AddScoped<IWorkRoleRepository, WorkRoleRepository>();
builder.Services.AddScoped<IResidentTicketRepository, ResidentTicketRepository>();
builder.Services.AddScoped<IScriptRepository, ScriptRepository>();
builder.Services.AddScoped<IAssetMaintenanceScheduleRepository, AssetMaintenanceScheduleRepository>();
builder.Services.AddScoped<IAssetMaintenanceHistoryRepository, AssetMaintenanceHistoryRepository>();
builder.Services.AddScoped<IResidentRepository, ResidentRepository>();

// Register services
builder.Services.AddScoped<IFloorService, FloorService>();
builder.Services.AddScoped<IApartmentService, ApartmentServices>();
builder.Services.AddScoped<IAnnouncementService, AnnouncementService>();
builder.Services.AddScoped<IAmenityService, AmenityService>();
builder.Services.AddScoped<IAmenityPackageService, AmenityPackageService>();
builder.Services.AddScoped<IAmenityBookingService, AmenityBookingService>();
builder.Services.AddScoped<IAmenityNotificationService, AmenityNotificationService>();
builder.Services.AddScoped<IAmenityCheckInService, AmenityCheckInService>();
builder.Services.AddScoped<IAccessCardService, AccessCardService>();
builder.Services.AddScoped<ICardHistoryService, CardHistoryService>();
builder.Services.AddScoped<SAMS_BE.Helpers.CardHistoryHelper>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<SAMS_BE.Interfaces.IService.IVoucherService, VoucherService>();
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<IBuildingService, BuildingService>();
builder.Services.AddScoped<IStaffService, StaffService>();
builder.Services.AddHttpClient<IPaymentService, PaymentService>();
builder.Services.AddScoped<IWorkRoleService, WorkRoleService>();
builder.Services.AddScoped<IKeycloakRoleService, KeycloakRoleService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IFaceRecognitionService, FaceRecognitionService>();
builder.Services.AddScoped<IResidentTicketService, ResidentTicketService>();
builder.Services.AddScoped<IInvoicePaymentService, InvoicePaymentService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<IAssetMaintenanceScheduleService, AssetMaintenanceScheduleService>();
builder.Services.AddScoped<IAssetMaintenanceHistoryService, AssetMaintenanceHistoryService>();
builder.Services.AddScoped<ITicketOverdueNotificationService, TicketOverdueNotificationService>();
builder.Services.AddScoped<IResidentService, ResidentService>();

// Register Background Services
builder.Services.AddHostedService<SAMS_BE.BackgroundServices.AnnouncementExpirationService>();


builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });

    // Cấu hình để xử lý file uploads và multipart/form-data
    c.MapType<IFormFile>(() => new OpenApiSchema
    {
        Type = "string",
        Format = "binary"
    });

    // Custom operation filter để xử lý multipart/form-data với complex types
    c.OperationFilter<SAMS_BE.Infrastructure.Swagger.FileUploadOperationFilter>();

    // Bỏ qua lỗi khi generate schema cho endpoints có IFormFile
    c.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFE", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddControllers();

// Invoice services
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();

// Register InvoiceConfiguration repository & service
builder.Services.AddScoped<SAMS_BE.Interfaces.IRepository.IInvoiceConfigurationRepository, SAMS_BE.Repositories.InvoiceConfigurationRepository>();
builder.Services.AddScoped<SAMS_BE.Interfaces.IService.IInvoiceConfigurationService, SAMS_BE.Services.InvoiceConfigurationService>();

// InvoiceDetail services
builder.Services.AddScoped<IInvoiceDetailRepository, InvoiceDetailRepository>();
builder.Services.AddScoped<IInvoiceDetailService, InvoiceDetailService>();

// Voucher services
builder.Services.AddScoped<SAMS_BE.Interfaces.IRepository.IVoucherRepository, VoucherRepository>();
builder.Services.AddScoped<SAMS_BE.Interfaces.IService.IVoucherService, VoucherService>();

// VoucherItem services
builder.Services.AddScoped<IVoucherItemRepository, VoucherItemRepository>();
builder.Services.AddScoped<IVoucherItemService, VoucherItemService>();

// Receipt services
builder.Services.AddScoped<IReceiptRepository, ReceiptRepository>();
builder.Services.AddScoped<IReceiptService, ReceiptService>();

// Journal Entry services
builder.Services.AddScoped<IJournalEntryService, JournalEntryService>();

// PaymentMethod services
builder.Services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>();
builder.Services.AddScoped<IPaymentMethodService, PaymentMethodService>();

// DI Service/Repository
builder.Services.AddScoped<SAMS_BE.Interfaces.IRepository.IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<SAMS_BE.Interfaces.IService.IDocumentService, DocumentService>();
builder.Services.AddScoped<SAMS_BE.Interfaces.IServiceTypeRepository, ServiceTypeRepository>();
builder.Services.AddScoped<SAMS_BE.Interfaces.IServicePriceRepository, ServicePriceRepository>();
builder.Services.AddScoped<IServicePriceService, ServicePriceService>();
builder.Services.AddScoped<IServiceTypeService, ServiceTypeService>();
// Dùng Cloudinary để lưu trữ file
builder.Services.AddSingleton<IFileStorageHelper, CloudinaryStorageHelper>();
builder.Services.AddHttpContextAccessor();

// Asset Maintenance Schedule services
builder.Services.AddScoped<IAssetMaintenanceScheduleRepository, AssetMaintenanceScheduleRepository>();
builder.Services.AddScoped<IAssetMaintenanceScheduleService, AssetMaintenanceScheduleService>();

// Asset Maintenance History services
builder.Services.AddScoped<IAssetMaintenanceHistoryRepository, AssetMaintenanceHistoryRepository>();
builder.Services.AddScoped<IAssetMaintenanceHistoryService, AssetMaintenanceHistoryService>();

builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddScoped<IKeycloakResourceService, KeycloakResourceService>();


// Add Hangfire
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"), new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(10),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(10),
        QueuePollInterval = TimeSpan.Zero,
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = false,
        TransactionTimeout = TimeSpan.FromMinutes(10)
    }));

// Configure Hangfire Server with delayed polling to allow job registration
builder.Services.AddHangfireServer(options =>
{
    options.SchedulePollingInterval = TimeSpan.FromSeconds(15); // Increased from 1 to 15 seconds
    options.HeartbeatInterval = TimeSpan.FromSeconds(30); // Increased from 5 to 30 seconds
    options.ServerCheckInterval = TimeSpan.FromMinutes(1);
    options.ServerTimeout = TimeSpan.FromMinutes(5);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SAMS-BE");
        c.RoutePrefix = "swagger";
    });
}


//  Serve static files (required for Hangfire Dashboard UI)
app.UseStaticFiles();

//  Add Hangfire Dashboard
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() },
    IgnoreAntiforgeryToken = true
});

app.UseCors("AllowFE");

//  Global Exception Handler
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (BusinessException ex) 
    {
        context.Response.StatusCode = StatusCodes.Status409Conflict;
        context.Response.ContentType = "application/json";
        object? code = null;

        if (ex.Data.Contains("code"))
        {
            code = ex.Data["code"];
        }

        await context.Response.WriteAsJsonAsync(new
        {
            code,
            message = ex.Message
        });
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, $"Unhandled exception: {context.Request.Method} {context.Request.Path}");

        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(new
        {
            message = "Internal server error"
        });
    }
});




app.UseHttpsRedirection();
app.UseAuthentication();
app.UseMiddleware<TenantContextMiddleware>();
//app.UseMiddleware<SAMS_BE.Security.DynamicPermissionMiddleware>();
app.UseAuthorization();
app.MapControllers();

// Configure recurring jobs in background to avoid startup timeout
_ = Task.Run(async () =>
{
    // Wait longer for app and Hangfire Server to fully start
    await Task.Delay(TimeSpan.FromSeconds(10));
    
    try
    {
        // CRITICAL: Clear ALL Hangfire recurring job data from database first
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        await SAMS_BE.Helpers.HangfireHelper.ClearAllRecurringJobsAsync(connectionString!);
        
        // Wait longer for database cleanup to complete
        await Task.Delay(TimeSpan.FromSeconds(5));
        
        // Now register all recurring jobs
        RecurringJob.AddOrUpdate<IInvoiceService>(
            "generate-monthly-invoices",
            service => service.RunConfiguredMonthlyGenerationAsync(),
            "0 0 * * *");

        RecurringJob.AddOrUpdate<IInvoiceService>(
            "update-overdue-invoices",
            service => service.UpdateOverdueInvoicesAsync(),
            "0 0 * * *");

        RecurringJob.AddOrUpdate<IAssetMaintenanceScheduleService>(
            "send-maintenance-reminders",
            service => service.SendMaintenanceRemindersAsync(),
            "0 8 * * *");

        RecurringJob.AddOrUpdate<IAssetMaintenanceScheduleService>(
            "start-maintenance",
            service => service.StartMaintenanceJobAsync(),
            "*/10 * * * * *"); 
        RecurringJob.AddOrUpdate<IAssetMaintenanceScheduleService>(
            "complete-maintenance",
            service => service.CompleteMaintenanceJobAsync(),
            "*/10 * * * * *"); 

        RecurringJob.AddOrUpdate<IAmenityBookingService>(
            "update-expired-bookings",
            service => service.UpdateExpiredBookingsAsync(),
            "0 0 * * *");

        RecurringJob.AddOrUpdate<ITicketOverdueNotificationService>(
            "notify-ticket-due-soon",
            service => service.CheckAndNotifyOverdueTicketsAsync(),
            "0 8 * * *");
            
        Console.WriteLine("✓ All Hangfire recurring jobs registered successfully");
    }
    catch (Exception ex)
    {
        // Log the error but don't crash the app
        Console.WriteLine($"⚠ Error configuring Hangfire jobs: {ex.Message}");
    }
});

app.UseCors(policy => policy
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());
app.Run();
