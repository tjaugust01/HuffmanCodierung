namespace HuffmanCodierung;

internal class Program
{
    static void Main(string[] args)
    {
        try
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding = System.Text.Encoding.UTF8;
        }
        catch
        {
            // Ignorieren, falls Ein-/Ausgabe umgeleitet ist
        }

        var app = new App();
        app.Run(args);
    }
}