// Compiling engine into single file .EXE:
// dotnet publish SharpKnight.Longsword -c Release

using SharpKnight.Core;
using SharpKnight.Engines;
using SharpKnight.Players;

namespace SharpKnight
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // Create the backend engine
            var backend = new LongswordEngine();

            // Wrap it in an EnginePlayer to handle UCI
            var player = new EnginePlayer(backend);

            // Enter the UCI command loop
            UciHandler.UciLoop(player);
        }
    }
}
