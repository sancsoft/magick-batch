using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageMagick;
using System.Configuration;
using Serilog;

namespace magick_batch_framework_console
{
    class Program
    {
        /// <summary>
        /// GhostScript is required for ImageMagick to work and must be installed separately.
        /// GhostScriptPath can be set in the project's app.config and should point to the directory
        /// where the gsdll32.dll (32 bit ver.) or gsdl64.dll (64 bit ver.) reside.
        /// </summary>
        static readonly string _GhostScriptPath = ConfigurationManager.AppSettings["GhostScriptPath"];

        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                // Writes to project folder's /bin/Debug/ directory
                .WriteTo.File("./magick-batch-console-log.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();


            MagickNET.Initialize();
            MagickNET.SetGhostscriptDirectory(_GhostScriptPath);
            bool endApp = false;
            Console.WriteLine("TMS Magick Batch PNG to PDF App\r");
            Console.WriteLine("-------------------------------\n");

            while (!endApp)
            {
                string sourceDir = "";
                string destDir = "";
                string fromDateInput = "";
                string toDateInput = "";

                // Ask the user to type the source directory
                Console.Write("Enter the file path for the source directory of the images to be converted: ");
                sourceDir = @"" + Console.ReadLine();


                // Ask the user to type the destination directory
                Console.Write("Enter the file path for the destination directory of the converted pdfs: ");
                destDir = @"" + Console.ReadLine();

                // Ask the user to type the from date
                Console.Write("Enter the date the program will start at in MM/dd/yyyy format: ");
                fromDateInput = Console.ReadLine();

                DateTime fromDate;
                while (!DateTime.TryParse(fromDateInput, out fromDate))
                {
                    Console.Write("This is not valid input. Please enter a date: ");
                    fromDateInput = Console.ReadLine();
                }

                // Ask the user to type the to date
                Console.Write("Enter the date the program will end at in MM/dd/yyyy format: ");
                toDateInput = Console.ReadLine();

                DateTime toDate;
                while (!DateTime.TryParse(toDateInput, out toDate))
                {
                    Console.Write("This is not valid input. Please enter a date: ");
                    toDateInput = Console.ReadLine();
                }

                Console.WriteLine("source directory: " + sourceDir);
                Console.WriteLine("destination directory: " + destDir);
                Console.WriteLine("from date: " + fromDate);
                Console.WriteLine("to date: " + toDate);

                try
                {
                    DirectoryInfo source = new DirectoryInfo(sourceDir);
                    DirectoryInfo destination = new DirectoryInfo(destDir);
                    FileInfo[] sourceFiles = source.GetFiles();
                    foreach (FileInfo file in sourceFiles)
                    {
                        if (file.CreationTime >= fromDate && file.CreationTime <= toDate)
                        {
                            string newFileName;
                            using (var magik = new MagickImage(file))
                            {
                                newFileName = file.Name.Replace(".pdf", ".png");
                                magik.Write(Path.Combine(destDir, newFileName));
                            }

                            Log.Information("Created: " + newFileName);
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error("EXCEPTION: \n" + e.Message);
                    Log.Error("STACK TRACE: \n" + e.StackTrace);
                }

                Console.WriteLine("------------------------\n");

                // Wait for the user to respond before closing.
                Console.Write("Press 'n' and Enter to close the app, or press any other key and Enter to continue: ");
                if (Console.ReadLine() == "n") endApp = true;

                Console.WriteLine("\n");
            }
            return;
        }
    }
}
