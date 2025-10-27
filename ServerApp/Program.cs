using System;

namespace ServerApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "TCP Chat Server";

            Server server = new Server();
            server.Start(5000); 

            Console.ReadLine();
        }
    }
}
