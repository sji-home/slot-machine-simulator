namespace Common;

public static class PatternEncoder
{
    public static int Pack(int a, int b, int c, int symbolCount)
    {
        return (a * symbolCount + b) * symbolCount + c;
    }

    public static int Pack(int[] matchArray, int symbolCount)
    {
        return (matchArray[0] * symbolCount + matchArray[1]) * symbolCount + matchArray[2];
    }

}
