namespace StdioEndToEnd.StdinEcho;

internal static class Program
{
    public static void Main()
    {
        Console.WriteLine("Stdin echo started. Waiting for input...");

        while (true)
        {
            var line = Console.ReadLine();
            if (line is null)
            {
                Console.WriteLine("Stdin closed (EOF). Exiting.");
                return;
            }

            Console.WriteLine(line);
        }
    }
}
