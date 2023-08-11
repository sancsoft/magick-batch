using ImageMagick;
using System;
using System.Configuration;
using System.IO;
using System.Threading;
using Serilog;

namespace magick_batch
{
    class Service
    {
        public Service()
        {
            _sourcePath = ConfigurationManager.AppSettings["SourcePath"];
            _targetPath = ConfigurationManager.AppSettings["TargetPath"];

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                // Writes to project folder's /bin/Debug/ directory
                .WriteTo.File("./magick-batch-log.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            if (!Directory.Exists(_sourcePath))
            {
                Log.Error("_sourcePath: " + _sourcePath + " does not exist.");
                Stop();
            }
            if (!Directory.Exists(_targetPath))
            {
                Log.Error("_targetPath: " + _targetPath + " does not exist.");
                Stop();
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
                // Kill the application.
                Environment.Exit(1);

                // Kill the thread.
                //_thread.Abort();
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

                Log.Information("Created " + newFileName);
            }
            catch (Exception ex)
            {
                Log.Error("Error during OnCreated. Error Message: \n" + ex.Message + "\n Stack Trace: \n" + ex.StackTrace);
                Stop();
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

                Log.Information("Changed " + newFileName);
            }
            catch (Exception ex)
            {
                Log.Error("Error during OnChange. Error Message: \n" + ex.Message + "\n Stack Trace: \n" + ex.StackTrace);
                Stop();
            }
        }

        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            try
            {
                string fileName = e.Name.Replace(".pdf", ".png");

                File.Delete(Path.Combine(_targetPath, fileName));

                Log.Information("Deleted " + fileName);
            }
            catch (Exception ex)
            {
                Log.Error("Error during OnDeleted. Error Message: \n" + ex.Message + "\n Stack Trace: \n" + ex.StackTrace);
                Stop();
            }
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            try
            {
                string oldName = e.OldName.Replace(".pdf", ".png");
                string newName = e.Name.Replace(".pdf", ".png");

                File.Move(Path.Combine(_targetPath, oldName), Path.Combine(_targetPath, newName));

                Log.Information("Renamed " + oldName + " to " + newName);
            }
            catch (Exception ex)
            {
                Log.Error("Error during OnRenamed. Error Message: \n" + ex.Message + "\n Stack Trace: \n" + ex.StackTrace);
                Stop();
            }
        }
    }
}
