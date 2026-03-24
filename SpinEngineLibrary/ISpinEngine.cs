using Common;
using System.Text;

namespace SpinEngineLibrary;

public interface ISpinEngine
{
    public SpinResult? Spin(Random rng);
    public void LoadVisibleWindow(
        Random rng, 
        int[] visibleWindow, 
        string[] visibleWindowSymbols, 
        int[] currentRowIndexes);
    public string BuildSpinOutput(string[] visibleWindowSymbols);
    public int CheckForWinningCombinations(int[] visibleWindow);
}
