using Common.Config;
using Microsoft.Extensions.Options;
using SpinEngineLibrary;
using System.Diagnostics;

namespace SlotMachineSimulator;

public class SimulatorMain
{
    private readonly GameConfiguration _gameConfiguration;
    private long _totalAmountWon;
    private long _totalAmountWagered;
    private static int _seed = Environment.TickCount;
    private readonly ISpinEngine _spinEngine;

    public SimulatorMain(IOptions<GameConfiguration> gameConfiguration,
                         ISpinEngine spinEngine)
    {
        _gameConfiguration = gameConfiguration.Value ?? throw new ArgumentNullException(nameof(gameConfiguration)); 
        _spinEngine = spinEngine ?? throw new ArgumentNullException(nameof(spinEngine));
    }

    public void RunSimulation()
    {
        try
        {
            _totalAmountWon = 0;
            _totalAmountWagered = 0;

            Console.WriteLine($"Running {_gameConfiguration.Name} Simulator...");
            Console.WriteLine();

            string[] outputs = [];
            if (_gameConfiguration.PrintOutput)
            {
                outputs = new string[_gameConfiguration.NumberOfSpins];
            }

            var stopWatch = Stopwatch.StartNew();

            Parallel.For(0, _gameConfiguration.NumberOfSpins,
                () => (
                    localWinnings: 0,
                    localWagered: 0,
                    rng: new Random(Interlocked.Increment(ref _seed))
                ),
                (i, state, local) =>
                {
                    int wager = _gameConfiguration.BetInfo; 
                    var spinResult = _spinEngine.Spin(local.rng);

                    if (spinResult is null)
                    {
                        throw new InvalidOperationException("Spin result cannot be null.");
                    }

                    if (_gameConfiguration.PrintOutput)
                    {
                        outputs[i] = spinResult.Output;
                    }                    
                    local.localWinnings += spinResult.Winnings;
                    local.localWagered += wager;

                    return local;
                },
                local =>
                {
                    Interlocked.Add(ref _totalAmountWon, local.localWinnings);
                    Interlocked.Add(ref _totalAmountWagered, local.localWagered);
                });

            stopWatch.Stop();
            var ts = stopWatch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            ts.Hours, ts.Minutes, ts.Seconds,
            ts.Milliseconds / 10);

            if (_gameConfiguration.PrintOutput)
            {
                for (int i = 0; i < _gameConfiguration.NumberOfSpins; i++)
                {
                    Console.Write(outputs[i]);
                }
            }

            Console.WriteLine($"Number of spins: {_gameConfiguration.NumberOfSpins}");
            Console.WriteLine($"Total amount won: {_totalAmountWon.ToString("C2")} {Environment.NewLine}Total amount wagered: {_totalAmountWagered.ToString("C2")}");

            Console.WriteLine();
            Console.WriteLine("RunTime " + elapsedTime);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred during simulation: {ex.Message}");
        }
    }
}
