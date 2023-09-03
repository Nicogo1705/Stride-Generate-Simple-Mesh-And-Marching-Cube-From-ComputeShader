using Stride.Engine;

namespace ShaderTest
{
    class ShaderTestApp
    {
        static void Main(string[] args)
        {
            using (var game = new Game())
            {
                game.Run();
            }
        }
    }
}
