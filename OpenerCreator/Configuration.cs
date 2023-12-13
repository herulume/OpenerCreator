using System.IO;
using Newtonsoft.Json;

namespace OpenerCreator;

public class Configuration
{
    public int CountdownTime = 7;

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
