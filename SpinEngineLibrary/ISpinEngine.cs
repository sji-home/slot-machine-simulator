using Common;
using System.Text;

namespace SpinEngineLibrary;

public interface ISpinEngine
{
    public SpinResult? Spin(Random rng);
    public void LoadVisibleWindow(Random rng);
    public StringBuilder? LoadSpinOutput();
    public SpinResult? CheckForWinningCombinations(StringBuilder? spinOutputBuilder);
}
