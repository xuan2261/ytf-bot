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
            var listOfProcessedFiles = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "listOfProcessedFiles.list");
            var theList = FileHandling.FindNotYetProcessedYoutubeMetaFiles(listOfProcessedFiles, 
                                                                           AppDomain.CurrentDomain.BaseDirectory,
                                                                           searchPattern: "Full_Meta_YT.json");
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
                .Where(file => file.EndsWith("Full_Meta_YT.json"))
                .ToList()
                .ForEach(File.Delete);

            Directory
                .EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory)
                .Where(file => file.EndsWith(TelegramManager.PathToListOfProcessedFiles))
                .ToList()
                .ForEach(File.Delete);
        }
    }
}
