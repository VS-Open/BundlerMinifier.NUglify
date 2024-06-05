using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;

namespace BundlerMinifier.NUglify
{
    public class BundlerCleanTask : Task
    {
        private bool _isSuccessful = true;

        public string BundleConfigFolder { get; set; }

        /// <summary>
        /// Execute the Task
        /// </summary>
        public override bool Execute()
        {
            foreach (string file in Directory.GetFiles(BundleConfigFolder, "bundleconfig*.json", SearchOption.AllDirectories))
                Process(file);

            return _isSuccessful;
        }


        private void Process(string fileName)
        {
            FileInfo configFile = new FileInfo(fileName);

            Log.LogMessage(MessageImportance.High, Environment.NewLine + "Bundler: Cleaning output from " + configFile.Name);

            var bundles = BundleHandler.GetBundles(configFile.FullName);

            if (bundles != null)
            {
                foreach (Bundle bundle in bundles)
                {
                    var outputFile = bundle.GetAbsoluteOutputFile();
                    var inputFiles = bundle.GetAbsoluteInputFiles();

                    var minFile = BundleMinifier.GetMinFileName(outputFile);
                    var mapFile = minFile + ".map";
                    var gzipFile = minFile + ".gz";

                    if (!inputFiles.Contains(outputFile))
                        Deletefile(outputFile);

                    Deletefile(minFile);
                    Deletefile(mapFile);
                    Deletefile(gzipFile);
                }

                Log.LogMessage(MessageImportance.High, "Bundler: Done cleaning output file from " + configFile.Name);

                return;
            }

            Log.LogWarning($"There was an error reading {configFile.Name}");
            _isSuccessful = false;
        }

        private void Deletefile(string file)
        {
            try
            {
                if (File.Exists(file))
                {
                    FileHelpers.RemoveReadonlyFlagFromFile(file);
                    File.Delete(file);
                }
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex);
            }
        }
    }
}