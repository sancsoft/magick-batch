using ImageMagick;
using System;
using System.Configuration;
using System.IO;
using System.Threading;

namespace magick_batch
{
    class Service
    {
        public Service()
        {
            _sourcePath = ConfigurationManager.AppSettings["SourcePath"];
            _targetPath = ConfigurationManager.AppSettings["TargetPath"];

            if (!Directory.Exists(_sourcePath))
            {
                Console.WriteLine("_sourcePath: " + _sourcePath + " does not exist.");
            }
            if (!Directory.Exists(_targetPath))
            {
                Console.WriteLine("_targetPath: " + _targetPath + " does not exist.");
            }
        }

        public void Start()
        {
            _taskRunning = true;

            // Create and start the thread in the background.
            _thread = new Thread(Watch)
            {
                IsBackground = true
            };

            _thread.Start();
        }

        public void Stop()
        {
            _taskRunning = false;

            if (_thread == null)
            {
                return;
            }

            // Wait up to 10 seconds for the thread to finish.
            _thread.Join(10000);

            if (_thread.IsAlive && _thread != null)
            {
                // Kill the thread.
                _thread.Abort();
            }
            _thread = null;
        }

        readonly string _sourcePath;
        readonly string _targetPath;
        Thread _thread;
        bool _taskRunning = false;

        private void Watch()
        {
            // Create the file system watcher.
            using (var watcher = new FileSystemWatcher())
            {
                // The directory to watch.
                watcher.Path = _sourcePath;

                // The actions to get notified for.
                watcher.NotifyFilter = NotifyFilters.LastAccess
                             | NotifyFilters.LastWrite
                             | NotifyFilters.FileName
                             | NotifyFilters.DirectoryName;

                // The type of files to watch.
                watcher.Filter = "*.pdf";

                // Bind the events.
                watcher.Changed += OnChanged;
                watcher.Created += OnCreated;
                watcher.Deleted += OnDeleted;
                watcher.Renamed += OnRenamed;

                // Activate the event watching.
                watcher.EnableRaisingEvents = true;

                // Let this thread sleep while listening for events.
                while (_taskRunning)
                {
                    Thread.Sleep(2000);
                }

                // Done listening, remove events.
                watcher.Changed -= OnChanged;
                watcher.Created -= OnCreated;
                watcher.Deleted -= OnDeleted;
                watcher.Renamed -= OnRenamed;
            }
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            try
            {
                string newFileName;
                using (var magik = new MagickImage(e.FullPath))
                {
                    newFileName = e.Name.Replace(".pdf", ".png");
                    magik.Write(Path.Combine(_targetPath, newFileName));
                }

                Console.WriteLine("Created " + newFileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Created error: " + ex.StackTrace);
            }
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                string newFileName;
                using (var magik = new MagickImage(e.FullPath))
                {
                    newFileName = e.Name.Replace(".pdf", ".png");
                    magik.Write(Path.Combine(_targetPath, newFileName));
                }

                Console.WriteLine("Changed " + newFileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Changed error: " + ex.StackTrace);
            }
        }

        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            try
            {
                string fileName = e.Name.Replace(".pdf", ".png");

                File.Delete(Path.Combine(_targetPath, fileName));

                Console.WriteLine("Deleted " + fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Deleted error: " + ex.StackTrace);
            }
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            try
            {
                string oldName = e.OldName.Replace(".pdf", ".png");
                string newName = e.Name.Replace(".pdf", ".png");

                File.Move(Path.Combine(_targetPath, oldName), Path.Combine(_targetPath, newName));

                Console.WriteLine("Renamed " + oldName + " to " + newName);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Renamed error: " + ex.StackTrace);
            }
        }
    }
}
