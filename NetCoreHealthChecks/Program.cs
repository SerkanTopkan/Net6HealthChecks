using HealthChecks.UI.Client;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.ServiceProcess;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IMyCustomService, MyCustomService>();
builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),name:"Sql Server Status", tags: new string[] { "Sql Server" })
    .AddRedis(
    redisConnectionString: builder.Configuration.GetConnectionString("Redis"),
    name: "Redis Status",
    tags: new string[] { "Docker Redis" })
    .AddDiskStorageHealthCheck(s => s.AddDrive("C:\\", 307200),name:"Disk Status") // 307200 MB (1 GB) free minimum
  .AddProcessAllocatedMemoryHealthCheck(6144, name:"Ram Status") // 6144 MB max allocated memory
  .AddProcessHealthCheck("System", p => p.Length > 0,name: "System Process Status") // check if process is running
  .AddWindowsServiceHealthCheck("MySQL-Server", s => s.Status == ServiceControllerStatus.Running,name:"MySql Status")
  .AddUrlGroup(new Uri("https://finansmix.com/altin-fiyatlari"),"Finansmix Data Status",timeout:TimeSpan.FromSeconds(30)).AddCheck<MyCustomCheck>(name:"Custom Service Status");
  
  


builder.Services.AddHealthChecksUI(setup =>
{
    setup.AddHealthCheckEndpoint("Local Point", "https://localhost:7154/health");
    setup.SetEvaluationTimeInSeconds(5);
    setup.SetApiMaxActiveRequests(2);

}).AddInMemoryStorage();



var app = builder.Build();



// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.UseHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { 
    Predicate = _ =>true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse    

});


app.UseHealthChecksUI(config => {
    config.UIPath = "/healthui";
    config.PageTitle="Status";
    
    });

app.Run();

public class MyCustomCheck : IHealthCheck
{
    private readonly IMyCustomService _customService;

    public MyCustomCheck(IMyCustomService customService)
    {
        _customService = customService;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var result = _customService.IsHealthy() ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy();
        return Task.FromResult(result);
    }

}

public interface IMyCustomService
{

    public bool IsHealthy();

}

public class MyCustomService : IMyCustomService
{

    public bool IsHealthy()
    {
        return new Random().NextDouble() > 0.5;
    }

}