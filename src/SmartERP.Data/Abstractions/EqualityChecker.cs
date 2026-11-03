namespace SmartERP.Data.Abstractions;

public class EqualityChecker
{
    private static readonly float epsilonF = 0.0001f;
    private static readonly double epsilonD = 0.0001;

    public static bool CheckEqual(float f1, float f2)
    {
        return Math.Abs(f1 - f2) < epsilonF;
    }

    public static bool CheckEqual(double d1, double d2)
    {
        return Math.Abs(d1 - d2) < epsilonD;
    }
    public static bool CheckEqual(double? d1, double? d2)
    {
        if (d1 is null) return d2 is null;
        if (d2 is null) return d1 is null;

        return Math.Abs(d1.Value - d2.Value) < epsilonD;
    }

    public static bool CheckNotEqual(float f1, float f2)
    {
        return f1 <= f2 - epsilonF || f1 >= f2 + epsilonF;
    }

    public static bool CheckNotEqual(double d1, double d2)
    {
        return d1 <= d2 - epsilonD || d1 >= d2 + epsilonD;
    }


    public static bool CheckNotEqual(double? d1, double? d2)
    {
        if (d1 is null) return d1 is not null;
        if (d2 is null) return d1 is not null;

        return d1.Value <= d2.Value - epsilonD || d1.Value >= d2.Value + epsilonD;
    }
}