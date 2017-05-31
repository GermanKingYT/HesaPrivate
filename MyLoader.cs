namespace Flowers_ADCSeries
{
    using HesaEngine.SDK;

    using MyBase;

    public class MyLoader : IScript
    {
        public string Name { get; } = "Flowers' ADC Series";
        public string Version { get; } = "1.0.0.0";
        public string Author { get; } = "NightMoon";

        public void OnInitialize()
        {
            Game.OnGameLoaded += MyChampion.Init;
        }
    }
}