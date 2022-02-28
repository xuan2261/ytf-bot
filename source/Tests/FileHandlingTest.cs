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
            var mySubDir = "theSubDir";
            CreateSubDirWithFiles(mySubDir, new List<string> { "11", "12", "13" });

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
        /// The method "FindNotYetProcessedVideoIdFiles" is probably somewhat computationally intensive.
        /// This test checks whether 500 elements can be processed loosely.
        /// </summary>
        [TestMethod]
        public void PerformanceTestFindNotYetProcessedVideoIdFiles()
        {
            SetupTest();
            var mySubDir = "theSubDir";
            List<string> myListOfFiles = new List<string>(500);
            var guidOfFirst = Guid.NewGuid().ToString();
            myListOfFiles.Add(guidOfFirst);
            for (int i = 1; i < 500; i++)
            {
                myListOfFiles.Add(Guid.NewGuid().ToString());
            }
            CreateSubDirWithFiles(mySubDir, myListOfFiles);


            var alreadyProcessed = Path.Combine(WorkFolder, mySubDir, $"{guidOfFirst}.video") + Environment.NewLine;
            var fileName = Path.Combine(WorkFolder, "listOfProcessedFiles.list");
            File.WriteAllText(fileName, alreadyProcessed);


            var theList = FileHandling.FindNotYetProcessedVideoIdFiles(fileName,
                                                                       WorkFolder,
                                                                       VideoMetaDataFull.VideoFileSearchPattern);
            Assert.AreEqual(theList.Count, 499);
        }

        /// <summary>
        /// Method 'FindNotYetProcessedVideoIdFiles' is called without the file with the processed filenames existing.
        /// </summary>
        [TestMethod]
        public void TestNotYetProcessedFilesIfThereIsNoList()
        {
            SetupTest();
            CreateSubDirWithFiles("theSubDir", new List<string> { "11", "12", "13" });

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
            var mySubDir = "theSubDir";
            CreateSubDirWithFiles(mySubDir, myListOfIds);

            var workSubDir = Path.Combine(WorkFolder, mySubDir);
            var newList = FileHandling.ReduceListOfIds(myListOfIds, workSubDir, VideoMetaDataFull.VideoFileSearchPattern);
            Assert.AreEqual(newList.Count, 0);

            // myListOfIds contains two ids that are not yet as a file in the folder
            myListOfIds.Add("14");
            myListOfIds.Add("15");
            newList = FileHandling.ReduceListOfIds(myListOfIds, workSubDir, VideoMetaDataFull.VideoFileSearchPattern);
            Assert.AreEqual(newList.Count, 2);

            // myListOfIds is empty => no results in newList
            myListOfIds.Clear();
            newList = FileHandling.ReduceListOfIds(myListOfIds, workSubDir, VideoMetaDataFull.VideoFileSearchPattern);
            Assert.AreEqual(newList.Count, 0);
        }

        /// <summary>
        /// Test creates 2 subfolders with 3 files each. The method "FindVideofilesInSubfolder" must then find 6 files.
        /// </summary>
        [TestMethod]
        public void FindVideoIdFilesInSubfoldersTest()
        {
            SetupTest();
            CreateSubDirWithFiles("theSubDir1", new List<string> { "11", "12", "13" });
            CreateSubDirWithFiles("theSubDir2", new List<string> { "14", "15", "16" });

            var theList = FileHandling.FindVideoIdFilesInSubfolders(WorkFolder, VideoMetaDataFull.VideoFileSearchPattern);
            Assert.AreEqual(theList.Count, 6);
        }

        /// <summary>
        /// Creates a subdir within the files inside this list of filenames. File extension is VideoMetaDataFull.VideoFileSearchPattern.
        /// </summary>
        /// <param name="subDir"></param>
        /// <param name="filesToCreate"></param>
        private void CreateSubDirWithFiles(string subDir, List<string> filesToCreate)
        {
            Directory.CreateDirectory(Path.Combine(WorkFolder, subDir));
            filesToCreate.ForEach(id =>
                                  {
                                      File.WriteAllText(Path.Combine(WorkFolder, subDir, $"{id}.{VideoMetaDataFull.VideoFileSearchPattern}"), $"Content egal {id}");
                                  });
        }
    }
}
