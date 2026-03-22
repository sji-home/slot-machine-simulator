using Common;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SlotMachineSimulator.Config;

namespace SlotMachineSimulator;

public class SimulatorMain
{
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly GameConfiguration _gameConfiguration;
    private Random _random = new();
    private int[,] _visibleWindow;
    private int _totalAmountWon = 0;
    private int _totalAmountWagered = 0;
    private int _numberOfSpins = 100000;

    public SimulatorMain(
        IHostApplicationLifetime appLifetime, 
        IOptions<GameConfiguration> gameConfiguration)
    {
        _appLifetime = appLifetime;
        _gameConfiguration = gameConfiguration.Value;
        _visibleWindow = new int[_gameConfiguration.VisibleArea.Rows, _gameConfiguration.VisibleArea.Columns];
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
        var randomNumbers = new List<int>();
        var randomIndex = 0;

        // Generate random numbers for each reel at the zero offset position.
        for (int i = 0; i < _gameConfiguration.ReelStrips.Count; i++)
        {
            randomIndex = rng.Next(_gameConfiguration.ReelStrips[i].Length);
            randomNumbers.Add(randomIndex);
        }

        //test  matches [0,1,2] for Jacks
        //randomNumbers.Add(4);
        //randomNumbers.Add(3);
        //randomNumbers.Add(3);
        //test

        var numVisibleWindowColumns = _gameConfiguration.VisibleArea.Columns;
        var numVisibleWindowRows = _gameConfiguration.VisibleArea.Rows;

        var symbol = string.Empty;
        var symbolValue = -1;
        var reelIx = -1;

        // Populate the visible window array
        var currentRowIndexes = new List<int>();
        currentRowIndexes.AddRange(randomNumbers);

        // loop rows
        for (int r = 0; r < numVisibleWindowRows; r++)
        {
            // loop columns
            for (int c = 0; c < numVisibleWindowColumns; c++)
            {
                if (r == 0)
                {
                    symbol = _gameConfiguration.ReelStrips[c][currentRowIndexes[c]];
                    symbolValue = _gameConfiguration.BaseSymbolDictionary[symbol];
                    _visibleWindow[r, c] = symbolValue;
                }
                else
                {
                    reelIx = currentRowIndexes[c] + 1;
                    if (reelIx >= _gameConfiguration.ReelStrips[c].Length)
                    {
                        reelIx = 0;
                    }
                    currentRowIndexes[c] = reelIx;
                    symbol = _gameConfiguration.ReelStrips[c][currentRowIndexes[c]];
                    symbolValue = _gameConfiguration.BaseSymbolDictionary[symbol];
                    _visibleWindow[r, c] = symbolValue;
                }
            }
        } // loop window

        var hashKeyFromWindow = 0;
        var reelOneCellValue = 0;
        var reelTwoCellValue = 0;
        var reelThreeCellValue = 0;
        int winningAmount = 0;
        var foundMatch = false;

        foreach (var payline in _gameConfiguration.PaylineVerticalOffsets)
        {
            // For each payline, obtain the corresponding values from the window using the payline's offsets, pack it and check if
            // the resulting hashKey matches any of the hashKeys in the PayTableDictionay.
            reelOneCellValue = _visibleWindow[payline[0], 0];
            reelTwoCellValue = _visibleWindow[payline[1], 1];
            reelThreeCellValue = _visibleWindow[payline[2], 2];

            hashKeyFromWindow = PatternEncoder.Pack(
                reelOneCellValue, 
                reelTwoCellValue, 
                reelThreeCellValue, 
                _gameConfiguration.BaseSymbols.Count);

            if (_gameConfiguration.PayTableDictionay.ContainsKey(hashKeyFromWindow))
            {
                foundMatch = true;
                winningAmount = _gameConfiguration.PayTableDictionay[hashKeyFromWindow];
                //Console.WriteLine($"matched");
                break;
            }
        } // loop paylines

        if (!foundMatch)
        {
            //Console.WriteLine($"no match");
        }
        return winningAmount;
    }

}
