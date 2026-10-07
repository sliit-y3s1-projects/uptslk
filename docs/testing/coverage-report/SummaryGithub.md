# Summary - UPTS API coverage
<details open><summary>Summary</summary>

|||
|:---|:---|
| Generated on: | 10/7/2026 - 7:21:15 PM |
| Coverage date: | 10/7/2026 - 7:21:14 PM |
| Parser: | Cobertura |
| Assemblies: | 1 |
| Classes: | 93 |
| Files: | 56 |
| **Line coverage:** | 74% (3578 of 4831) |
| Covered lines: | 3578 |
| Uncovered lines: | 1253 |
| Coverable lines: | 4831 |
| Total lines: | 6748 |
| **Branch coverage:** | 62.9% (1136 of 1804) |
| Covered branches: | 1136 |
| Total branches: | 1804 |
| **Method coverage:** | [Feature is only available for sponsors](https://reportgenerator.io/pro) |

</details>

## Coverage
<details><summary>api - 74%</summary>

|**Name**|**Line**|**Branch**|
|:---|---:|---:|
|**api**|**74%**|**62.9%**|
|api.Controllers.AgentRecoveryController|16.5%|27%|
|api.Controllers.AuthController|91.1%|75.8%|
|api.Controllers.BookingsController|98%|75.6%|
|api.Controllers.CentresController|84.8%|79.5%|
|api.Controllers.DriversController|98.3%|85.7%|
|api.Controllers.FareRulesController|100%|93.7%|
|api.Controllers.IncidentsController|100%|86.6%|
|api.Controllers.MaintenanceRecordsController|86.2%|71.8%|
|api.Controllers.PassengerNotificationsController|100%|75%|
|api.Controllers.PassengersController|63.4%|43.4%|
|api.Controllers.PaymentsController|100%|84%|
|api.Controllers.RoutesController|98.4%|79.1%|
|api.Controllers.SupportRequestsController|98.1%|96.1%|
|api.Controllers.TripsController|98%|76.5%|
|api.Controllers.VehiclesController|69.2%|52%|
|api.Data.AppDbContext|99.3%||
|api.Data.DbContextLockExtensions|100%|100%|
|api.Data.NullableUtcDateTimeConverter|100%||
|api.Data.UtcDateTimeConverter|100%||
|api.DTOs.AssignUserCentreRequest|100%||
|api.DTOs.AuthResponse|100%||
|api.DTOs.ChangePasswordRequest|100%||
|api.DTOs.CreateUserRequest|100%||
|api.DTOs.LoginRequest|100%||
|api.DTOs.RegisterRequest|100%||
|api.DTOs.UpdateProfileRequest|100%||
|api.DTOs.UpdateUserDetailsRequest|100%||
|api.DTOs.VerifyNicRequest|100%||
|api.Models.Wallet|100%||
|api.Services.AgentRecovery.AgentExecutionResult|100%||
|api.Services.AgentRecovery.AgentRecommendation|100%||
|api.Services.AgentRecovery.Agents.DispatchRecoveryAgent|79.5%|58.3%|
|api.Services.AgentRecovery.Agents.FleetReadinessAgent|17.6%|0%|
|api.Services.AgentRecovery.Agents.NetworkContinuityAgent|18.7%|0%|
|api.Services.AgentRecovery.Agents.PassengerFareImpactAgent|27.2%|0%|
|api.Services.AgentRecovery.AgentToolCall|100%||
|api.Services.AgentRecovery.PassengerNotificationPreparationResult|100%||
|api.Services.AgentRecovery.PassengerNotificationService|94.2%|50%|
|api.Services.AgentRecovery.Planning.AgentCapability|100%||
|api.Services.AgentRecovery.Planning.GeminiRecoveryPlanner|1.4%|0%|
|api.Services.AgentRecovery.Planning.IncidentPlanningSnapshot|100%||
|api.Services.AgentRecovery.Planning.PlannedRecoveryStep|100%||
|api.Services.AgentRecovery.Planning.RecoveryAgentRegistry|77.7%|25%|
|api.Services.AgentRecovery.Planning.RecoveryPlanDraft|100%||
|api.Services.AgentRecovery.Planning.RecoveryPlannerResponse|100%||
|api.Services.AgentRecovery.Planning.RecoveryPlanningInput|100%||
|api.Services.AgentRecovery.Planning.RecoveryPlanningResult|100%||
|api.Services.AgentRecovery.Planning.RecoveryPlanningService|84.2%|66.6%|
|api.Services.AgentRecovery.Planning.RecoveryPlanningUnavailableException|100%||
|api.Services.AgentRecovery.Planning.RecoveryPlanValidator|84.7%|68.7%|
|api.Services.AgentRecovery.Planning.RecoveryReplanAudit|0%||
|api.Services.AgentRecovery.Planning.SafeRecoveryPlanFactory|100%||
|api.Services.AgentRecovery.Planning.TripPlanningSnapshot|100%||
|api.Services.AgentRecovery.ProposalCompositionResult|100%||
|api.Services.AgentRecovery.RecoveryAccessScope|100%|80%|
|api.Services.AgentRecovery.RecoveryActionExecutor|96.6%|75%|
|api.Services.AgentRecovery.RecoveryContext|100%||
|api.Services.AgentRecovery.RecoveryNotificationFactory|100%|100%|
|api.Services.AgentRecovery.RecoveryPlanStep|0%||
|api.Services.AgentRecovery.RecoveryProposal|100%||
|api.Services.AgentRecovery.RecoveryProposalComposer|94.7%|64.6%|
|api.Services.AgentRecovery.RecoveryTextValidator|93.7%|80.9%|
|api.Services.AgentRecovery.RecoveryWorkflowService|3.8%|0.7%|
|api.Services.AgentRecovery.Tools.AssessPassengerImpactInput|100%||
|api.Services.AgentRecovery.Tools.AssessPassengerImpactOutput|100%||
|api.Services.AgentRecovery.Tools.DispatchRecoveryTools|96.8%|75%|
|api.Services.AgentRecovery.Tools.FindConflictFreeDriverInput|100%||
|api.Services.AgentRecovery.Tools.FindConflictFreeDriverOutput|100%||
|api.Services.AgentRecovery.Tools.FindDepartureBayInput|100%||
|api.Services.AgentRecovery.Tools.FindDepartureBayOutput|100%||
|api.Services.AgentRecovery.Tools.FindReplacementVehicleInput|100%||
|api.Services.AgentRecovery.Tools.FindReplacementVehicleOutput|100%||
|api.Services.AgentRecovery.Tools.FleetRecoveryTools|100%|50%|
|api.Services.AgentRecovery.Tools.NetworkRecoveryTools|100%|50%|
|api.Services.AgentRecovery.Tools.PassengerRecoveryTools|100%||
|api.Services.AgentRecovery.Tools.RecoveryToolInputValidator|75%|50%|
|api.Services.AgentRecovery.ValidationResult|0%||
|api.Services.BookingEligibility|100%|92.8%|
|api.Services.CentreAccess|100%|100%|
|api.Services.DatabaseHealthCheck|0%|0%|
|api.Services.JwtTokenService|100%|100%|
|api.Services.Payments.BookingPaymentService|98.2%|76.8%|
|api.Services.Payments.PaymentCheckoutRequest|100%||
|api.Services.Payments.PaymentCheckoutSession|100%||
|api.Services.Payments.PaymentCheckoutStatus|100%||
|api.Services.Payments.PaymentRefundRequest|100%||
|api.Services.Payments.PaymentRefundResult|100%||
|api.Services.Payments.StripePaymentGateway|1.1%|0%|
|api.Services.ScheduleTimes|100%|100%|
|api.Services.SupabaseImageStorageService|4.3%|0%|
|api.Services.TripConflictService|100%|86.3%|
|api.Services.VehicleMaintenanceRules|100%|91.6%|
|Program|86.9%|21.4%|

</details>
