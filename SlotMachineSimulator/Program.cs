using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SlotMachineSimulator;
using SlotMachineSimulator.Config;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.Sources.Clear();

IHostEnvironment env = builder.Environment;

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// register configuration and services
builder.Services.Configure<GameConfiguration>(
    builder.Configuration.GetSection(key: nameof(GameConfiguration)));

builder.Services.AddSingleton<SimulatorMain>();

using IHost host = builder.Build();

// Resolve and run the main class
var simulatorMain = host.Services.GetRequiredService<SimulatorMain>();
simulatorMain.RunSimulation();

Console.ReadKey();