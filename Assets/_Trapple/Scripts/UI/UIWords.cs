using Trapple.Hub;

namespace Trapple.UI
{
    // Thaleah's original character set is Latin. Keep the configuration (and saves)
    // untouched; present the supplied English art and matching English captions.
    public static class UIWords
    {
        public static string World(string id) => id == "meadow" ? "APPLE MEADOW" : id == "forest" ? "DECEPTIVE FOREST" :
            id == "factory" ? "ABANDONED FACTORY" : id == "lava" ? "LAVA FORTRESS" : "WORLD";
        public static string Upgrade(UpgradeKind kind) => kind == UpgradeKind.Speed ? "SPEED" : kind == UpgradeKind.Jump ? "JUMP" : "AGILITY";
        public static string Skin(string id) => id == "classic" ? "FIRST APPLE" : id == "forest" ? "FOREST TRAVELER" :
            id == "factory" ? "LITTLE MINER" : id == "lava" ? "VOLCANO HEART" : "APPI";
    }
}
