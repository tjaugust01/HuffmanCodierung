namespace HuffmanCodierung;

public static class Terminal
{
    public static void info(string msg)
    {
        WriteColor(msg, ConsoleColor.Cyan);
    }

    public static void error(string msg)
    {
        WriteColor(msg, ConsoleColor.Red);
    }

    public static void success(string msg)
    {
        WriteColor(msg, ConsoleColor.Green);
    }

    private static void WriteColor(string msg, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(msg);
        Console.ResetColor();
    }

    public static string Prompt(string question)
    {
        string input;
        do
        {
            Console.WriteLine($"{question}");
            input = Console.ReadLine()?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(input))
            {
                Console.WriteLine("Fehler: Das Feld darf nicht leer sein!");
            }
        } while (string.IsNullOrEmpty(input));

        return input;
    }
}