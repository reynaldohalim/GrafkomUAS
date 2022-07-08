using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using System;

namespace GrafkomUAS
{
    class Program
    {
        static void Main(string[] args)
        {
            var ourWindowSetting = new NativeWindowSettings()
            {
                Size = new Vector2i(1600, 900),
                Title = "Grafkom UAS Kel 15"
            };
            using (var win = new Windows(GameWindowSettings.Default, ourWindowSetting))
                win.Run();
        }
    }
}
