using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Common;
using Common.Config;
using SpinEngineLibrary;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.Sources.Clear();

IHostEnvironment env = builder.Environment;

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

// register configuration and services
builder.Services.Configure<GameConfiguration>(
    builder.Configuration.GetSection(key: nameof(GameConfiguration)));

builder.Services.PostConfigure<GameConfiguration>(config =>
{
    config.InitializeDerivedData();
});

builder.Services.AddSingleton<ISpinEngine, ThreeReelSpinEngine>();
builder.Services.AddSingleton<SimulatorMain>();

using IHost host = builder.Build();

// Resolve and run the main class
var simulatorMain = host.Services.GetRequiredService<SimulatorMain>();

string? input;

do
{
    Console.WriteLine();
    simulatorMain.RunSimulation();
    Console.WriteLine();

    while (true)
    {
        Console.Write("Spin again? [y/yes, n/no]: ");
        input = Console.ReadLine();

        if (IsYes(input))
            break; 

        if (IsNo(input))
            return; 

        Console.WriteLine("Invalid input. Please enter 'y', 'yes', 'n', or 'no'.");
    }

} while (true);

static bool IsYes(string? input) =>
    input?.Trim().Equals("y", StringComparison.OrdinalIgnoreCase) == true ||
    input?.Trim().Equals("yes", StringComparison.OrdinalIgnoreCase) == true;

static bool IsNo(string? input)
{
    if (input is null) return false;

    var s = input.Trim();
    return s.Equals("n", StringComparison.OrdinalIgnoreCase)
        || s.Equals("no", StringComparison.OrdinalIgnoreCase);
}
