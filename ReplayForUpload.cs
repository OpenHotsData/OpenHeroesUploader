using System;
using System.IO;

namespace OpenHeroesUploader
{

    public enum UploadState
    {
        Unhandeled,
        Uploading,
        Uploaded,
        Duplicate,
        Faulted
    }
    public class ReplayForUpload
    {
        public string FileName => System.IO.Path.GetFileNameWithoutExtension(Path);
        public string Directory => new FileInfo(Path).Directory.FullName;
        public string Path { get; set; }
        public DateTime Changed { get; set; }
        public UploadState State { get; set; }

        public static ReplayForUpload newFromPath(string path)
        {
            var file = new FileInfo(path);
            return new ReplayForUpload()
            {
                Path = path,
                Changed = file.LastWriteTime,
                State = UploadState.Unhandeled
            };
        }

    }

}
