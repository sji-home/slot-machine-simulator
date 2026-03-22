namespace Common;

public static class PatternEncoder
{

    public static int PackRow(int[,] window, int row, int symbolCount)
    {
        return (window[row, 0] * symbolCount + window[row, 1]) * symbolCount
             + window[row, 2];
    }

    public static int Pack(int a, int b, int c, int symbolCount)
    {
        return (a * symbolCount + b) * symbolCount + c;
    }

    public static int Pack(int[] matchArray, int symbolCount)
    {
        // error handling 
        return (matchArray[0] * symbolCount + matchArray[1]) * symbolCount + matchArray[2];
    }

}
