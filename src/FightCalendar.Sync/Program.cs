using FightCalendar.Data;
using FightCalendar.Sync;
using FightCalendar.Sync.Firestore;
using FightCalendar.Sync.Services;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.Configure<FirestoreOptions>(builder.Configuration.GetSection(FirestoreOptions.SectionName));
builder.Services.AddHttpClient<FirestoreEventsClient>();
builder.Services.AddScoped<EventSyncService>();
builder.Services.AddScoped<EventSyncRunner>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
