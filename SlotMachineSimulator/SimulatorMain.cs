using Common;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SlotMachineSimulator.Config;
using System.Buffers;
using System.Diagnostics;
using System.Text;

namespace SlotMachineSimulator;

public class SimulatorMain
{
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly GameConfiguration _gameConfiguration;
    private long _totalAmountWon;
    private long _totalAmountWagered;
    private long _numberOfSpins;
    private static int _seed = Environment.TickCount;
    private static readonly object _lock = new();

    public SimulatorMain(
        IHostApplicationLifetime appLifetime, 
        IOptions<GameConfiguration> gameConfiguration)
    {
        _appLifetime = appLifetime;
        _gameConfiguration = gameConfiguration.Value;
    }

    public void RunSimulation()
    {
        try
        {
            _totalAmountWon = 0;
            _totalAmountWagered = 0;
            _numberOfSpins = 3;

            Console.WriteLine($"Running {_gameConfiguration.Name} Simulator...");
            Console.WriteLine($"{Environment.NewLine}");

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            Parallel.For(0, _numberOfSpins,
                () => (
                    localWinnings: 0,
                    localWagered: 0,
                    rng: new Random(Interlocked.Increment(ref _seed))
                ),
                (i, state, local) =>
                {
                    int wager = _gameConfiguration.BetInfo; 
                    int winnings = Spin(local.rng);

                    local.localWinnings += winnings;
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

            Console.WriteLine("RunTime " + elapsedTime);

            Console.WriteLine();
            Console.WriteLine($"Number of spins: {_numberOfSpins}");
            Console.WriteLine($"Total amount won: {_totalAmountWon.ToString("C2")} {Environment.NewLine}Total amount wagered: {_totalAmountWagered.ToString("C2")}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred during simulation: {ex.Message}");
        }
        finally
        {
            _appLifetime.StopApplication();
}
    }

    public int Spin(Random rng)
    {
        var numVisibleWindowColumns = _gameConfiguration.VisibleArea.Columns;
        var numVisibleWindowRows = _gameConfiguration.VisibleArea.Rows;
        var numReelStrips = _gameConfiguration.ReelStrips.Count;

        int windowLength = numVisibleWindowRows * numVisibleWindowColumns;
        int[] visibleWindow = ArrayPool<int>.Shared.Rent(windowLength);
        string[] visibleWindowSymbols = ArrayPool<string>.Shared.Rent(windowLength);

        int[] currentRowIndexes = new int[numReelStrips];

        try
        {
            // Generate random numbers for each reel at the zero offset position.
            for (int i = 0; i < numReelStrips; i++)
            {
                currentRowIndexes[i] = rng.Next(_gameConfiguration.ReelStrips[i].Length);
            }

            for (int r = 0; r < numVisibleWindowRows; r++)
            {
                for (int c = 0; c < numVisibleWindowColumns; c++)
                {
                    if (r != 0)
                    {
                        var reelIx = currentRowIndexes[c] + 1;
                        if (reelIx >= _gameConfiguration.ReelStrips[c].Length)
                        {
                            reelIx = 0;
                        }
                        currentRowIndexes[c] = reelIx;
                    }

                    var symbol = _gameConfiguration.ReelStrips[c][currentRowIndexes[c]];
                    var symbolValue = _gameConfiguration.BaseSymbolDictionary[symbol];

                    visibleWindow[r * numVisibleWindowColumns + c] = symbolValue;
                    visibleWindowSymbols[r * numVisibleWindowColumns + c] = symbol;
                }
            } // loop visible window to fill it with symbol values

            // Display symbols for the visible window for the current spin per project
            // requirements.
            int maxWidth = 0;
            foreach (var symbol in visibleWindowSymbols)
            {
                if (symbol is not null && symbol.Length > maxWidth)
                    maxWidth = symbol.Length;
            }

            var spinOutputStr = new StringBuilder();
            for (int r = 0; r < numVisibleWindowRows; r++)
            {
                for (int c = 0; c < numVisibleWindowColumns; c++)
                {
                    spinOutputStr.Append(visibleWindowSymbols[r * numVisibleWindowColumns + c].PadRight(maxWidth + 2));
                }
                spinOutputStr.AppendLine();
            }
            spinOutputStr.AppendLine();

            lock (_lock)
            {
                Console.Write(spinOutputStr.ToString());
            }

            var totalSpinWinningAmount = 0;
            foreach (var payline in _gameConfiguration.PaylineVerticalOffsets)
            {
                // For each payline, obtain the corresponding values from the window using the payline's offsets, encode it and check if
                // the resulting hashKey matches any of the hashKeys in the PayTableDictionay.
                int reelOneCellValue = visibleWindow[payline[0] * numVisibleWindowColumns + 0];
                int reelTwoCellValue = visibleWindow[payline[1] * numVisibleWindowColumns + 1];
                int reelThreeCellValue = visibleWindow[payline[2] * numVisibleWindowColumns + 2];

                var hashKeyFromWindow = PatternEncoder.EncodePaylineKey(
                    reelOneCellValue,
                    reelTwoCellValue,
                    reelThreeCellValue,
                    _gameConfiguration.BaseSymbols.Count);

                if (_gameConfiguration.PayTableDictionay.TryGetValue(hashKeyFromWindow, out int spinWinningAmount))
                {
                    totalSpinWinningAmount += spinWinningAmount;
                }
            } // loop paylines

            return totalSpinWinningAmount;

        }
        finally
        {
            ArrayPool<int>.Shared.Return(visibleWindow);
            ArrayPool<string>.Shared.Return(visibleWindowSymbols, clearArray: true);
        }
    }

}
