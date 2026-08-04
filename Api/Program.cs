using System.Text;
using Application;
using Domain.Entities;
using Persistence;
using Persistence.Contexts;
using Shared;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Configuration from .env
builder.Configuration["ConnectionStrings:DefaultConnection"] = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
builder.Configuration["JwtSettings:Key"] = Environment.GetEnvironmentVariable("JWT_KEY");
builder.Configuration["JwtSettings:Issuer"] = Environment.GetEnvironmentVariable("JWT_ISSUER");
builder.Configuration["JwtSettings:Audience"] = Environment.GetEnvironmentVariable("JWT_AUDIENCE");
builder.Configuration["EmailSetting:Host"] = Environment.GetEnvironmentVariable("SMTP_HOST");
builder.Configuration["EmailSetting:Port"] = Environment.GetEnvironmentVariable("SMTP_PORT");
builder.Configuration["EmailSetting:User"] = Environment.GetEnvironmentVariable("SMTP_USER");
builder.Configuration["EmailSetting:Password"] = Environment.GetEnvironmentVariable("SMTP_PASSWORD");
builder.Configuration["EmailSetting:SenderName"] = Environment.GetEnvironmentVariable("SMTP_SENDER_NAME");

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromMinutes(30);
});

// JWT Authentication
var jwtKey = builder.Configuration["JwtSettings:Key"]!;
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"]!;
var jwtAudience = builder.Configuration["JwtSettings:Audience"]!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Layers
builder.Services.AddApplicationLayer();
builder.Services.AddPersistenceLayer();
builder.Services.AddSharedInfrastructure(builder.Configuration);

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Artemis Banking Pro API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
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
            Array.Empty<string>()
        }
    });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// Startup seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

    var connection = context.Database.GetDbConnection();
    await connection.OpenAsync();

    using var command = connection.CreateCommand();
    command.CommandText = "EXEC sp_getapplock @Resource = 'ArtemisProStartup', @LockMode = 'Exclusive', @LockTimeout = 120000";
    await command.ExecuteNonQueryAsync();

    try
    {
        await context.Database.MigrateAsync();
        await Persistence.Seeds.DefaultRolesAndUsers.SeedAsync(context, userManager, roleManager);
    }
    finally
    {
        using var releaseCommand = connection.CreateCommand();
        releaseCommand.CommandText = "EXEC sp_releaseapplock @Resource = 'ArtemisProStartup'";
        await releaseCommand.ExecuteNonQueryAsync();
        await connection.CloseAsync();
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
