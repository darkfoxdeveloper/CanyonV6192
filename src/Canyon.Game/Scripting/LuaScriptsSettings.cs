using Microsoft.Extensions.Configuration;

namespace Canyon.Game.Scripting
{
    public class LuaScriptsSettings
    {
        public static LuaScriptsSettings Settings { get; private set; }

        static LuaScriptsSettings()
        {
            Reload();
        }

        public static void Reload()
        {
            Settings = new LuaScriptsSettings();
        }

        public LuaScriptsSettings() 
        {
            new ConfigurationBuilder()
                .AddIniFile(Path.Combine(Environment.CurrentDirectory, "lua", "lua.ini"))
                .Build()
                .Bind(this);
        }

        public Dictionary<string, string> MOD { get; set; }
    }
}
