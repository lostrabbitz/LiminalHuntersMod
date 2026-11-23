using Terraria.Audio;

namespace LiminalHuntersMod.Common.Managers
{
    /// <summary>
    /// All custom sounds in one place (file names WITHOUT extension: .wav / .ogg / .mp3 all work).
    /// Folder: Assets/Sounds/SFX/Items/PhantomLance/
    /// If your hit sound lives somewhere else, just change the path of LanceHit below.
    /// </summary>
    public static class MirageSounds
    {
        private static string Path(string file)
            => $"{nameof(LiminalHuntersMod)}/Assets/Sounds/Items/Weapons/PhantomLance/{file}";

        /// <summary>Phantom Dodge triggering. File: Assets/Sounds/SFX/Player/MirageDodge</summary>
        public static readonly SoundStyle Dodge = new SoundStyle($"{nameof(LiminalHuntersMod)}/Assets/Sounds/SFX/Player/MirageDodge")
        {
            Volume = 0.9f
        };

        /// <summary>Swing of the Phantom Lance (player AND clones).</summary>
        public static readonly SoundStyle LanceSwing = new SoundStyle(Path("PhantomLanceSwing"))
        {
            Volume = 0.4f,
            PitchVariance = 0.2f,
            MaxInstances = 3
        };

        /// <summary>
        /// Swing of the PLAYER's lance. Same file, but MaxInstances = 0 (unlimited): the clones' swings share the
        /// limited style above, and with several clones swinging they could use up all the slots and silence the player.
        /// </summary>
        public static readonly SoundStyle LanceSwingPlayer = new SoundStyle(Path("PhantomLanceSwing"))
        {
            Volume = 0.7f,
            PitchVariance = 0.2f,
            MaxInstances = 0
        };


        /// <summary>Lance hitting an enemy (player AND clones). Change the file name to your existing hit sound.</summary>
        public static readonly SoundStyle LanceHit = new SoundStyle(Path("PhantomLanceHit"))
        {
            Volume = 0.7f,
            PitchVariance = 0.2f,
            MaxInstances = 3
        };

        /// <summary>Juxtapose: player vanishes.</summary>
        public static readonly SoundStyle JuxtaposeDisappear = new SoundStyle(Path("JuxtaposeDisappear"))
        {
            Volume = 0.9f
        };

        /// <summary>Juxtapose: player reappears.</summary>
        public static readonly SoundStyle JuxtaposeAppear = new SoundStyle(Path("JuxtaposeAppear"))
        {
            Volume = 0.9f
        };
    }
}
