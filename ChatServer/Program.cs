namespace ChatServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Server server = new Server(5000);
            server.Start();
        }
    }
}