using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;

namespace BundlerMinifier.NUglify
{
    /// <summary>
    /// An MSBuild task for running web compilers on a given config file.
    /// </summary>
    public class BundlerBuildTask : Task
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

            Log.LogMessage(MessageImportance.High, Environment.NewLine + "Bundler: Begin processing " + configFile.Name);

            BundleFileProcessor processor = new BundleFileProcessor();
            processor.Processing += (s, e) => RemoveReadonlyFlagFromFile(e.Bundle.GetAbsoluteOutputFile());
            processor.AfterBundling += (s, e) => Log.LogMessage(MessageImportance.High, "\tBundled " + e.Bundle.OutputFileName);
            BundleMinifier.BeforeWritingMinFile += (s, e) => RemoveReadonlyFlagFromFile(e.ResultFile);
            processor.BeforeWritingSourceMap += (s, e) => RemoveReadonlyFlagFromFile(e.ResultFile);
            processor.AfterWritingSourceMap += (s, e) => Log.LogMessage(MessageImportance.High, "\tSourceMap " + FileHelpers.MakeRelative(fileName, e.ResultFile));
            BundleMinifier.ErrorMinifyingFile += BundleMinifier_ErrorMinifyingFile;
            BundleMinifier.AfterWritingMinFile += (s, e) => Log.LogMessage(MessageImportance.High, "\tMinified " + FileHelpers.MakeRelative(fileName, e.ResultFile));
            processor.MinificationSkipped += (s, e) => Log.LogMessage(MessageImportance.Normal, "Bundler: No changes, skipping minification of " + e.OutputFileName);

            processor.Process(configFile.FullName);

            Log.LogMessage(MessageImportance.High, "Bundler: Done processing " + configFile.Name);
        }

        private static void RemoveReadonlyFlagFromFile(string fileName)
        {
            FileInfo file = new FileInfo(fileName);

            if (file.Exists && file.IsReadOnly)
                file.IsReadOnly = false;
        }

        private void BundleMinifier_ErrorMinifyingFile(object sender, MinifyFileEventArgs e)
        {
            if (e.Result?.HasErrors != true)
                return;

            _isSuccessful = false;

            foreach (var error in e.Result.Errors)
                Log.LogError("Bundler & Minifier", "0", "", error.FileName, error.LineNumber, error.ColumnNumber, error.LineNumber, error.ColumnNumber, error.Message, null);
        }
    }
}