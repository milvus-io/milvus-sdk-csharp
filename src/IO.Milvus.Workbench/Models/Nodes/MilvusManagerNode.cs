using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace IO.Milvus.Workbench.Models
{
    public class MilvusInstanceConfig
    {
        public string Name { get; set; }

        public string Host { get; set; }

        public int Port { get; set; }
    }

    public class MilvusManagerNode : Node<MilvusConnectionNode>
    {
        public string ConfigFilePathName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "milvusworkbench", "milvusinstance.json");

        public async Task SaveAsync()
        {
            var configs = Children.Select(p => new MilvusInstanceConfig()
            {
                Name = p.Name,
                Host = p.Host,
                Port = p.Port
            });

            var str = JsonConvert.SerializeObject(configs);

            var dir = Path.GetDirectoryName(ConfigFilePathName);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

#if NET461_OR_GREATER
            await Task.Run(() =>
            {
                File.WriteAllText(ConfigFilePathName, str);
            });
#else
            await File.WriteAllTextAsync(ConfigFilePathName, str);
#endif
        }

        public async Task<List<MilvusInstanceConfig>> ReadConfigAsync()
        {
            if (File.Exists(ConfigFilePathName))
            {
#if NET461_OR_GREATER
                var str = await Task.Run(() =>{
                    return File.ReadAllText(ConfigFilePathName); 
                });               
#else
                var str = await File.ReadAllTextAsync(ConfigFilePathName);
#endif
                return JsonConvert.DeserializeObject<List<MilvusInstanceConfig>>(str);
            }
            else
            {
                return new List<MilvusInstanceConfig>();
            }
        }
    }
}
