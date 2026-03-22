namespace Common;

/// <summary>
/// This static class provides methods to generate a unique integer key for a given
/// combination of symbols as integer values using symbolCount as the base.
/// The incoming value (a,b,c) is the base of symbolCount and it is converted into a unique
/// value in base 10.
/// 
/// Assumptions: 
/// 1. Only 3 symbols are considered.
/// 2. The symbol values must all be less than symbolCount.
/// </summary>
public static class PatternEncoder
{
    public static int EncodePaylineKey(int a, int b, int c, int symbolCount)
    {
        return (a * symbolCount + b) * symbolCount + c;
    }

    public static int EncodePaylineKey(int[] matchArray, int symbolCount)
    {
        return (matchArray[0] * symbolCount + matchArray[1]) * symbolCount + matchArray[2];
    }

}
