namespace Common
{
    public static class FileHandling
    {
        /// <summary>
        /// Method finds the youtube meta video files that are not yet processed.
        /// Files with a freely definable content exist in the current directory. The file names of these files end with
        /// "searchPattern". It is assumed that these files are somehow processed and the already processed files are located as a
        /// string in the text file "pathToListOfProcessedFileNames".This method creates a list of file names that have not yet been
        /// processed and are therefore not in the file "pathToListOfProcessedFileNames".
        /// </summary>
        /// <returns>Returns a list within the full path to youtube meta files that are not processed yet.</returns>
        public static List<string> FindNotYetProcessedYoutubeMetaFiles(string pathToListOfProcessedFileNames, string folderToSearchIn, string searchPattern)
        {
            var availableYoutubeMetaFiles = Directory.EnumerateFiles(folderToSearchIn)
                                                     .Where(file => file.EndsWith(searchPattern))
                                                     .ToList();
            var notYetProcessedFileNames = new List<string>();
            if (File.Exists(pathToListOfProcessedFileNames))
            {
                var listOfProcessedYoutubeFiles = File.ReadAllLines(pathToListOfProcessedFileNames).ToList();
                availableYoutubeMetaFiles.ForEach(fullPathToFile =>
                {
                    if (!listOfProcessedYoutubeFiles.Contains(fullPathToFile))
                    {
                        notYetProcessedFileNames.Add(fullPathToFile);
                    }
                });
            }
            else
            {
                // If there is not yet a file that logs which youtube meta files have been processed, a list of the names of all the
                // youtube meta files that can be found is returned.
                return availableYoutubeMetaFiles;
            }
            return notYetProcessedFileNames;
        }

        /// <summary>
        /// This method writes the list of the names of the files that have been processed to the log file pathToListOfProcessedFiles.
        /// </summary>
        /// <param name="pathToListOfProcessedFiles"></param>
        /// <param name="newProcessedFiles"></param>
        public static void WriteProcessedFileNamesIntoListOfProcessedFiles(string pathToListOfProcessedFiles, List<string> newProcessedFiles)
        {
            File.AppendAllLines(pathToListOfProcessedFiles, newProcessedFiles);
        }

        /// <summary>
        /// Call method to trim the file within the list of file names that are processed already.
        /// Yes, this is so inefficient.
        /// </summary>
        /// <param name="pathToListOfProcessedFiles">Full path to file</param>
        /// <param name="maxEntries">Maximum file names listed in this file</param>
        public static void TrimFileListOfProcessedFile(string pathToListOfProcessedFiles, int maxEntries)
        {
            var lines = File.ReadAllLines(pathToListOfProcessedFiles);
            if (lines.Length > maxEntries)
            {
                var countToSkip = lines.Length - maxEntries;
                File.WriteAllLines(pathToListOfProcessedFiles, lines.Skip(countToSkip).ToArray());
            }
        }

        /// <summary>
        /// This method searches the directory "folderToSearchIn" for files that satisfy the pattern "searchPattern" and deletes found files
        /// as soon as more than "maximumFiles" exist.
        /// The files are sorted by creation date and the oldest files are deleted. 
        /// </summary>
        /// <param name="folderToSearchIn">Folder to search in</param>
        /// <param name="searchPattern">Search pattern</param>
        /// <param name="maximumFiles">maximum files</param>
        /// <returns></returns>
        public static void RollingFileUpdater(string folderToSearchIn, string searchPattern, int maximumFiles)
        {
            DirectoryInfo info = new DirectoryInfo(folderToSearchIn);
            FileInfo[] files = info.GetFiles().Where(file => file.Name.Contains(searchPattern))
                                              .OrderBy(p => p.CreationTime)
                                              .ToArray();

            for (int i = 0; i < files.Length-maximumFiles; i++)
            {
                File.Delete(files[i].FullName);
            }
        }
    }
}
