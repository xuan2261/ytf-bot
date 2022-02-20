using Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TelegramApi;

namespace Tests
{
    [TestClass]
    public class FileHandlingTest
    {
        public string WorkFolder => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "testFileHandlingWorkDir");

        public void SetupTest()
        {
            if (Directory.Exists(WorkFolder))
            {
                // Clean dir
                var di = new DirectoryInfo(WorkFolder);
                foreach (var file in di.EnumerateFiles())
                {
                    file.Delete();
                }
            }
            else
            {
                Directory.CreateDirectory(WorkFolder);
            }
        }

        [TestMethod]
        public void TestTrimFile()
        {
            SetupTest();

            var iBimsATestFile = "testFile.txt";
            for (int i = 0; i < 56; i++)
            {
                File.AppendAllText(iBimsATestFile, $"TestEntry {i:D2} {Environment.NewLine}");
            }
            FileHandling.TrimFileListOfProcessedFile(iBimsATestFile, 54);
            Assert.AreEqual(File.ReadAllLines(iBimsATestFile).Length, 54);
        }

        /// <summary>
        /// Create a few files and tests the rolling file updater.
        /// </summary>
        [TestMethod]
        public void TestRollingFileUpdater()
        {
            SetupTest();

            // Create 15 files
            for (int i = 0; i < 15; i++)
            {
                File.AppendAllText(Path.Combine(WorkFolder, $"fileUpdater_{i:D2}.txt"), $"File content {i:D2}");
            }
            
            FileHandling.RollingFileUpdater(WorkFolder, "fileUpdater", 10);
            var files = Directory.EnumerateFiles(WorkFolder)
                                 .Where(file => file.Contains("fileUpdater"))
                                 .ToList();
            Assert.IsTrue(files.Count == 10);
            FileHandling.RollingFileUpdater(WorkFolder, "fileUpdater", 2);
            files = Directory.EnumerateFiles(WorkFolder)
                             .Where(file => file.Contains("fileUpdater"))
                             .ToList();
            Assert.IsTrue(files.Count == 2);

            FileHandling.RollingFileUpdater(WorkFolder, "fileUpdater", 0);
            files = Directory.EnumerateFiles(WorkFolder)
                             .Where(file => file.Contains("fileUpdater"))
                             .ToList();
            Assert.IsTrue(files.Count == 0);
        }

        /// <summary>
        /// Keine Ahnung warum man hier DeploymentItems verwenden soll. Der Scheiß funktioniert ja eh nicht richtig. Bei Verwendung der Attribute
        /// ClassInitialize und ClassCleanup.
        /// </summary>
        [TestMethod]
        public void TestFindNotYetProcessedVideoIdFiles()
        {
            SetupTest();
            var myListOfIds = new List<string> { "11", "12", "13" };

            myListOfIds.ForEach(id =>
                                {
                                    File.WriteAllText(Path.Combine(WorkFolder, $"{id}.video"), $"Content egal {id}");
                                });

            var alreadyProcessed = Path.Combine(WorkFolder, "11.video") + Environment.NewLine + Path.Combine(WorkFolder, "12.video");
            var fileName = Path.Combine(WorkFolder, "listOfProcessedFiles.list");
            File.WriteAllText(fileName, alreadyProcessed);

           
            var theList = FileHandling.FindNotYetProcessedVideoIdFiles(fileName,
                                                                       WorkFolder,
                                                                       VideoMetaDataFull.VideoFileSearchPattern);
            Assert.AreEqual(theList.Count, 1);
        }

        /// <summary>
        /// Checks method ReduceListOfIds.
        /// </summary>
        [TestMethod]
        public void TestGetIdsOfFilesNotYetIncludedInFolder()
        {
            SetupTest();
            var myListOfIds = new List<string> {"11", "12", "13" };

            myListOfIds.ForEach(id =>
                                {
                                    File.Create(Path.Combine(WorkFolder, $"{id}.video"));
                                });
            
            // myListOfIds = file list in Workfolder
            var newList = FileHandling.ReduceListOfIds(myListOfIds, WorkFolder, VideoMetaDataFull.VideoFileSearchPattern);
            Assert.AreEqual(newList.Count, 0);

            // myListOfIds contains two ids that are not yet as a file in the folder
            myListOfIds.Add("14");
            myListOfIds.Add("15");
            newList = FileHandling.ReduceListOfIds(myListOfIds, WorkFolder, VideoMetaDataFull.VideoFileSearchPattern);
            Assert.AreEqual(newList.Count, 2);

            // myListOfIds is empty => no results in newList
            myListOfIds.Clear();
            newList = FileHandling.ReduceListOfIds(myListOfIds, WorkFolder, VideoMetaDataFull.VideoFileSearchPattern);
            Assert.AreEqual(newList.Count, 0);
        }
    }
}
