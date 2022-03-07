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
        public static void SetupTest(string workingFolder)
        {
            if (Directory.Exists(workingFolder))
            {
                // Clean dir
                var di = new DirectoryInfo(workingFolder);
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
                Directory.CreateDirectory(workingFolder);
            }
        }

        /// <summary>
        /// Creates amount of video files in test subfolders.
        /// The time stamp in the video files is created as follows:
        /// i:=0 -> UtcNow
        /// i:=1 -> Utc.Now - 1 minute
        /// i:=t -> Utc.Now - t seconds
        ///
        /// The bigger 't' resp. 'i' the older the video.
        /// </summary>
        /// <param name="amountOfFiles">Isch klar oder.</param>
        /// <param name="subDirRespChannelId">Subdirectory</param>
        public static void CreateVideoMetaFiles(int amountOfFiles, string subDirRespChannelId, string workingFolder)
        {
            const string NotRelevant = "not relevant for Test";
            var videoMetaData = new VideoMetaDataFull
                                {
                                    ChannelId = subDirRespChannelId,
                                    ChannelTitle = NotRelevant,
                                    Id = "ytThing",
                                    DescriptionBase64 = NotRelevant,
                                    Title = NotRelevant,
                                    TitleBase64 = NotRelevant
                                };

            for (var i = 0; i < amountOfFiles; i++)
            {
                videoMetaData.PublishedAtRaw = DateTime.UtcNow - TimeSpan.FromMinutes(i);
                videoMetaData.Id = MakeVideoId(i);
                VideoMetaDataFull.SerializeToFileInSubfolder(videoMetaData, workingFolder);
            }
        }

        /// <summary>
        /// Method create a list of video ids.
        /// Ex.: amountOfIds = 3
        /// - ytThing_00.video
        /// - ytThing_01.video
        /// - ytThing_02.video
        /// </summary>
        /// <param name="amountOfIds">Count of Ids To be created</param>
        public static List<string> CreateListOfIds(int amountOfIds)
        {
            var listOfIds = new List<string>();
            for (var i = 0; i < amountOfIds; i++)
            {
                listOfIds.Add(MakeVideoId(i));
            }

            return listOfIds;
        }

        /// <summary>
        /// Creates a video id. In this class video ids look like:
        /// - ytThing_00.video
        /// - ytThing_01.video
        /// </summary>
        /// <param name="number">numerator</param>
        /// <returns></returns>
        public static string MakeVideoId(int number)
        {
            return $"ytThing_{number:D2}";
        }

        [TestMethod]
        public void TestTrimFile()
        {
            SetupTest(WorkFolder);

            const string BimsATestFile = "testFile.txt";
            for (var i = 0; i < 56; i++)
            {
                File.AppendAllText(BimsATestFile, $"TestEntry {i:D2} {Environment.NewLine}");
            }
            FileHandling.TrimFileListOfProcessedFile(BimsATestFile, 54);
            Assert.AreEqual(File.ReadAllLines(BimsATestFile).Length, 54);
        }

        /// <summary>
        /// Create a few files and tests the rolling file updater.
        /// </summary>
        [TestMethod]
        public void TestRollingFileUpdater()
        {
            SetupTest(WorkFolder);
            var mySubDir = "subDir_RollingFileTester";
            var subDirFullPath = Path.Combine(WorkFolder, mySubDir);
            CreateVideoMetaFiles(15, mySubDir, WorkFolder);

            FileHandling.RollingFileUpdater(subDirFullPath, VideoMetaDataFull.VideoFileSearchPattern, 10);
            var files = Directory.EnumerateFiles(subDirFullPath)
                                 .Where(file => file.Contains(VideoMetaDataFull.VideoFileSearchPattern))
                                 .ToList();
            Assert.IsTrue(files.Count == 10);

            FileHandling.RollingFileUpdater(subDirFullPath, VideoMetaDataFull.VideoFileSearchPattern, 2);
            files = Directory.EnumerateFiles(subDirFullPath)
                             .Where(file => file.Contains(VideoMetaDataFull.VideoFileSearchPattern))
                             .ToList();
            Assert.IsTrue(files.Count == 2);

            FileHandling.RollingFileUpdater(subDirFullPath, VideoMetaDataFull.VideoFileSearchPattern, 0);
            files = Directory.EnumerateFiles(subDirFullPath)
                             .Where(file => file.Contains(VideoMetaDataFull.VideoFileSearchPattern))
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
            SetupTest(WorkFolder);
            var mySubDir = "theSubDir";
            CreateVideoMetaFiles(3, mySubDir, WorkFolder);

            var alreadyProcessed = Path.Combine(WorkFolder, mySubDir, $"{MakeVideoId(0)}.video") + Environment.NewLine;
            alreadyProcessed += Path.Combine(WorkFolder, mySubDir, $"{MakeVideoId(1)}.video") +Environment.NewLine;
            var fileName = Path.Combine(WorkFolder, "listOfProcessedFiles.list");
            File.WriteAllText(fileName, alreadyProcessed);

            var theList = FileHandling.FindNotYetProcessedVideoIdFiles(fileName,
                                                                       WorkFolder,
                                                                       VideoMetaDataFull.VideoFileSearchPattern);
            Assert.AreEqual(theList.Count, 1);
            Assert.IsTrue(theList[0].Contains("02.video"));
        }

        /// <summary>
        /// The method "FindNotYetProcessedVideoIdFiles" is probably somewhat computationally intensive.
        /// This test checks whether 500 elements can be processed loosely.
        /// </summary>
        [TestMethod]
        public void PerformanceTestFindNotYetProcessedVideoIdFiles()
        {
            SetupTest(WorkFolder);
            var mySubDir = "theSubDir";
            var myListOfFiles = CreateListOfIds(500);
            CreateVideoMetaFiles(500, mySubDir, WorkFolder);

            var alreadyProcessed = Path.Combine(WorkFolder, mySubDir, $"{MakeVideoId(0)}.video") + Environment.NewLine;
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
            SetupTest(WorkFolder);
            CreateVideoMetaFiles(3, "theSubDir1", WorkFolder);

            var fileName = Path.Combine(WorkFolder, "listOfProcessedFiles.list");
            var theList = FileHandling.FindNotYetProcessedVideoIdFiles(fileName,
                                                                       WorkFolder,
                                                                       VideoMetaDataFull.VideoFileSearchPattern);
            Assert.AreEqual(theList.Count, 0);
            var lineCount = File.ReadLines(fileName).Count();
            Assert.AreEqual(lineCount,3);
        }

        /// <summary>
        /// Checks method ReduceListOfIds.
        /// </summary>
        [TestMethod]
        public void TestGetIdsOfFilesNotYetIncludedInFolder()
        {
            SetupTest(WorkFolder);
            var mySubDir = "theSubDir";
            CreateVideoMetaFiles(3, mySubDir, WorkFolder);
            var myListOfIds = CreateListOfIds(3);

            var workSubDir = Path.Combine(WorkFolder, mySubDir);
            var newList = FileHandling.ReduceListOfIds(myListOfIds, workSubDir, VideoMetaDataFull.VideoFileSearchPattern);
            Assert.AreEqual(newList.Count, 0);

            // myListOfIds contains two ids that are not yet as a file in the folder
            myListOfIds.Add(MakeVideoId(12));
            myListOfIds.Add(MakeVideoId(13));
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
            SetupTest(WorkFolder);
            CreateVideoMetaFiles(3, "theSubDir1", WorkFolder);
            CreateVideoMetaFiles(3, "theSubDir2", WorkFolder);

            var theList = FileHandling.FindVideoIdFilesInSubfolders(WorkFolder, VideoMetaDataFull.VideoFileSearchPattern);
            Assert.AreEqual(theList.Count, 6);
        }
    }
}
