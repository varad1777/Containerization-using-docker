using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyApp.AverageCalculator; // ✅ Add this line

var builder = Host.CreateApplicationBuilder(args);

// ✅ Register your worker instead of default Worker
builder.Services.AddHostedService<AverageCalculatorWorker>();

var host = builder.Build();
host.Run();
