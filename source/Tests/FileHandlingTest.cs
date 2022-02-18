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

        [TestMethod]
        public void TestTrimFile()
        {
            if (!Directory.Exists(WorkFolder))
            {
                Directory.CreateDirectory(WorkFolder);
            }

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
            if (!Directory.Exists(WorkFolder))
            {
                Directory.CreateDirectory(WorkFolder);
            }

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
        [DeploymentItem("2022-01-15T09-09-55Z_Full_Meta_YT.json")]
        [DeploymentItem("2022-01-23T13-10-11Z_Full_Meta_YT.json")]
        [DeploymentItem("2022-02-03T19-20-44Z_Full_Meta_YT.json")]
        [DeploymentItem("listOfProcessedFiles.list")]
        [TestMethod]
        public void TestGetFiles()
        {
            if (!Directory.Exists(WorkFolder))
            {
                Directory.CreateDirectory(WorkFolder);
            }

            var listOfProcessedFiles = Path.Combine(WorkFolder, "listOfProcessedFiles.list");
            var theList = FileHandling.FindNotYetProcessedYoutubeMetaFiles(listOfProcessedFiles,
                                                                           WorkFolder,
                                                                           VideoMetaDataFull.YoutubeSearchPattern);
            Assert.AreEqual(theList.Count, 1);
            CleanupFullMetaYoutubeFiles();
        }

        /// <summary>
        /// Checks method GetIdsOfFilesNotYetIncludedInFolder.
        /// </summary>
        [TestMethod]
        public void TestGetIdsOfFilesNotYetIncludedInFolder()
        {
            if (!Directory.Exists(WorkFolder))
            {
                Directory.CreateDirectory(WorkFolder);
            }
            var myListOfIds = new List<string>
                              {
                                  "11",
                                  "12",
                                  "13"
                              };

            myListOfIds.ForEach(id =>
                                {
                                    File.Create(Path.Combine(WorkFolder, $"{id}.video"));
                                });
            

            var newList = FileHandling.GetIdsOfFilesNotYetIncludedInFolder(myListOfIds, WorkFolder, "video");
            Assert.AreEqual(newList.Count, 0);

            myListOfIds.Add("14");
            myListOfIds.Add("15");
            newList = FileHandling.GetIdsOfFilesNotYetIncludedInFolder(myListOfIds, WorkFolder, "video");
            Assert.AreEqual(newList.Count, 2);

            myListOfIds.Clear();
            newList = FileHandling.GetIdsOfFilesNotYetIncludedInFolder(myListOfIds, WorkFolder, "video");
            Assert.AreEqual(newList.Count, 0);
        }


        /// <summary>
        /// CleanUp, Initialize and DeploymentItem do not work as expected. MS sucks, no exceptions. Therefore a separate method.
        /// </summary>
        public void CleanupFullMetaYoutubeFiles()
        {
            Directory
                .EnumerateFiles(WorkFolder)
                .Where(file => file.EndsWith(VideoMetaDataFull.YoutubeSearchPattern))
                .ToList()
                .ForEach(File.Delete);

            Directory
                .EnumerateFiles(WorkFolder)
                .Where(file => file.EndsWith("listOfProcessedFiles.list"))
                .ToList()
                .ForEach(File.Delete);
        }
    }
}
