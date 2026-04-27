using System;
using BaseLib.Config;

namespace Sls2Mods.Utils.Config;

public static class ModConfigMenuRegistrar
{
    public static bool TryRegister(string modId, SimpleModConfig menu, Action<string> log)
    {
        try
        {
            ModConfigRegistry.Register(modId, menu);
            log($"{modId}: registered in-game config menu");
            return true;
        }
        catch (Exception ex)
        {
            log($"{modId}: failed to register in-game config menu, using JSON config only: {ex.Message}");
            return false;
        }
    }
}
