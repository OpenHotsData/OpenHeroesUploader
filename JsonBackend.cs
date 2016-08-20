using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.IO.IsolatedStorage;

namespace OpenHeroesUploader
{
    public class JsonBackend
    {
        private string Path {get; }
        
        public JsonBackend(string path)
        {
            Path = path;
        }

        public class SerializeReplay
        {
            public UploadState State { get; set; }
            public string Path { get; set; }
        }

        public Task WriteReplayFile(IEnumerable<ReplayForUpload> replays)
        {
            var groups = replays.GroupBy(r => r.Directory, r => new SerializeReplay()
            {
                State = r.State,
                Path = r.Path
            });

            var dict = groups.ToDictionary(r => r.Key, r => r.Select(i => i));
            string jstring = JsonConvert.SerializeObject(dict);
            var store = IsolatedStorageFile.GetUserStoreForApplication();
            using (var ws = store.OpenFile(Path, FileMode.Create))
            using (var writer = new StreamWriter(ws))
            {
                return writer.WriteAsync(jstring);
            }
        }

        public async Task<IDictionary<string, ReplayForUpload>> LoadReplays()
        {
            var store = IsolatedStorageFile.GetUserStoreForApplication();
            var files = store.GetFileNames();
            try
            {
                using (var stream = store.OpenFile(Path, FileMode.Open, FileAccess.Read))
                using (var reader = new StreamReader(stream))
                {
                    string contents = await reader.ReadToEndAsync();
                    var fromSource = JsonConvert.DeserializeObject<Dictionary<string, List<SerializeReplay>>>(contents);
                    var replays = fromSource.SelectMany(kvp => kvp.Value.Select(ser =>
                    {
                        var res = ReplayForUpload.newFromPath(ser.Path);
                        res.State = ser.State;
                        return res;
                    }));
                    return replays.ToDictionary(r => r.Directory);
                }
            } catch (Exception)
            {
                return new Dictionary<string, ReplayForUpload>();
            }
        }
    }
}

