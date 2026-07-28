using Application;
using Domain.Entities;
using Persistence;
using Persistence.Contexts;
using Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Configuration from .env
builder.Configuration["ConnectionStrings:DefaultConnection"] = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
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

// Cookie configuration
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.Name = "ArtemisBankingPro.Identity.Session";
});

// Layers
builder.Services.AddApplicationLayer();
builder.Services.AddPersistenceLayer();
builder.Services.AddSharedInfrastructure(builder.Configuration);

builder.Services.AddControllersWithViews();

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

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
