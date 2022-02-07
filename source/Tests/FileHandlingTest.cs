using Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using TelegramApi;

namespace Tests
{
    [TestClass]
    public class FileHandlingTest
    {
        [TestMethod]
        public void TestTrimFile()
        {
            var iBimsATestFile = "testFile.txt";
            for (int i = 0; i < 56; i++)
            {
                File.AppendAllText(iBimsATestFile, $"TestEntry {i:D2} {Environment.NewLine}");
            }
            FileHandling.TrimFileListOfProcessedFile(iBimsATestFile, 54);
            Assert.AreEqual(File.ReadAllLines(iBimsATestFile).Length, 54);
        }


        [DeploymentItem("2022-01-15T09-09-55Z_Full_Meta_YT.json")]
        [DeploymentItem("2022-01-23T13-10-11Z_Full_Meta_YT.json")]
        [DeploymentItem("2022-02-03T19-20-44Z_Full_Meta_YT.json")]
        [TestMethod]
        public void TestRollingFileUpdater()
        {
            for (int i = 0; i < 15; i++)
            {
                File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"fileUpdater_{i:D2}.txt"), $"File content {i:D2}");
            }
            
            FileHandling.RollingFileUpdater(AppDomain.CurrentDomain.BaseDirectory, "fileUpdater", 10);
            var files = Directory.EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory)
                                 .Where(file => file.Contains("fileUpdater"))
                                 .ToList();
            Assert.IsTrue(files.Count == 10);
            FileHandling.RollingFileUpdater(AppDomain.CurrentDomain.BaseDirectory, "fileUpdater", 2);
            files = Directory.EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory)
                             .Where(file => file.Contains("fileUpdater"))
                             .ToList();
            Assert.IsTrue(files.Count == 2);

            FileHandling.RollingFileUpdater(AppDomain.CurrentDomain.BaseDirectory, "fileUpdater", 0);
            files = Directory.EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory)
                             .Where(file => file.Contains("fileUpdater"))
                             .ToList();
            Assert.IsTrue(files.Count == 0);
        }

        /// <summary>
        /// Keine Ahnung warum man hier DeploymentItems verwenden soll. Der Scheiß funktioniert ja eh nicht richtig. Bei Verwendung der Attribute
        /// ClassInitialize und ClassCleanup.
        /// </summary>
        [DeploymentItem("2022-01-15T09-09-55Z_Full_Meta_YT.json")]
        [DeploymentItem("2022-01-23T13-10-11Z_Full_Meta_YT.json")]
        [DeploymentItem("2022-02-03T19-20-44Z_Full_Meta_YT.json")]
        [DeploymentItem("listOfProcessedFiles.list")]
        [TestMethod]
        public void TestGetFiles()
        {
            var workfolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "testWorkDir");
            var listOfProcessedFiles = Path.Combine(workfolder, "listOfProcessedFiles.list");
            var theList = FileHandling.FindNotYetProcessedYoutubeMetaFiles(listOfProcessedFiles,
                                                                           workfolder,
                                                                           VideoMetaDataFull.YoutubeSearchPattern);
            Assert.AreEqual(theList.Count, 1);
            CleanupFullMetaYoutubeFiles();
        }


        /// <summary>
        /// CleanUp, Initialize and DeploymentItem do not work as expected. MS sucks, no exceptions. Therefore a separate method.
        /// </summary>
        public void CleanupFullMetaYoutubeFiles()
        {
            Directory
                .EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory)
                .Where(file => file.EndsWith(VideoMetaDataFull.YoutubeSearchPattern))
                .ToList()
                .ForEach(File.Delete);

            Directory
                .EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory)
                .Where(file => file.EndsWith("listOfProcessedFiles.list"))
                .ToList()
                .ForEach(File.Delete);
        }
    }
}
