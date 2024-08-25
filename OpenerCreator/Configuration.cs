using System.IO;
using Dalamud.Configuration;
using Newtonsoft.Json;

namespace OpenerCreator;

public class Configuration : IPluginConfiguration
{
    public bool AbilityAnts = true;
    public int CountdownTime = 7;
    public bool IgnoreTrueNorth = true;
    public bool IsCountdownEnabled = false;
    public bool StopAtFirstMistake = false;
    public int Version { get; set; } = 1;

    public static Configuration Load()
    {
        return OpenerCreator.PluginInterface.ConfigFile.Exists
                   ? JsonConvert.DeserializeObject<Configuration>(
                         File.ReadAllText(OpenerCreator.PluginInterface.ConfigFile.FullName)) ?? new Configuration()
                   : new Configuration();
    }

    public void Save()
    {
        File.WriteAllText(OpenerCreator.PluginInterface.ConfigFile.FullName, JsonConvert.SerializeObject(this));
    }
}
