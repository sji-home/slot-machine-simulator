using Common;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SlotMachineSimulator.Config;

namespace SlotMachineSimulator;

public class SimulatorMain
{
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly GameConfiguration _gameConfiguration;
    private long _totalAmountWon = 0;
    private long _totalAmountWagered = 0;
    private long _numberOfSpins = 100000;

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
        int[] currentRowIndexes = new int[numReelStrips];

        // Generate random numbers for each reel at the zero offset position.
        for (int i = 0; i < numReelStrips; i++)
        {
            currentRowIndexes[i] = rng.Next(_gameConfiguration.ReelStrips[i].Length);
        }

        //test  matches [0,1,2] for Jacks
        //randomNumbers.Add(4);
        //randomNumbers.Add(3);
        //randomNumbers.Add(3);
        //test

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
            }
        } // loop visible window

        foreach (var payline in _gameConfiguration.PaylineVerticalOffsets)
        {
            // For each payline, obtain the corresponding values from the window using the payline's offsets, pack it and check if
            // the resulting hashKey matches any of the hashKeys in the PayTableDictionay.
            var reelOneCellValue = visibleWindow[payline[0], 0];
            var reelTwoCellValue = visibleWindow[payline[1], 1];
            var reelThreeCellValue = visibleWindow[payline[2], 2];

            var hashKeyFromWindow = PatternEncoder.Pack(
                reelOneCellValue, 
                reelTwoCellValue, 
                reelThreeCellValue, 
                _gameConfiguration.BaseSymbols.Count);

            if (_gameConfiguration.PayTableDictionay.TryGetValue(hashKeyFromWindow, out int winningAmount))
            {
                return winningAmount;
            }
        } // loop paylines

        return 0; // no win
    }

}
