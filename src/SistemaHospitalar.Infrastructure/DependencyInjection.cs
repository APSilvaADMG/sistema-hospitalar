using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Security;
using SistemaHospitalar.Infrastructure.Ai;
using SistemaHospitalar.Infrastructure.Auth;
using SistemaHospitalar.Infrastructure.Connect;
using SistemaHospitalar.Infrastructure.Help;
using SistemaHospitalar.Infrastructure.Messaging;
using SistemaHospitalar.Infrastructure.Pix;
using SistemaHospitalar.Infrastructure.Persistence;
using SistemaHospitalar.Infrastructure.Research;
using SistemaHospitalar.Infrastructure.Security;
using SistemaHospitalar.Infrastructure.Services;
using SistemaHospitalar.Infrastructure.OfficialUpdates;
using SistemaHospitalar.Infrastructure.OfficialUpdates.Providers;
using SistemaHospitalar.Infrastructure.Tiss;
using SistemaHospitalar.Infrastructure.TvSignage;

namespace SistemaHospitalar.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<AppDbContext>(options =>
        {
            var builder = options.UseNpgsql(connectionString)
                .AddInterceptors(new AuditLogImmutabilityInterceptor());

            if (string.Equals(Environment.GetEnvironmentVariable("GTH_ALLOW_LOAD_SEED"), "true", StringComparison.OrdinalIgnoreCase))
            {
                builder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
            }
        });

        services.Configure<HospitalOptions>(configuration.GetSection(HospitalOptions.SectionName));
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<FieldEncryptionOptions>(configuration.GetSection(FieldEncryptionOptions.SectionName));
        services.AddSingleton<IFieldEncryptionService, FieldEncryptionService>();
        services.AddSingleton<Startup.DatabaseInitializationState>();
        services.AddSingleton<JwtTokenGenerator>();

        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException("JWT settings not configured.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key))
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddSingleton<IAuthorizationHandler, PermissionAnyAuthorizationHandler>();
        services.AddAuthorization(options =>
        {
            foreach (var permission in PermissionCodes.All)
            {
                options.AddPolicy($"perm:{permission}", policy =>
                    policy.Requirements.Add(new PermissionRequirement(permission)));
            }

            options.AddPolicy("operations.realtime", policy =>
                policy.Requirements.Add(new PermissionAnyRequirement(
                    PermissionCodes.TransportOperate,
                    PermissionCodes.CleaningOperate,
                    PermissionCodes.TransportManage,
                    PermissionCodes.CleaningManage,
                    PermissionCodes.PatientsRead,
                    PermissionCodes.PepRead,
                    PermissionCodes.HospitalizationManage)));

            options.AddPolicy("connect.realtime", policy =>
                policy.Requirements.Add(new PermissionAnyRequirement(
                    PermissionCodes.ConnectRead,
                    PermissionCodes.ConnectWrite,
                    PermissionCodes.PatientsRead,
                    PermissionCodes.PepRead,
                    PermissionCodes.BillingRead)));
        });
        services.AddSignalR();
        services.AddSingleton<IOperationsRealtimeNotifier, Realtime.OperationsRealtimeNotifier>();
        services.AddSingleton<IConnectRealtimeNotifier, Realtime.ConnectRealtimeNotifier>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IMedicalRecordService, MedicalRecordService>();
        services.AddScoped<IPatientIdentityService, PatientIdentityService>();
        services.AddScoped<IBedsideCareService, BedsideCareService>();
        services.AddScoped<IDigitalRecordService, DigitalRecordService>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<IProfessionalService, ProfessionalService>();
        services.AddScoped<IFinancialAccountService, FinancialAccountService>();
        services.AddScoped<IFinancialCashSessionService, FinancialCashSessionService>();
        services.AddScoped<IMiscellaneousReceiptService, MiscellaneousReceiptService>();
        services.AddScoped<IVaccinationService, VaccinationService>();
        services.AddScoped<IWardPharmacyService, WardPharmacyService>();
        services.AddScoped<IBillingDashboardService, BillingDashboardService>();
        services.AddScoped<IPixPaymentService, PixPaymentService>();
        services.AddScoped<IHospitalizationService, HospitalizationService>();
        services.AddScoped<IHospitalizationHubService, HospitalizationHubService>();
        services.AddScoped<ISurgeryService, SurgeryService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IInventoryConfigService, InventoryConfigService>();
        services.AddScoped<IProductKitService, ProductKitService>();
        services.AddScoped<IStockRequisitionService, StockRequisitionService>();
        services.AddScoped<IWarehouseService, WarehouseService>();
        services.AddScoped<IPharmacyService, PharmacyService>();
        services.AddScoped<ILabService, LabService>();
        services.AddScoped<IClinicalCatalogService, ClinicalCatalogService>();
        services.AddScoped<IHospitalReferenceCatalogService, HospitalReferenceCatalogService>();
        services.AddScoped<IImagingService, ImagingService>();
        services.AddScoped<IHrService, HrService>();
        services.AddScoped<IBiService, BiService>();
        services.AddScoped<IReportsService, ReportsService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ICommandCenterService, CommandCenterService>();
        services.AddScoped<IPatientTimelineService, PatientTimelineService>();
        services.AddScoped<IAdministrativeExtensionsService, AdministrativeExtensionsService>();
        services.AddScoped<ITissBillingService, TissBillingService>();
        services.AddScoped<IGuidesHubService, GuidesHubService>();
        services.AddScoped<IServiceUnitService, ServiceUnitService>();
        services.AddScoped<ISusGuideService, SusGuideService>();
        services.AddScoped<GuideAuditLogger>();
        services.AddScoped<ClinicalStatusAuditLogger>();
        services.AddHttpContextAccessor();
        services.AddScoped<ITissClinicalSourceService, TissClinicalSourceService>();
        services.AddScoped<IInsuranceIntegrationService, InsuranceIntegrationService>();
        services.AddScoped<ITissExtendedService, TissExtendedService>();
        services.AddSingleton<MockOperatorTissClient>();
        services.AddHttpClient<HttpOperatorTissClient>();
        services.AddScoped<IAiService, AiService>();
        services.AddScoped<IClinicalIntelligenceService, ClinicalIntelligenceService>();
        services.Configure<GroqOptions>(configuration.GetSection(GroqOptions.SectionName));
        services.Configure<MimicResearchOptions>(configuration.GetSection(MimicResearchOptions.SectionName));
        services.AddSingleton<MimicEtlStateHolder>();
        services.AddScoped<MimicEtlImporter>();
        services.AddScoped<IMimicResearchService, MimicResearchService>();
        services.AddHttpClient<IGroqLlmService, GroqLlmService>();
        services.AddScoped<IIntegrationService, IntegrationService>();
        services.AddScoped<IIntegrationReadinessService, IntegrationReadinessService>();
        services.AddScoped<IPatientPortalService, PatientPortalService>();
        services.AddScoped<IEmergencyService, EmergencyService>();
        services.AddScoped<IPurchasingService, PurchasingService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<ISecurityComplianceService, SecurityComplianceService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IUnifiedNotificationHubService, UnifiedNotificationHubService>();
        services.AddScoped<IPendencyService, PendencyService>();
        services.AddScoped<IIcuService, IcuService>();
        services.AddScoped<IAmbulanceService, AmbulanceService>();
        services.AddScoped<IParkingService, ParkingService>();
        services.AddScoped<INutritionService, NutritionService>();
        services.AddScoped<IConsultingRoomService, ConsultingRoomService>();
        services.AddScoped<IHospitalityService, HospitalityService>();
        services.AddScoped<IClinicalEngineeringService, ClinicalEngineeringService>();
        services.AddScoped<ISecurityService, SecurityService>();
        services.AddScoped<IPhysicalAccessService, PhysicalAccessService>();
        services.AddScoped<ITransportService, TransportService>();
        services.AddScoped<IHotelariaHospitalarService, HotelariaHospitalarService>();
        services.AddScoped<ISyncService, SyncService>();
        services.AddScoped<IGovernmentIntegrationService, GovernmentIntegrationService>();
        services.AddScoped<ICmeService, CmeService>();
        services.AddScoped<IHemotherapyService, HemotherapyService>();
        services.AddScoped<IDialysisService, DialysisService>();
        services.AddScoped<ILaundryService, LaundryService>();
        services.AddScoped<IOncologyService, OncologyService>();
        services.AddScoped<IPhysiotherapyService, PhysiotherapyService>();
        services.AddScoped<ITelemedicineService, TelemedicineService>();
        services.AddScoped<IInfectionControlService, InfectionControlService>();
        services.AddScoped<IHospitalEventEngine, HospitalEventEngine>();
        services.AddScoped<IWasteService, WasteService>();
        services.AddScoped<ITaskEngineService, TaskEngineService>();
        services.AddSingleton<HospitalEventPublisher>();

        services.Configure<ConnectSettings>(configuration.GetSection(ConnectSettings.SectionName));
        services.Configure<SecuritySettings>(configuration.GetSection(SecuritySettings.SectionName));
        services.AddHttpClient<MetaWhatsAppProvider>();
        services.AddSingleton<WhatsAppHealthService>();
        services.AddScoped<ConnectTemplateBuilder>();
        services.AddScoped<ConnectMessagingService>();
        services.AddScoped<IConnectBotService, ConnectBotService>();
        services.AddScoped<IConnectService, ConnectService>();
        services.AddScoped<IConnectMailService, ConnectMailService>();
        services.AddScoped<IConnectAttachmentStorage, ConnectAttachmentStorage>();
        services.AddScoped<IConnectChatService, ConnectChatService>();
        services.AddScoped<IConnectNotificationService, ConnectNotificationService>();
        services.AddScoped<IBulletinService, BulletinService>();
        services.AddScoped<IConnectCommSummaryService, ConnectCommSummaryService>();
        services.AddScoped<IConnectTicketService, ConnectTicketService>();
        services.AddScoped<IConnectTaskService, ConnectTaskService>();
        services.AddScoped<IConnectWorkflowService, ConnectWorkflowService>();
        services.AddScoped<IConnectCalendarService, ConnectCalendarService>();
        services.AddScoped<IConnectContextService, ConnectContextService>();
        services.AddScoped<IConnectAiAssistantService, ConnectAiAssistantService>();
        services.AddScoped<IHelpService, HelpService>();
        services.Configure<TvSignageSettings>(configuration.GetSection(TvSignageSettings.SectionName));
        services.AddHttpClient("TvAzureSpeech");
        services.AddHttpClient("TvGoogleSpeech");
        services.AddHttpClient("TvOpenWeather");
        services.AddScoped<ITvWeatherService, TvWeatherService>();
        services.AddSingleton<ITvSpeechService, TvSpeechService>();
        services.AddScoped<ITvSignageService, TvSignageService>();
        services.AddScoped<ITvSignageMediaStorage, TvSignageMediaStorage>();
        services.AddScoped<ITvSignageRealtimeNotifier, TvSignageRealtimeNotifier>();
        services.AddHostedService<TvSignageBackgroundWorker>();
        services.AddScoped<IConnectAppointmentIntegration, ConnectAppointmentIntegration>();
        services.AddScoped<IWhatsAppProvider>(sp =>
        {
            var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ConnectSettings>>().Value;
            return settings.WhatsApp.UseMockProvider
                ? sp.GetRequiredService<MockWhatsAppProvider>()
                : sp.GetRequiredService<MetaWhatsAppProvider>();
        });
        services.AddScoped<MockWhatsAppProvider>();
        services.AddHostedService<ConnectReminderWorker>();
        services.AddHostedService<ConnectCollectionWorker>();
        services.AddHostedService<ConnectSlaMonitorWorker>();
        services.AddHostedService<ConnectCalendarReminderWorker>();
        services.AddHostedService<PendencySyncWorker>();

        services.Configure<OfficialUpdatesSettings>(configuration.GetSection(OfficialUpdatesSettings.SectionName));
        services.AddHttpClient(nameof(OfficialUpdatesService));
        services.AddHttpClient(nameof(SigtapOfficialSyncService))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.All,
            });
        services.AddScoped<ISigtapOfficialSyncService, SigtapOfficialSyncService>();
        services.AddScoped<IOfficialUpdateProvider, TussOfficialUpdateProvider>();
        services.AddScoped<IOfficialUpdateProvider, TissOfficialUpdateProvider>();
        services.AddScoped<IOfficialUpdateProvider, SigtapOfficialUpdateProvider>();
        services.AddScoped<IOfficialUpdateProvider, AnsOfficialUpdateProvider>();
        services.AddScoped<IOfficialUpdateProvider, SusTablesOfficialUpdateProvider>();
        services.AddScoped<IOfficialUpdateProvider, AnvisaOfficialUpdateProvider>();
        services.AddScoped<IOfficialUpdateProvider, BrasindiceOfficialUpdateProvider>();
        services.AddScoped<IOfficialUpdateProvider, SimproOfficialUpdateProvider>();
        services.AddScoped<IOfficialUpdatesService, OfficialUpdatesService>();
        services.AddHostedService<OfficialUpdatesSchedulerWorker>();

        services.AddBularioIntegration(configuration);

        return services;
    }
}
