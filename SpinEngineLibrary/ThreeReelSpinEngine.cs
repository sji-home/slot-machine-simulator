using Common;
using Common.Config;
using Microsoft.Extensions.Options;
using System.Buffers;
using System.Text;

namespace SpinEngineLibrary;

public class ThreeReelSpinEngine : ISpinEngine
{
    private readonly GameConfiguration _gameConfiguration;
    private readonly int _numVisibleWindowColumns;
    private readonly int _numVisibleWindowRows;
    private readonly int _windowLength;
    private readonly int _numReelStrips;
    private readonly int _symbolCount;

    public ThreeReelSpinEngine(IOptions<GameConfiguration> gameConfiguration)
    {
        _gameConfiguration = gameConfiguration.Value ?? throw new ArgumentNullException(nameof(gameConfiguration)); 

        _numVisibleWindowColumns = _gameConfiguration.VisibleArea.Columns;
        _numVisibleWindowRows = _gameConfiguration.VisibleArea.Rows;
        _windowLength = _numVisibleWindowRows * _numVisibleWindowColumns;
        _numReelStrips = _gameConfiguration.ReelStrips.Count;
        _symbolCount = _gameConfiguration.BaseSymbols.Count;
    }

    public SpinResult? Spin(Random rng)
    {
        var visibleWindow = ArrayPool<int>.Shared.Rent(_windowLength);
        var visibleWindowSymbols = ArrayPool<string>.Shared.Rent(_windowLength);
        var currentRowIndexes = new int[_gameConfiguration.ReelStrips.Count];

        try
        {
            LoadVisibleWindow(rng, visibleWindow, visibleWindowSymbols, currentRowIndexes);

            var output = BuildSpinOutput(visibleWindowSymbols);

            var winnings = CheckForWinningCombinations(visibleWindow);

            return new SpinResult(winnings, output);
        }
        finally
        {
            ArrayPool<int>.Shared.Return(visibleWindow);
            ArrayPool<string>.Shared.Return(visibleWindowSymbols, clearArray: true);
        }
    }

    public void LoadVisibleWindow(
        Random rng,
        int[] visibleWindow,
        string[] visibleWindowSymbols,
        int[] currentRowIndexes)
    {
        // Assign random numbers for each reel at the zero offset position.
        for (int i = 0; i < _numReelStrips; i++)
        {
            currentRowIndexes[i] = rng.Next(_gameConfiguration.ReelStrips[i].Length);
        }

        for (int r = 0; r < _numVisibleWindowRows; r++)
        {
            for (int c = 0; c < _numVisibleWindowColumns; c++)
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

                visibleWindow[r * _numVisibleWindowColumns + c] = symbolValue;
                visibleWindowSymbols[r * _numVisibleWindowColumns + c] = symbol;
            }
        } // loop visible window to fill it with symbol values
    }

    public string BuildSpinOutput(string[] visibleWindowSymbols)
    {
        if (!_gameConfiguration.PrintOutput)
        {
            return string.Empty;
        }

        StringBuilder? spinOutputBuilder = null;
        if (_gameConfiguration.PrintOutput)
        {
            spinOutputBuilder = new StringBuilder();
            int maxWidth = 0;
            for (int i = 0; i < _windowLength; i++)
            {
                var symbol = visibleWindowSymbols[i];
                if (symbol.Length > maxWidth)
                    maxWidth = symbol.Length;
            }

            for (int r = 0; r < _numVisibleWindowRows; r++)
            {
                for (int c = 0; c < _numVisibleWindowColumns; c++)
                {
                    spinOutputBuilder.Append(visibleWindowSymbols[r * _numVisibleWindowColumns + c].PadRight(maxWidth + 2));
                }
                spinOutputBuilder.AppendLine();
            }
            spinOutputBuilder.AppendLine();
        }
        return spinOutputBuilder.ToString();
    }

    public int CheckForWinningCombinations(int[] visibleWindow)
    {
        var totalSpinWinningAmount = 0;
        foreach (var payline in _gameConfiguration.PaylineVerticalOffsets)
        {
            // For each payline, obtain the corresponding values from the window using the payline's offsets, encode it and check if
            // the resulting key matches any of the keys in the PayoutByKey array.
            int reelOneCellValue = visibleWindow[payline[0] * _numVisibleWindowColumns + 0];
            int reelTwoCellValue = visibleWindow[payline[1] * _numVisibleWindowColumns + 1];
            int reelThreeCellValue = visibleWindow[payline[2] * _numVisibleWindowColumns + 2];

            var keyFromWindow = ThreeReelPatternEncoder.EncodePaylineKey(
                reelOneCellValue,
                reelTwoCellValue,
                reelThreeCellValue,
                _symbolCount);

            var payout = _gameConfiguration.PayoutByKey[keyFromWindow];
            if (payout > 0)
            {
                totalSpinWinningAmount += payout;
            }
        } // loop paylines

        return totalSpinWinningAmount;
    }
}
