using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FightCalendar.Web.Data;
using FightCalendar.Web.Services.Firestore;
using FightCalendar.Web.Services.Sync;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "Fight Calendar API",
        Version = "v1",
        Description = "Read-only feed of upcoming combat sports events (UFC, ONE, RIZIN, BKFC, and other tracked "
            + "promotions), scraped from Tapology. No authentication required."
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));
});

// No email sender is configured, so accounts can't confirm via email -
// requiring confirmation would lock every new registration out.
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

builder.Services.Configure<FirestoreOptions>(builder.Configuration.GetSection(FirestoreOptions.SectionName));
builder.Services.AddHttpClient<FirestoreEventsClient>();
builder.Services.AddScoped<EventSyncService>();
builder.Services.AddScoped<EventSyncRunner>();
builder.Services.AddHostedService<FirestoreSyncBackgroundService>();

// Lets a separately-hosted React frontend (a different origin) call /api/*.
// Origins come from config, not hardcoded, so prod can point at the real
// frontend URL without a code change.
const string FrontendCorsPolicy = "Frontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
// Swagger is available on the deployed "dev" environment too (ASPNETCORE_ENVIRONMENT=Staging) -
// that's the whole point of having a dev deployment to poke at. The migrations endpoint and the
// detailed developer exception page stay local-only (true Development), since both would leak
// implementation details or let a stranger apply schema changes if exposed on a public URL.
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseCors(FrontendCorsPolicy);

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();
