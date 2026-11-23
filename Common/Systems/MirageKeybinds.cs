using Terraria.ModLoader;

namespace LiminalHuntersMod.Common.Systems
{
    public class MirageKeybinds : ModSystem
    {
        /// <summary>"Liminal Ability" - default Q. (Q is also Terraria's default Quick Heal; players can rebind either.)</summary>
        public static ModKeybind LiminalAbility { get; private set; }

        public override void Load()
        {
            LiminalAbility = KeybindLoader.RegisterKeybind(Mod, "LiminalAbility", "Q");
        }

        public override void Unload()
        {
            LiminalAbility = null;
        }
    }
}
