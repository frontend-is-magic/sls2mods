namespace Sls2Mods.Utils.Randoming;

public static class DeterministicSeed
{
    public static uint FromString(string value)
    {
        unchecked
        {
            uint hash = 2166136261;
            foreach (var ch in value)
            {
                hash ^= ch;
                hash *= 16777619;
            }

            return hash;
        }
    }
}
