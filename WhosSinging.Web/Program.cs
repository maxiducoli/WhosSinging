using Microsoft.EntityFrameworkCore;
using WhosSinging.Web.Configuration;
using WhosSinging.Web.Data;
using WhosSinging.Web.Hubs;
using WhosSinging.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// ===== Configuración =====
builder.Services.Configure<MusicLibraryOptions>(
    builder.Configuration.GetSection(MusicLibraryOptions.SectionName));

// ===== MVC =====
builder.Services.AddControllersWithViews();

// ===== SignalR (lo usaremos para el multijugador) =====
builder.Services.AddSignalR();

// ===== Base de datos SQLite =====
var appDataPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(appDataPath);
var dbPath = Path.Combine(appDataPath, "whossinging.db");

builder.Services.AddDbContext<WhosSingingDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// ===== Servicios =====
builder.Services.AddScoped<ISongService, SongService>();
builder.Services.AddScoped<IImportService, ImportService>();
builder.Services.AddSingleton<IAudioService, AudioService>();
builder.Services.AddSingleton<IRoomManager, RoomManager>();
builder.Services.AddSingleton<IGameService, GameService>();

var app = builder.Build();

// ===== Crear la base e importar canciones al iniciar =====
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WhosSingingDbContext>();
    db.Database.EnsureCreated();

    var importService = scope.ServiceProvider.GetRequiredService<IImportService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    int imported = await importService.ImportAllAsync();
    logger.LogInformation("Importación inicial: {Count} canciones nuevas", imported);
}

// ===== Pipeline (orden de las cosas que hace la app) =====
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Esta línea la descomentamos en la Clase 10, cuando creemos el Hub
app.MapHub<GameHub>("/hubs/game");

app.Run();