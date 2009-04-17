using System;

namespace kaorudono
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (Kaoru game = new Kaoru())
            {
                game.Run();
            }
        }
    }
}

