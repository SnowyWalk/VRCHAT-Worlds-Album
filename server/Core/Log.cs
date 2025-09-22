namespace server.Core;

public static class Log
{
    public static void Info(object? e) => Info(e != null ? e.ToString()! : "null");
    public static void Info(string msg)
    {
        Print(msg, ConsoleColor.Green);
    }
    
    public static void Warn(object? e) => Warn(e != null ? e.ToString()! : "null");
    public static void Warn(string msg)
    {
        Print(msg, ConsoleColor.Yellow);
    }
    
    public static void Error(object? e) => Error(e != null ? e.ToString()! : "null");
    public static void Error(string msg)
    {
        Print(msg, ConsoleColor.Red);
    }
    
    public static void Debug(object? e) => Debug(e != null ? e.ToString()! : "null");
    public static void Debug(string msg)
    {
        Print(msg, ConsoleColor.Cyan);
    }

    private static void Print(string msg, ConsoleColor color)
    {
        var defaultColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(msg);
        Console.ForegroundColor = defaultColor;
    }
}