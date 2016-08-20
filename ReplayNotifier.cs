using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;

namespace OpenHeroesUploader
{
    public class ReplayNotifier : IDisposable
    {
        private FileSystemWatcher watcher;
        public Subject<string> filesSubject = new Subject<string>();
        public IObservable<string> files;
        FileSystemWatcher logger;

        public ReplayNotifier(string sourcedir)
        {
            Console.WriteLine("replay notifier created for " + sourcedir);
            files = filesSubject;
            watcher = new FileSystemWatcher(sourcedir, "*.StormReplay");
            watcher.Created += (object sender, FileSystemEventArgs e) =>
            {
                filesSubject.OnNext(e.FullPath);
            };
            logger = new FileSystemWatcher(sourcedir);
            logger.Changed += (object sender, FileSystemEventArgs args) => Console.WriteLine(String.Format("watcher changed: {0} - {1}", args.ChangeType, args.Name));
            logger.Created += (object sender, FileSystemEventArgs args) => Console.WriteLine(String.Format("created: {0}", args.Name));
            logger.Deleted += (object sender, FileSystemEventArgs args) => Console.WriteLine(String.Format("deleted: {0}", args.Name));
            logger.Renamed += (object sender, RenamedEventArgs args) => Console.WriteLine(String.Format("deleted: {0}", args.OldName, args.Name));
            logger.EnableRaisingEvents = true;
            watcher.EnableRaisingEvents = true;
        }

        public IEnumerable<string> InitialFiles => (new DirectoryInfo(watcher.Path)).EnumerateFiles("*.StormReplay").Select(f => f.FullName);
        public string Path => watcher.Path;

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    watcher.Dispose();
                    logger.Dispose();
                    filesSubject.OnCompleted();
                    filesSubject.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

    }
}
