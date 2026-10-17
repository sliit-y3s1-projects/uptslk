using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using api.Data;
using api.Entities;
using api.Entities.Enums;
using api.Services.AgentRecovery;
using api.Services.AgentRecovery.Copilot;
using api.Tests.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace api.Tests.AgentRecovery;

public sealed class RecoveryCopilotHttpTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    // ---------- Fleet ----------

    [Fact]
    public async Task Alternatives_ReturnsScoredOptionsBestFirstWithTheCurrentProposalMarked()
    {
        using var scenario = await Scenario.CreateAsync();
        using var client = await scenario.ClientAsync("CentreManager", centreId: scenario.World.Centre.Id);

        var response = await client.GetAsync(scenario.Url("alternatives"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var options = body.GetProperty("alternatives").EnumerateArray().ToList();
        Assert.NotEmpty(options);
        var scores = options.Select(option => option.GetProperty("score").GetDouble()).ToList();
        Assert.Equal(scores.OrderByDescending(score => score), scores);
        Assert.Contains(options, option => option.GetProperty("isCurrent").GetBoolean());
        Assert.All(options, option => Assert.Equal(4, option.GetProperty("factors").GetArrayLength()));
        Assert.True(body.GetProperty("evaluated").GetInt32() >= options.Count);
    }

    [Fact]
    public async Task Simulate_FlagsABusThatCannotCarryEveryPassenger()
    {
        using var scenario = await Scenario.CreateAsync();
        var tiny = await TestWorld.AddVehicleAsync(scenario.Factory, scenario.World.Centre.Id, "NB-TINY", capacity: 2);
        using var client = await scenario.ClientAsync("Admin");

        var response = await client.PostAsJsonAsync(scenario.Url("simulate"), new { vehicleId = tiny.Id, delayMinutes = 15 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(body.GetProperty("feasible").GetBoolean());
        Assert.Equal(0, body.GetProperty("score").GetDouble());
        Assert.Contains("cannot carry", body.GetProperty("issues")[0].GetString());
    }

    [Fact]
    public async Task ApplyAlternative_ReplacesTheProposalAndKeepsTheAuditTrail()
    {
        using var scenario = await Scenario.CreateAsync();
        using var client = await scenario.ClientAsync("Admin");

        var response = await client.PostAsJsonAsync(scenario.Url("proposal"),
            new { vehicleId = scenario.Large.Id, delayMinutes = 30 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var workflow = await scenario.ReloadAsync();
        var proposal = JsonSerializer.Deserialize<RecoveryProposal>(workflow.ProposalJson!, Json)!;
        Assert.Equal(scenario.Large.Id, proposal.VehicleId);
        Assert.Equal(scenario.Trip.ScheduledTime.AddMinutes(30), proposal.ScheduledTime);
        Assert.Contains(workflow.Steps, step => step.AgentName == "Fleet Simulation Agent");
        Assert.Contains("Simulation", workflow.ValidationJson);
        Assert.Equal("[]", workflow.NotificationDraftsJson);
        Assert.Equal(WorkflowStatus.PausedForApproval, workflow.Status);
    }

    [Fact]
    public async Task ApplyAlternative_RefusesAnOptionThatCannotBeUsed()
    {
        using var scenario = await Scenario.CreateAsync();
        var tiny = await TestWorld.AddVehicleAsync(scenario.Factory, scenario.World.Centre.Id, "NB-TINY", capacity: 2);
        using var client = await scenario.ClientAsync("Admin");

        var response = await client.PostAsJsonAsync(scenario.Url("proposal"), new { vehicleId = tiny.Id, delayMinutes = 15 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(scenario.Medium.Id.ToString(), (await scenario.ReloadAsync()).ProposalJson);
    }

    // ---------- Network ----------

    [Fact]
    public async Task Impact_FollowsTheDelayIntoTheNextTripOfTheSameBusAndHonoursTheCoordinator()
    {
        using var scenario = await Scenario.CreateAsync();
        // The recovered trip frees the bus 85 minutes after the original time, but it is due out again at 80.
        var next = await TestWorld.AddTripAsync(scenario.Factory, scenario.World with { Vehicle = scenario.Medium, Driver = scenario.Driver3, Bay = scenario.World.Bay },
            scenario.Trip.ScheduledTime.AddMinutes(80));
        using var client = await scenario.ClientAsync("Admin");

        var impact = await (await client.GetAsync(scenario.Url("impact"))).Content.ReadFromJsonAsync<JsonElement>();

        var node = impact.GetProperty("nodes").EnumerateArray().Single(candidate => candidate.GetProperty("lane").GetString() == "Bus");
        Assert.Equal(next.Id, node.GetProperty("tripId").GetGuid());
        Assert.Equal("High", node.GetProperty("risk").GetString());
        Assert.Equal(5, node.GetProperty("expectedLateMinutes").GetInt32());
        Assert.Equal(1, impact.GetProperty("summary").GetProperty("high").GetInt32());

        var ignored = await (await client.PostAsJsonAsync(scenario.Url("impact/review"), new { tripId = next.Id, decision = "Ignore" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, ignored.GetProperty("summary").GetProperty("high").GetInt32());
        Assert.Equal(1, ignored.GetProperty("summary").GetProperty("ignored").GetInt32());

        await client.PostAsJsonAsync(scenario.Url("impact/review"), new { tripId = next.Id, decision = "Escalate" });
        Assert.Contains("Escalated by coordinator:", (await scenario.ReloadAsync()).ProposalJson);
    }

    [Fact]
    public async Task ExplainImpact_UsesTheAgentWhenAvailableAndRulesOtherwise()
    {
        var narrator = new FakeNarrator { Reply = "The bus is still on the recovered trip." };
        using var scenario = await Scenario.CreateAsync(narrator);
        var next = await TestWorld.AddTripAsync(scenario.Factory, scenario.World with { Vehicle = scenario.Medium, Driver = scenario.Driver3 },
            scenario.Trip.ScheduledTime.AddMinutes(80));
        using var client = await scenario.ClientAsync("Admin");

        var ai = await (await client.PostAsJsonAsync(scenario.Url("impact/explain"), new { tripId = next.Id, question = "Why?" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("Question: Why?", narrator.LastPrompt);
        narrator.Reply = null;
        var rules = await (await client.PostAsJsonAsync(scenario.Url("impact/explain"), new { tripId = next.Id }))
            .Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("ai", ai.GetProperty("source").GetString());
        Assert.Equal("rules", rules.GetProperty("source").GetString());
        Assert.Contains("late", rules.GetProperty("answer").GetString());
    }

    [Fact]
    public async Task ExplainImpact_ReturnsNotFoundForATripThatIsNotInTheAnalysis()
    {
        using var scenario = await Scenario.CreateAsync();
        using var client = await scenario.ClientAsync("Admin");

        var response = await client.PostAsJsonAsync(scenario.Url("impact/explain"), new { tripId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- Passenger ----------

    [Fact]
    public async Task Messages_SplitPassengersIntoGroups()
    {
        using var scenario = await Scenario.CreateAsync();
        using var client = await scenario.ClientAsync("Admin");

        var body = await (await client.GetAsync(scenario.Url("messages"))).Content.ReadFromJsonAsync<JsonElement>();

        var segments = body.GetProperty("segments").EnumerateArray().ToDictionary(item => item.GetProperty("segment").GetString()!);
        Assert.Equal(3, body.GetProperty("totalRecipients").GetInt32());
        Assert.Equal(new[] { "priority", "app", "no-account" }, segments.Keys);
        Assert.All(segments.Values, item => Assert.Equal(1, item.GetProperty("recipients").GetInt32()));
        Assert.False(segments["no-account"].GetProperty("canReceiveInApp").GetBoolean());
        Assert.Contains("Staff at the bay", segments["priority"].GetProperty("message").GetString());
    }

    [Fact]
    public async Task RegenerateMessage_KeepsAnAgentRewriteOnlyWhenItStillStatesTheFacts()
    {
        var narrator = new FakeNarrator();
        using var scenario = await Scenario.CreateAsync(narrator);
        using var client = await scenario.ClientAsync("Admin");
        var time = scenario.Trip.ScheduledTime.AddMinutes(15).ToString("HH:mm");

        narrator.Reply = $"Friendly note: route {scenario.World.Route.RouteNumber} leaves at {time} from bay B-BAY2. Thanks for your patience!";
        var good = await (await client.PostAsJsonAsync(scenario.Url("messages/app/regenerate"), new { instruction = "make it friendly" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        narrator.Reply = "Everything is fine, no changes at all, please relax and enjoy your day.";
        var bad = await (await client.PostAsJsonAsync(scenario.Url("messages/app/regenerate"), new { instruction = "make it short" }))
            .Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("ai", good.GetProperty("source").GetString());
        Assert.Equal("rules", bad.GetProperty("source").GetString());
        Assert.Contains(time, bad.GetProperty("message").GetString());
        Assert.Contains("B-BAY2", bad.GetProperty("message").GetString());
    }

    [Fact]
    public async Task SaveMessage_RejectsWordingThatIsTooShortAndAcceptsGoodWording()
    {
        using var scenario = await Scenario.CreateAsync();
        using var client = await scenario.ClientAsync("Admin");

        var tooShort = await client.PutAsJsonAsync(scenario.Url("messages/app"), new { message = "Late." });
        var unknownGroup = await client.PutAsJsonAsync(scenario.Url("messages/vip"), new { message = "Your bus is late, sorry about that." });
        var good = await client.PutAsJsonAsync(scenario.Url("messages/app"), new { message = "Your bus leaves a little later. Please wait at bay B-BAY2." });

        Assert.Equal(HttpStatusCode.BadRequest, tooShort.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, unknownGroup.StatusCode);
        Assert.Equal(HttpStatusCode.OK, good.StatusCode);
        var body = await good.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("edited", body.GetProperty("source").GetString());
        Assert.NotEmpty(body.GetProperty("warnings").EnumerateArray());
    }

    [Fact]
    public async Task Approval_SendsTheEditedWordingAndRemembersTheApproval()
    {
        using var scenario = await Scenario.CreateAsync();
        using var client = await scenario.ClientAsync("Admin");
        const string wording = "Good news: your bus is running a little late and will now leave from bay B-BAY2.";
        await client.PutAsJsonAsync(scenario.Url("messages/app"), new { message = wording });

        var decision = await client.PostAsJsonAsync(scenario.Url("approval"), new { decision = "Approved" });

        Assert.Equal(HttpStatusCode.OK, decision.StatusCode);
        var notices = await scenario.Factory.WithDbAsync(db => db.PassengerNotifications.Include(n => n.Passenger).ToListAsync());
        Assert.Equal(3, notices.Count);
        Assert.Equal(wording, notices.Single(n => n.Passenger.UserId.HasValue).Message);
        Assert.All(notices.Where(n => !n.Passenger.UserId.HasValue), n => Assert.NotEqual(wording, n.Message));
        var memory = await scenario.Factory.WithDbAsync(db => db.RecoveryMemories.ToListAsync());
        Assert.Equal(RecoveryMemoryKind.Approved, Assert.Single(memory).Kind);

        var sent = await (await client.GetAsync(scenario.Url("messages"))).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(sent.GetProperty("sent").GetBoolean());
    }

    // ---------- Dispatch ----------

    [Fact]
    public async Task Revise_ExcludesWhatTheManagerRejectedAndRemembersWhy()
    {
        using var scenario = await Scenario.CreateAsync();
        using var client = await scenario.ClientAsync("CentreManager", centreId: scenario.World.Centre.Id);

        var response = await client.PostAsJsonAsync(scenario.Url("revise"),
            new { reason = "That driver already worked a double shift" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("PausedForApproval", body.GetProperty("status").GetString());
        Assert.Equal(1, body.GetProperty("revision").GetInt32());
        Assert.Contains(body.GetProperty("appliedConstraints").EnumerateArray(), item => item.GetString()!.StartsWith("Avoid driver"));

        var workflow = await scenario.ReloadAsync();
        var proposal = JsonSerializer.Deserialize<RecoveryProposal>(workflow.ProposalJson!, Json)!;
        Assert.NotEqual(scenario.Driver2.Id, proposal.DriverId);
        Assert.Equal(1, workflow.ApprovalRequests.Count(item => item.Decision == ApprovalDecision.Rejected));
        Assert.Equal(1, workflow.ApprovalRequests.Count(item => item.Decision == ApprovalDecision.Pending));
        Assert.Contains(workflow.Steps, step => step.AgentName == "Dispatch Revision Agent");
        var memory = await scenario.Factory.WithDbAsync(db => db.RecoveryMemories.SingleAsync());
        Assert.Equal(RecoveryMemoryKind.Rejected, memory.Kind);
        Assert.True(memory.AvoidDriver);
        Assert.Equal(scenario.Driver2.Id, memory.DriverId);
    }

    [Fact]
    public async Task Revise_HonoursExplicitConstraintsAndALaterRevisionKeepsEarlierOnes()
    {
        using var scenario = await Scenario.CreateAsync();
        using var client = await scenario.ClientAsync("Admin");

        await client.PostAsJsonAsync(scenario.Url("revise"), new { reason = "Wrong bus for this route", avoidVehicle = true });
        var first = JsonSerializer.Deserialize<RecoveryProposal>((await scenario.ReloadAsync()).ProposalJson!, Json)!;
        var second = await client.PostAsJsonAsync(scenario.Url("revise"), new { reason = "Still not right", avoidDriver = true });

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.NotEqual(scenario.Medium.Id, first.VehicleId);
        var workflow = await scenario.ReloadAsync();
        var last = JsonSerializer.Deserialize<RecoveryProposal>(workflow.ProposalJson!, Json)!;
        Assert.NotEqual(scenario.Medium.Id, last.VehicleId);
        Assert.NotEqual(first.DriverId, last.DriverId);
        Assert.Equal(2, workflow.ApprovalRequests.Count(item => item.Decision == ApprovalDecision.Rejected));
    }

    [Fact]
    public async Task Revise_StopsTheRecoveryWhenNothingMeetsTheConstraints()
    {
        using var scenario = await Scenario.CreateAsync(spareResources: false);
        using var client = await scenario.ClientAsync("Admin");

        var response = await client.PostAsJsonAsync(scenario.Url("revise"), new { reason = "Wrong bus", avoidVehicle = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Failed", body.GetProperty("status").GetString());
        var workflow = await scenario.ReloadAsync();
        Assert.Equal(WorkflowStatus.Failed, workflow.Status);
        Assert.Contains("Wrong bus", workflow.FailureReason);
        Assert.Equal(1, await scenario.Factory.CountAsync<RecoveryMemory>());
    }

    [Fact]
    public async Task Revise_IsForManagersOnlyAndNeedsAReason()
    {
        using var scenario = await Scenario.CreateAsync();
        using var dispatcher = await scenario.ClientAsync("Dispatcher", centreId: scenario.World.Centre.Id);
        using var admin = await scenario.ClientAsync("Admin");

        var forbidden = await dispatcher.PostAsJsonAsync(scenario.Url("revise"), new { reason = "No good" });
        var blank = await admin.PostAsJsonAsync(scenario.Url("revise"), new { reason = " " });

        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, blank.StatusCode);
        Assert.Equal(0, await scenario.Factory.CountAsync<RecoveryMemory>());
    }

    [Fact]
    public async Task RejectionsLaterSteerTheSimulatorAwayFromTheSameChoice()
    {
        using var scenario = await Scenario.CreateAsync();
        using var admin = await scenario.ClientAsync("Admin");
        await admin.PostAsJsonAsync(scenario.Url("revise"), new { reason = "Driver is too tired", avoidDriver = true });

        var lessons = await (await admin.GetAsync(scenario.Url("lessons"))).Content.ReadFromJsonAsync<JsonElement>();
        var options = await (await admin.GetAsync(scenario.Url("alternatives"))).Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(1, lessons.GetArrayLength());
        Assert.Equal("Rejected", lessons[0].GetProperty("kind").GetString());
        // Drivers a manager rejected are tried last, so no top option should use them while another driver is free.
        var best = options.GetProperty("alternatives")[0];
        Assert.NotEqual(scenario.Driver2.Id, best.GetProperty("driverId").GetGuid());
    }

    [Fact]
    public async Task StartingARecoveryAvoidsWhatManagersRejectedBefore()
    {
        using var scenario = await Scenario.CreateAsync(startWorkflow: false);
        // A remembered decision belongs to an earlier, closed workflow on the same route.
        var earlier = await scenario.AddFinishedWorkflowAsync();
        await scenario.Factory.WithDbAsync(async db =>
        {
            db.Add(new RecoveryMemory
            {
                CentreId = scenario.World.Centre.Id, WorkflowId = earlier, Kind = RecoveryMemoryKind.Rejected,
                IncidentType = IncidentType.Breakdown, RouteNumber = scenario.World.Route.RouteNumber,
                Summary = "Bus NB-MED", VehicleId = scenario.Medium.Id, AvoidVehicle = true, Reason = "Air conditioning is broken"
            });
            await db.SaveChangesAsync();
        });
        using var admin = await scenario.ClientAsync("Admin");

        var started = await admin.PostAsJsonAsync("/api/v1/agent-recovery/workflows", new { incidentId = scenario.Incident.Id });

        Assert.Equal(HttpStatusCode.Created, started.StatusCode);
        var id = (await started.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var workflow = await scenario.Factory.WithDbAsync(db => db.AgentWorkflows.Include(w => w.Steps).SingleAsync(w => w.Id == id));
        Assert.Equal(WorkflowStatus.PausedForApproval, workflow.Status);
        var proposal = JsonSerializer.Deserialize<RecoveryProposal>(workflow.ProposalJson!, Json)!;
        Assert.Equal(scenario.Large.Id, proposal.VehicleId);
        Assert.Contains(workflow.Steps, step => step.AgentName == "Recovery Memory Agent");
    }

    // ---------- Access ----------

    [Fact]
    public async Task Tools_AreHiddenFromOtherCentresAndFromWorkflowsNotWaitingForApproval()
    {
        using var scenario = await Scenario.CreateAsync();
        using var outsider = await scenario.ClientAsync("CentreManager", centreId: Guid.NewGuid());
        using var admin = await scenario.ClientAsync("Admin");
        using var anonymous = scenario.Factory.CreateAnonymousClient();

        var foreign = await outsider.GetAsync(scenario.Url("alternatives"));
        var unauthenticated = await anonymous.GetAsync(scenario.Url("alternatives"));
        await scenario.Factory.WithDbAsync(async db =>
        {
            var workflow = await db.AgentWorkflows.SingleAsync(w => w.Id == scenario.WorkflowId);
            workflow.Status = WorkflowStatus.Completed;
            await db.SaveChangesAsync();
        });
        var finished = await admin.GetAsync(scenario.Url("alternatives"));

        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticated.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, finished.StatusCode);
    }

    private sealed class FakeNarrator : IRecoveryNarrator
    {
        public string? Reply { get; set; }
        public string? LastPrompt { get; private set; }

        public Task<string?> GenerateAsync(string instruction, string prompt, CancellationToken cancellationToken)
        {
            LastPrompt = prompt;
            return Task.FromResult(Reply);
        }
    }

    // One centre in trouble: a broken-down trip with three bookings, two spare buses, spare drivers and a spare bay.
    private sealed class Scenario : IDisposable
    {
        public required TestApiFactory Factory { get; init; }
        public required TestWorld World { get; init; }
        public required Trip Trip { get; init; }
        public required Vehicle Medium { get; init; }
        public required Vehicle Large { get; init; }
        public required Driver Driver2 { get; init; }
        public required Driver Driver3 { get; init; }
        public required Bay Bay2 { get; init; }
        public required Incident Incident { get; init; }
        public Guid WorkflowId { get; init; }

        // The test login only names a user, but approving and rejecting store who decided, so the user must exist.
        public async Task<HttpClient> ClientAsync(string role, Guid? centreId = null)
        {
            var userId = await Factory.WithDbAsync(async db =>
            {
                var user = new User
                {
                    UserName = $"u{Guid.NewGuid():N}@test.local", Email = $"u{Guid.NewGuid():N}@test.local",
                    Name = "Test Reviewer", Role = Enum.Parse<UserRole>(role)
                };
                db.Add(user);
                await db.SaveChangesAsync();
                return user.Id;
            });
            return Factory.CreateClientAs(role, userId, centreId);
        }

        public string Url(string path) => $"/api/v1/agent-recovery/workflows/{WorkflowId}/{path}";

        public static async Task<Scenario> CreateAsync(FakeNarrator? narrator = null, bool spareResources = true, bool startWorkflow = true)
        {
            var factory = new TestApiFactory(services =>
            {
                services.RemoveAll<IRecoveryNarrator>();
                services.AddSingleton<IRecoveryNarrator>(narrator ?? new FakeNarrator());
            });
            await factory.InitializeDatabaseAsync();
            var world = await TestWorld.SeedAsync(factory);
            var medium = await TestWorld.AddVehicleAsync(factory, world.Centre.Id, "NB-MED", 45);
            var large = spareResources
                ? await TestWorld.AddVehicleAsync(factory, world.Centre.Id, "NB-LARGE", 55)
                : medium;
            var driver2 = await TestWorld.AddDriverAsync(factory, world.Centre.Id, "LIC-2");
            var driver3 = spareResources ? await TestWorld.AddDriverAsync(factory, world.Centre.Id, "LIC-3") : driver2;
            var bay2 = await TestWorld.AddBayAsync(factory, world.Centre.Id, "B-BAY2");
            var trip = await TestWorld.AddTripAsync(factory, world);
            var (account, _) = await TestWorld.AddPassengerAsync(factory, world, balance: 500);
            await AddBookingAsync(factory, trip, account.Id, "A1", PassengerCategory.Adult);
            await AddBookingAsync(factory, trip, await AddPassengerAsync(factory, PassengerCategory.Senior), "A2", PassengerCategory.Senior);
            await AddBookingAsync(factory, trip, await AddPassengerAsync(factory, PassengerCategory.Adult), "A3", PassengerCategory.Adult);

            var incident = await factory.WithDbAsync(async db =>
            {
                var item = new Incident
                {
                    CentreId = world.Centre.Id, ReportedByName = "Test operator", TripId = trip.Id, Type = IncidentType.Breakdown,
                    Severity = IncidentSeverity.Medium, Title = "Engine fault", Description = "Bus cannot continue.",
                    Status = IncidentStatus.InProgress, SlaDueAt = DateTime.UtcNow.AddHours(1)
                };
                db.Add(item);
                await db.SaveChangesAsync();
                return item;
            });

            var workflowId = Guid.Empty;
            if (startWorkflow)
            {
                workflowId = await factory.WithDbAsync(async db =>
                {
                    var proposal = new RecoveryProposal(medium.Id, driver2.Id, bay2.Id, trip.ScheduledTime.AddMinutes(15), 3, []);
                    var workflow = new AgentWorkflow
                    {
                        CentreId = world.Centre.Id, IncidentId = incident.Id, TripId = trip.Id, Objective = "Restore service safely.",
                        Status = WorkflowStatus.PausedForApproval, ProposalJson = JsonSerializer.Serialize(proposal, Json), PlanningMode = "Fallback"
                    };
                    db.Add(workflow);
                    await db.SaveChangesAsync();
                    db.Add(new ApprovalRequest { WorkflowId = workflow.Id, Reason = "Approve the validated recovery proposal." });
                    await db.SaveChangesAsync();
                    return workflow.Id;
                });
            }

            return new Scenario
            {
                Factory = factory, World = world, Trip = trip, Medium = medium, Large = large, Driver2 = driver2,
                Driver3 = driver3, Bay2 = bay2, Incident = incident, WorkflowId = workflowId
            };

            static async Task<Guid> AddPassengerAsync(TestApiFactory f, PassengerCategory category) =>
                await f.WithDbAsync(async db =>
                {
                    var passenger = new Passenger { FullName = $"Walk-in {Guid.NewGuid():N}"[..14], PhoneNumber = $"07{Random.Shared.NextInt64(10000000, 99999999)}", Category = category };
                    db.Add(passenger);
                    await db.SaveChangesAsync();
                    return passenger.Id;
                });
        }

        // A closed workflow on the same route, so a remembered decision has something to belong to.
        public async Task<Guid> AddFinishedWorkflowAsync() =>
            await Factory.WithDbAsync(async db =>
            {
                var workflow = new AgentWorkflow
                {
                    CentreId = World.Centre.Id, IncidentId = Incident.Id, TripId = Trip.Id, Objective = "Earlier recovery.",
                    Status = WorkflowStatus.Failed, PlanningMode = "Fallback"
                };
                db.Add(workflow);
                await db.SaveChangesAsync();
                return workflow.Id;
            });

        public async Task<AgentWorkflow> ReloadAsync() =>
            await Factory.WithDbAsync(db => db.AgentWorkflows.AsNoTracking()
                .Include(workflow => workflow.Steps)
                .Include(workflow => workflow.ApprovalRequests)
                .SingleAsync(workflow => workflow.Id == WorkflowId));

        public void Dispose() => Factory.Dispose();

        private static Task AddBookingAsync(TestApiFactory factory, Trip trip, Guid passengerId, string seat, PassengerCategory category) =>
            factory.WithDbAsync(async db =>
            {
                db.Add(new Booking
                {
                    TripId = trip.Id, PassengerId = passengerId, SeatNumber = seat, PassengerCount = 1, Fare = 100,
                    PassengerCategory = category, QrCode = Guid.NewGuid().ToString("N"), Status = BookingStatus.Confirmed
                });
                await db.SaveChangesAsync();
            });
    }
}
