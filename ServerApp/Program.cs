using System;

namespace ServerApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "TCP Chat Server";

            Server server = new Server();
            server.Start();

            Console.WriteLine("\n[SERVER] running! Press Enter to shutdown");
            Console.ReadLine();

            server.Stop();

            Console.ReadLine();
        }
    }
}
