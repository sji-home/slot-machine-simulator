namespace Common.Config;

public sealed class GameConfiguration
{
    public string Name { get; set; } = string.Empty;
    public int BetInfo { get; set; }
    public bool PrintOutput { get; set; }
    public int NumberOfSpins { get; set; }
    public VisibleArea VisibleArea { get; set; }

    public List<List<int>> PaylineVerticalOffsets { get; set; } = new();
    public List<string> BaseSymbols { get; set; } = new();
    public List<string[]> ReelStrips { get; set; } = new();
    public List<PayItem> BasePayTable { get; set; } = new();

    // Derived data, these are computed once.
    public Dictionary<string, int> BaseSymbolDictionary { get; private set; } = new();
    //public Dictionary<int, int> PayTableDictionary { get; private set; } = new();

    public int[] PayoutByKey { get; private set; } = [];

    public void InitializeDerivedData()
    {
        BaseSymbolDictionary = InitializeBaseSymbolDictionary();
        //PayTableDictionary = InitializePayTableDictionary();
        PayoutByKey = InitializePayoutKey();
    }

    private int[] InitializePayoutKey()
    {
        // Because with base 6 and 3 positions, the total number of combinations is only: 6^3 = 216
        int symbolCount = BaseSymbols.Count;
        int combinations = symbolCount * symbolCount * symbolCount;

        var payoutByKey = new int[combinations];

        foreach (var payItem in BasePayTable)
        {
            string[] matchStringArray = payItem.ExactMatch.Split(',');

            if (matchStringArray.Length != 3)
            {
                throw new InvalidOperationException(
                    $"Pay item '{payItem.ExactMatch}' must contain exactly 3 symbols.");
            }

            int[] matchArray = new int[3];

            for (int i = 0; i < 3; i++)
            {
                string symbol = matchStringArray[i].Trim();

                if (!BaseSymbolDictionary.TryGetValue(symbol, out int symbolValue))
                {
                    throw new InvalidOperationException(
                        $"Symbol '{symbol}' not found in BaseSymbolDictionary.");
                }

                matchArray[i] = symbolValue;
            }

            int key = ThreeReelPatternEncoder.EncodePaylineKey(matchArray, symbolCount);
            payoutByKey[key] = payItem.Amount;
        }

        return payoutByKey;
    }

    //private Dictionary<int, int> InitializePayTableDictionary()
    //{
    //    string[] matchStringArray = null;
    //    int[] matchArray = null;
    //    var symbolValue = -1;
    //    int key = -1;

    //    var payTableDictionay = new Dictionary<int, int>();

    //    foreach (var payItem in this.BasePayTable)
    //    {
    //        matchStringArray = payItem.ExactMatch.Split(',');
    //        matchArray = new int[matchStringArray.Length];

    //        for (int i = 0; i < matchStringArray.Length; i++)
    //        {
    //            var symbol = matchStringArray[i].Trim();
    //            if (this.BaseSymbolDictionary.TryGetValue(symbol, out symbolValue))
    //            {
    //                matchArray[i] = symbolValue;
    //            }
    //            else
    //            {
    //                throw new InvalidOperationException($"Symbol {symbol} not found in BaseSymbolDictionary");
    //            }
    //        }

    //        key = PatternEncoder.EncodePaylineKey(matchArray, this.BaseSymbols.Count);
    //        payTableDictionay.Add(key, payItem.Amount);
    //    } // loop BasePayTable

    //    return payTableDictionay;
    //}

    private Dictionary<string, int> InitializeBaseSymbolDictionary()
    {
        var baseSymbolDictionary = new Dictionary<string, int>(this.BaseSymbols.Count);

        for (int i = 0; i < BaseSymbols.Count; i++)
        {
            string symbol = BaseSymbols[i];

            if (baseSymbolDictionary.ContainsKey(symbol))
            {
                throw new InvalidOperationException(
                    $"Duplicate symbol '{symbol}' found in BaseSymbols.");
            }

            baseSymbolDictionary.Add(symbol, i);
        }

        return baseSymbolDictionary;
    }

}
