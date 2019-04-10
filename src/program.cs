using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

using Console = SadConsole.Console;

using LM.MyRNG;
using LM.MyRNG.Generators;

namespace LM {
    class Program {
        public const int Width = 40;
        public const int Height = 11;

        static void Main(string[] args) {
            SadConsole.Game.Create("IBM.font", Width, Height);
            
            SadConsole.Game.OnInitialize = Init;
            SadConsole.Game.OnUpdate = Update;
                        
            SadConsole.Game.Instance.Run();
            //Code here will not run until the game window closes.
            SadConsole.Game.Instance.Dispose();
        }
        
        private static void Update(GameTime time) {
            if(SadConsole.Global.KeyboardState.IsKeyReleased(Microsoft.Xna.Framework.Input.Keys.F5)) {
                SadConsole.Settings.ToggleFullScreen();
            }
        }

        private static void Init() {
            var names = new List<string> {
                "Carlos"
                , "Charlotte"
                , "Marley"
            };
            var namegen = new NameGenerator.NameGenerator(1, names, 253);

            var rng = new Generators.House(true);

            //Any custom loading and prep. We will use a sample console for now.
            Console startingConsole = new Console(Width, Height);
            startingConsole.FillWithRandomGarbage();
            startingConsole.Fill(new Rectangle(3, 3, 27, 5), null, Color.DarkSlateGray, 0, SpriteEffects.None);
            startingConsole.Print(6, 5, rng.GetStdNormal().ToString(), ColorAnsi.White);
            startingConsole.Print(6, 6, rng.GetInt32(1, 50).ToString(), ColorAnsi.White);
            startingConsole.Print(6, 7, rng.GetInt64(int.MaxValue).ToString(), ColorAnsi.White);
            startingConsole.Print(6, 4, rng.Next().ToString(), ColorAnsi.White);

            //Set our new console as the thing to render and process
            SadConsole.Global.CurrentScreen = startingConsole;
        }
    }
}