using Common;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SlotMachineSimulator.Config;
using System.Text;

namespace SlotMachineSimulator;

public class SimulatorMain
{
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly GameConfiguration _gameConfiguration;
    private long _totalAmountWon = 0;
    private long _totalAmountWagered = 0;
    private long _numberOfSpins = 5;

    public SimulatorMain(
        IHostApplicationLifetime appLifetime, 
        IOptions<GameConfiguration> gameConfiguration)
    {
        _appLifetime = appLifetime;
        _gameConfiguration = gameConfiguration.Value;
    }

    public async Task RunAsync()
    {
        try
        {
            // Simulate the slot machine game logic here using _gameConfiguration
            Console.WriteLine($"Running {_gameConfiguration.Name} Simulator...");
            Console.WriteLine($"{Environment.NewLine}");

            await Task.Delay(50); // Simulate some processing time

            Parallel.For(0, _numberOfSpins,
                () => (
                    localWinnings: 0,
                    localWagered: 0,
                    rng: new Random(Guid.NewGuid().GetHashCode())
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

            Console.WriteLine();
            Console.WriteLine($"Number of spins: {_numberOfSpins}.");
            Console.WriteLine($"Total amount won: {_totalAmountWon.ToString("C2")} {Environment.NewLine}Total amount wagered: {_totalAmountWagered.ToString("C2")}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred during simulation: {ex.Message}");
        }
        finally
        {
            // Stop the application after the simulation is done
            _appLifetime.StopApplication();
}
    }

    public int Spin(Random rng)
    {
        var numVisibleWindowColumns = _gameConfiguration.VisibleArea.Columns;
        var numVisibleWindowRows = _gameConfiguration.VisibleArea.Rows;
        var numReelStrips = _gameConfiguration.ReelStrips.Count;

        int[,] visibleWindow = new int[numVisibleWindowRows, numVisibleWindowColumns];
        string[,] visibleWindowSymbols = new string[numVisibleWindowRows, numVisibleWindowColumns];
        int[] currentRowIndexes = new int[numReelStrips];

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
                visibleWindow[r, c] = symbolValue;
                visibleWindowSymbols[r, c] = symbol;
            }
        } // loop visible window to fill it with symbol values

        // Display symbols for the visible window for the current spin per project
        // requirements.
        int maxWidth = 0;
        foreach (var symbol in visibleWindowSymbols)
        {
            if (symbol.Length > maxWidth)
                maxWidth = symbol.Length;
        }

        var spinOutputStr = new StringBuilder();
        for (int r = 0; r < numVisibleWindowRows; r++)
        {
            for (int c = 0; c < numVisibleWindowColumns; c++)
            {
                spinOutputStr.Append(visibleWindowSymbols[r, c].PadRight(maxWidth + 2));
            }
            spinOutputStr.AppendLine();
        }
        spinOutputStr.AppendLine();
        Console.Write(spinOutputStr.ToString());

        foreach (var payline in _gameConfiguration.PaylineVerticalOffsets)
        {
            // For each payline, obtain the corresponding values from the window using the payline's offsets, pack it and check if
            // the resulting hashKey matches any of the hashKeys in the PayTableDictionay.
            var reelOneCellValue = visibleWindow[payline[0], 0];
            var reelTwoCellValue = visibleWindow[payline[1], 1];
            var reelThreeCellValue = visibleWindow[payline[2], 2];

            var hashKeyFromWindow = PatternEncoder.EncodePaylineKey(
                reelOneCellValue, 
                reelTwoCellValue, 
                reelThreeCellValue, 
                _gameConfiguration.BaseSymbols.Count);

            if (_gameConfiguration.PayTableDictionay.TryGetValue(hashKeyFromWindow, out int winningAmount))
            {
                return winningAmount;
            }
        } // loop paylines

        return 0; 
    }

}
