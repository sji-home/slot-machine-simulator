using Common;

namespace SlotMachineSimulator.Config;

public sealed class GameConfiguration
{
    public string Name { get; set; }
    public int BetInfo { get; set; }
    public bool PrintOutput { get; set; }
    public int NumberOfSpins { get; set; }
    public VisibleArea VisibleArea { get; set; }

    public List<List<int>> PaylineVerticalOffsets { get; set; } = new();

    public List<string> BaseSymbols { get; set; } = [];
    public Dictionary<string, int> BaseSymbolDictionary => InitializeBaseSymbolDictionary();
    public List<string[]> ReelStrips { get; set; }
    public Dictionary<int, int> PayTableDictionay => InitializePayTableDictionary();
    public List<PayItem> BasePayTable { get; set; } = [];


    public Dictionary<int, int> InitializePayTableDictionary()
    {
        string[] matchStringArray = null;
        int[] matchArray = null;
        var symbolValue = -1;
        int hashKey = -1;

        var payTableDictionay = new Dictionary<int, int>();

        foreach (var payItem in this.BasePayTable)
        {
            matchStringArray = payItem.ExactMatch.Split(',');
            matchArray = new int[matchStringArray.Length];

            for (int i = 0; i < matchStringArray.Length; i++)
            {
                var symbol = matchStringArray[i];
                if (this.BaseSymbolDictionary.TryGetValue(symbol, out symbolValue))
                {
                    matchArray[i] = symbolValue;
                }
                else
                {
                    throw new Exception($"Symbol {symbol} not found in BaseSymbolDictionary");
                }
            }

            hashKey = PatternEncoder.EncodePaylineKey(matchArray, this.BaseSymbols.Count);
            payTableDictionay.Add(hashKey, payItem.Amount);
        } // loop BasePayTable

        return payTableDictionay;
    }

    public HashSet<int> InitializePaylineHashSet()
    {
        var symbolCount = this.BaseSymbols.Count;
        var payLineRowCount = this.PaylineVerticalOffsets.Count;

        var paylineHashSet = new HashSet<int>();

        for (int r = 0; r < payLineRowCount; r++)
        {
            var c1 = this.PaylineVerticalOffsets[r][0];
            var c2 = this.PaylineVerticalOffsets[r][1];
            var c3 = this.PaylineVerticalOffsets[r][2];

            paylineHashSet.Add(PatternEncoder.EncodePaylineKey(c1, c2, c3, symbolCount));
        }
        return paylineHashSet;
    }

    public Dictionary<string, int> InitializeBaseSymbolDictionary()
    {
        var baseSymbolDictionary = new Dictionary<string, int>();
        int i = 0;
        foreach (var symbol in this.BaseSymbols)
        {
            baseSymbolDictionary.Add(symbol, i++);
        }
        return baseSymbolDictionary;
    }



}
