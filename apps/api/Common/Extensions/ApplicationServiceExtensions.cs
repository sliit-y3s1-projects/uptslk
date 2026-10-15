using api.Interfaces.Services;
using api.Services.AgentRecovery;
using api.Services.AgentRecovery.Agents;
using api.Services.AgentRecovery.Planning;
using api.Services.AgentRecovery.Tools;
using api.Services.External;
using api.Services;

namespace api.Common.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<JwtTokenService>();
        services.AddScoped<TripConflictService>();
        services.AddScoped<BookingPaymentService>();
        services.AddScoped<BookingCancellationService>();
        services.AddScoped<MobileNotificationService>();

        services.AddSingleton<IImageStorageService, SupabaseImageStorageService>();
        services.AddSingleton<IPaymentGateway, StripePaymentGateway>();
        services.AddSingleton<INotificationPublisher, SignalRNotificationPublisher>();

        return services;
    }

    // The AI-assisted incident recovery workflow: agents, their tools and the planner.
    public static IServiceCollection AddAgentRecovery(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<INetworkRecoveryTools, NetworkRecoveryTools>();
        services.AddScoped<IFleetRecoveryTools, FleetRecoveryTools>();
        services.AddScoped<IDispatchRecoveryTools, DispatchRecoveryTools>();
        services.AddScoped<IPassengerRecoveryTools, PassengerRecoveryTools>();

        services.AddScoped<IRecoveryAgent, NetworkContinuityAgent>();
        services.AddScoped<IRecoveryAgent, FleetReadinessAgent>();
        services.AddScoped<IRecoveryAgent, DispatchRecoveryAgent>();
        services.AddScoped<IRecoveryAgent, PassengerFareImpactAgent>();

        services.AddScoped<RecoveryAgentRegistry>();
        services.AddScoped<RecoveryPlanValidator>();
        services.AddScoped<RecoveryProposalComposer>();
        services.AddScoped<PassengerNotificationService>();
        services.AddScoped<RecoveryActionExecutor>();
        services.AddScoped<IRecoveryPlanner, GeminiRecoveryPlanner>();
        services.AddScoped<RecoveryPlanningService>();
        services.AddScoped<RecoveryWorkflowService>();

        services.AddOptions<AgentAiOptions>()
            .Bind(configuration.GetSection(AgentAiOptions.SectionName))
            .PostConfigure(options =>
            {
                if (string.IsNullOrWhiteSpace(options.ApiKey))
                    options.ApiKey = configuration["GEMINI_API_KEY"];
            })
            .Validate(options => !string.IsNullOrWhiteSpace(options.Provider), "AgentAi:Provider is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Model), "AgentAi:Model is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.PromptVersion), "AgentAi:PromptVersion is required.")
            .Validate(options => options.TimeoutSeconds is >= 1 and <= 60, "AgentAi:TimeoutSeconds must be between 1 and 60.")
            .Validate(options => options.MaxPlanningRetries is >= 0 and <= 2, "AgentAi:MaxPlanningRetries must be between 0 and 2.")
            .Validate(options => options.MaxWorkflowReplans is >= 0 and <= 1, "AgentAi:MaxWorkflowReplans must be either 0 or 1.")
            .Validate(options => options.MaximumPlanSteps is >= 4 and <= 12, "AgentAi:MaximumPlanSteps must be between 4 and 12.")
            .ValidateOnStart();

        return services;
    }
}
