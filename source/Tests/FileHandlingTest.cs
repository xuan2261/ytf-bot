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

        /// <summary>
        /// Delete all content of working directory. 
        /// </summary>
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

                foreach (var directory in di.EnumerateDirectories())
                {
                    Directory.Delete(directory.FullName, true);
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
        /// This test creates a subfolder with 3 files and a list of processed file names containing two of the three created files.
        /// The method 'FindNotYetProcessedVideoIdFiles' method then returns only one item as a result.
        /// </summary>
        [TestMethod]
        public void TestFindNotYetProcessedVideoIdFiles()
        {
            SetupTest();
            var myListOfIds = new List<string> { "11", "12", "13" };
            var mySubDir = "theSubDir";
            Directory.CreateDirectory(Path.Combine(WorkFolder, mySubDir));

            myListOfIds.ForEach(id =>
                                {
                                    File.WriteAllText(Path.Combine(WorkFolder, mySubDir, $"{id}.video"), $"Content egal {id}");
                                });

            var alreadyProcessed = Path.Combine(WorkFolder, mySubDir, "11.video") + Environment.NewLine + Path.Combine(WorkFolder, mySubDir, "12.video");
            var fileName = Path.Combine(WorkFolder, "listOfProcessedFiles.list");
            File.WriteAllText(fileName, alreadyProcessed);

           
            var theList = FileHandling.FindNotYetProcessedVideoIdFiles(fileName,
                                                                       WorkFolder,
                                                                       VideoMetaDataFull.VideoFileSearchPattern);
            Assert.AreEqual(theList.Count, 1);
            Assert.IsTrue(theList[0].Contains("13.video"));
        }

        /// <summary>
        /// Method 'FindNotYetProcessedVideoIdFiles' is called without the file with the processed filenames existing.
        /// </summary>
        [TestMethod]
        public void TestNotYetProcessedFilesIfThereIsNoList()
        {
            SetupTest();
            var myListOfIds = new List<string> { "11", "12", "13" };
            var mySubDir = "theSubDir";
            Directory.CreateDirectory(Path.Combine(WorkFolder, mySubDir));
            myListOfIds.ForEach(id =>
                                {
                                    File.WriteAllText(Path.Combine(WorkFolder, mySubDir, $"{id}.video"), $"Content egal {id}");
                                });
            var fileName = Path.Combine(WorkFolder, "listOfProcessedFiles.list");
            var theList = FileHandling.FindNotYetProcessedVideoIdFiles(fileName,
                                                                       WorkFolder,
                                                                       VideoMetaDataFull.VideoFileSearchPattern);

            Assert.AreEqual(theList.Count, 0);
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

        /// <summary>
        /// Test creates 2 subfolders with 3 files each. The method "FindVideofilesInSubfolder" must then find 6 files.
        /// </summary>
        [TestMethod]
        public void FindVideoIdFilesInSubfoldersTest()
        {
            void CreateSubDir(string s, List<string> list)
            {
                Directory.CreateDirectory(Path.Combine(WorkFolder, s));

                list.ForEach(id => { File.WriteAllText(Path.Combine(WorkFolder, s, $"{id}.video"), $"Content egal {id}"); });
            }

            SetupTest();
            CreateSubDir("theSubDir1", new List<string> { "11", "12", "13" });
            CreateSubDir("theSubDir2", new List<string> { "14", "15", "16" });

            var theList = FileHandling.FindVideoIdFilesInSubfolders(WorkFolder, VideoMetaDataFull.VideoFileSearchPattern);
            Assert.AreEqual(theList.Count, 6);
        }
    }
}
