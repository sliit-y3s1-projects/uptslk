using System.Text;
using System.Text.Json.Serialization;
using api.Data;
using api.Enums;
using api.Models;
using api.Services;
using api.Services.Payments;
using api.Services.AgentRecovery;
using api.Services.AgentRecovery.Agents;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var urls = builder.Configuration["ASPNETCORE_URLS"] ?? "http://0.0.0.0:5250";
builder.WebHost.UseUrls(urls);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<User, IdentityRole<Guid>>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<TripConflictService>();
builder.Services.AddSingleton<IImageStorageService, SupabaseImageStorageService>();
builder.Services.AddSingleton<IPaymentGateway, StripePaymentGateway>();
builder.Services.AddScoped<BookingPaymentService>();
builder.Services.AddScoped<IRecoveryAgent, NetworkContinuityAgent>();
builder.Services.AddScoped<IRecoveryAgent, FleetReadinessAgent>();
builder.Services.AddScoped<IRecoveryAgent, DispatchRecoveryAgent>();
builder.Services.AddScoped<IRecoveryAgent, PassengerFareImpactAgent>();
builder.Services.AddScoped<RecoveryWorkflowService>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (string.IsNullOrEmpty(context.Token)) context.Token = context.Request.Cookies["upts_access_token"];
                return Task.CompletedTask;
            }
        };
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebApp", policy =>
    {
        var webOrigins = new[]
            {
                "http://localhost:5173",
                "https://uptslk.vercel.app",
                builder.Configuration["Payments:Stripe:WebAppBaseUrl"]
            }
            .OfType<string>()
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        policy.WithOrigins(webOrigins)
              .AllowAnyHeader().AllowCredentials()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    var adminEmail = builder.Configuration["BootstrapAdmin:Email"] ?? "admin@upts.lk";
    var adminPassword = builder.Configuration["BootstrapAdmin:Password"] ?? "admin123";
    var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
    if (existingAdmin is null)
    {
        var admin = new User { UserName = adminEmail, Email = adminEmail, Name = "UPTSLK Super Admin", Role = UserRole.Admin };
        var result = await userManager.CreateAsync(admin, adminPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException($"Could not create the bootstrap admin: {string.Join(", ", result.Errors.Select(error => error.Description))}");
    }
    else if (!existingAdmin.IsActive)
    {
        existingAdmin.IsActive = true;
        await userManager.UpdateAsync(existingAdmin);
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowWebApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
