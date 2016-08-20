using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.IO;
using System.Collections.ObjectModel;
using System.Configuration;
using Newtonsoft.Json;
using System.Reactive.Threading.Tasks;
using System.Threading;

namespace OpenHeroesUploader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public IObservable<string> DirectoryObservable { get; }
        private Subject<string> DirectorySubject = new Subject<string>();
        private JsonBackend Store { get; }

        private IObservable<IDictionary<string, ReplayForUpload>> _KnownReplays;

        private IObservable<IDictionary<string, ReplayForUpload>> __KnownReplays
        {
            get
            {
                return _KnownReplays;
            }
            set
            {
                _KnownReplays = value;
            }
        }
        ObservableCollection<ReplayForUpload> GridItems = new ObservableCollection<ReplayForUpload>();

        ObservableCollection<ReplayForUpload> Unhandled = new ObservableCollection<ReplayForUpload>();
        ObservableCollection<ReplayForUpload> Uploading = new ObservableCollection<ReplayForUpload>();
        SemaphoreSlim uploadThrottle;
        
        Uploader Uploader { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            uploadThrottle = new SemaphoreSlim(4);
            DirectoryObservable = DirectorySubject;
            Store = new JsonBackend("fuuuuuuuuu");
            //KnownReplays = Store.LoadReplays().ToObservable;
        }

        private IObservable<ReplayForUpload> pipeline(IObservable<String> folders)
        {
            Func<IObservable<String>, IObservable<ReplayForUpload>> enqueueUnhanded = f => f.Do(folder => directorycontent.Content = folder)
                .Select(folder => new ReplayNotifier(folder))
                .Select(n => InitializeStorage(n).Concat(n.files.Select(ReplayForUpload.newFromPath)))
                .Switch()
                .Do(replay =>
                {
                    if (replay.State == UploadState.Unhandeled)
                    {
                        Unhandled.Add(replay);
                    }
                });

            return enqueueUnhanded(folders)
              .SelectAsync(r => Uploader.UploadWithRetries(r, 10, 0));

        }

        private IObservable<ReplayForUpload> InitializeStorage(ReplayNotifier notifier)
        {

            var initialfiles = notifier.InitialFiles.Select(ReplayForUpload.newFromPath);
            return initialfiles.ToObservable();
        }
        
        
        private void click_folder_select(object sender, RoutedEventArgs e)
        {
            using (var dlg = new CommonOpenFileDialog())
            {
                dlg.Title = "Heroes Replays folder";
                dlg.IsFolderPicker = true;

                dlg.AddToMostRecentlyUsedList = false;
                dlg.AllowNonFileSystemItems = false;
                dlg.EnsureFileExists = true;
                dlg.EnsurePathExists = true;
                dlg.EnsureReadOnly = false;
                dlg.EnsureValidNames = true;
                dlg.Multiselect = false;
                dlg.ShowPlacesList = true;

                if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    var folder = dlg.FileName;
                    try
                    {
                        DirectorySubject.OnNext(folder);
                    } catch (Exception ee)
                    {
                        var st = ee.StackTrace;
                    }
                }
                
            }
        }

        void SaveSourcedir(string sourcedir)
        {
            Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            configuration.AppSettings.Settings["sourcedir"].Value = sourcedir;
            configuration.Save();
            ConfigurationManager.RefreshSection("appSettings");
        }
        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var sourcedir = ConfigurationManager.AppSettings["sourcedir"];
            if(sourcedir != "")
            {
                DirectorySubject.OnNext(sourcedir);
            }
            var uploadURL = new Uri(ConfigurationManager.AppSettings["uploadurl"]);
            Uploader = new Uploader(uploadURL, uploadThrottle); 
            
            pipeline(DirectoryObservable)
                .Subscribe(_ => { }); //noop to evaluate the pipe

            // Load data by setting the CollectionViewSource.Source property:
            // replayForUploadViewSource.Source = [generic data source]
        }

    }


}
