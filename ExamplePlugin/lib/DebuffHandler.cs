using RoR2;

namespace ExamplePlugin
{
    public abstract class DebuffHandler
    {
        public uint GetVictimNetworkUserId(CharacterBody victim)
        {
            return victim.networkIdentity.netId.Value;
        }

        public abstract void ClearCache();
        public abstract void EnsureIsCachedAndAlive(CharacterBody victim);
    }
}