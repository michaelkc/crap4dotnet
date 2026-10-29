namespace Sample;

public static class Calculator
{
    public static int Absolute(int value)
    {
        if (value < 0)
            return -value;
        return value;
    }
}
