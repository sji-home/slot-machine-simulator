using Common;
using Common.Config;
using Microsoft.Extensions.Options;
using System.Buffers;
using System.Text;

namespace SpinEngineLibrary;

public class ThreeReelSpinEngine : ISpinEngine
{
    private readonly GameConfiguration _gameConfiguration;
    private int[] _visibleWindow = [];
    private string[] _visibleWindowSymbols = [];
    private int[] _currentRowIndexes = [];
    private int _numVisibleWindowColumns;
    private int _numVisibleWindowRows;
    private int _windowLength;

    public ThreeReelSpinEngine(IOptions<GameConfiguration> gameConfiguration)
    {
        _gameConfiguration = gameConfiguration.Value;

        _numVisibleWindowColumns = _gameConfiguration.VisibleArea.Columns;
        _numVisibleWindowRows = _gameConfiguration.VisibleArea.Rows;
        _windowLength = _numVisibleWindowRows * _numVisibleWindowColumns;

        _visibleWindow = ArrayPool<int>.Shared.Rent(_windowLength);
        _visibleWindowSymbols = ArrayPool<string>.Shared.Rent(_windowLength);
        _currentRowIndexes = new int[_gameConfiguration.ReelStrips.Count];
    }

    public SpinResult? Spin(Random rng)
    {
        try
        {
            LoadVisibleWindow(rng);

            var spinResult = CheckForWinningCombinations(LoadSpinOutput());

            return spinResult;
        }
        finally
        {
            ArrayPool<int>.Shared.Return(_visibleWindow);
            ArrayPool<string>.Shared.Return(_visibleWindowSymbols, clearArray: true);
        }
    }

    public void LoadVisibleWindow(Random rng)
    {
        // Assign random numbers for each reel at the zero offset position.
        var numReelStrips = _gameConfiguration.ReelStrips.Count;

        for (int i = 0; i < numReelStrips; i++)
        {
            _currentRowIndexes[i] = rng.Next(_gameConfiguration.ReelStrips[i].Length);
        }

        for (int r = 0; r < _numVisibleWindowRows; r++)
        {
            for (int c = 0; c < _numVisibleWindowColumns; c++)
            {
                if (r != 0)
                {
                    var reelIx = _currentRowIndexes[c] + 1;
                    if (reelIx >= _gameConfiguration.ReelStrips[c].Length)
                    {
                        reelIx = 0;
                    }
                    _currentRowIndexes[c] = reelIx;
                }

                var symbol = _gameConfiguration.ReelStrips[c][_currentRowIndexes[c]];
                var symbolValue = _gameConfiguration.BaseSymbolDictionary[symbol];

                _visibleWindow[r * _numVisibleWindowColumns + c] = symbolValue;
                _visibleWindowSymbols[r * _numVisibleWindowColumns + c] = symbol;
            }
        } // loop visible window to fill it with symbol values
    }

    public StringBuilder? LoadSpinOutput()
    {
        StringBuilder? spinOutputBuilder = null;
        if (_gameConfiguration.PrintOutput)
        {
            spinOutputBuilder = new StringBuilder();
            int maxWidth = 0;
            for (int i = 0; i < _windowLength; i++)
            {
                var symbol = _visibleWindowSymbols[i];
                if (symbol.Length > maxWidth)
                    maxWidth = symbol.Length;
            }

            for (int r = 0; r < _numVisibleWindowRows; r++)
            {
                for (int c = 0; c < _numVisibleWindowColumns; c++)
                {
                    spinOutputBuilder.Append(_visibleWindowSymbols[r * _numVisibleWindowColumns + c].PadRight(maxWidth + 2));
                }
                spinOutputBuilder.AppendLine();
            }
            spinOutputBuilder.AppendLine();
        }
        return spinOutputBuilder;
    }

    public SpinResult? CheckForWinningCombinations(StringBuilder? spinOutputBuilder)
    {
        var totalSpinWinningAmount = 0;
        foreach (var payline in _gameConfiguration.PaylineVerticalOffsets)
        {
            // For each payline, obtain the corresponding values from the window using the payline's offsets, encode it and check if
            // the resulting key matches any of the keys in the PayoutByKey array.
            int reelOneCellValue = _visibleWindow[payline[0] * _numVisibleWindowColumns + 0];
            int reelTwoCellValue = _visibleWindow[payline[1] * _numVisibleWindowColumns + 1];
            int reelThreeCellValue = _visibleWindow[payline[2] * _numVisibleWindowColumns + 2];

            var keyFromWindow = ThreeReelPatternEncoder.EncodePaylineKey(
                reelOneCellValue,
                reelTwoCellValue,
                reelThreeCellValue,
                _gameConfiguration.BaseSymbols.Count);

            var payout = _gameConfiguration.PayoutByKey[keyFromWindow];
            if (payout > 0)
            {
                totalSpinWinningAmount += payout;
            }
        } // loop paylines

        return new SpinResult
        {
            Winnings = totalSpinWinningAmount,
            Output = spinOutputBuilder?.ToString() ?? string.Empty
        };
    }
}
