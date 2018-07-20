using System;
using System.Collections.Generic;
using System.IO;

namespace Datatypes
{
    public class FileElement
    {
        string Path;
        double Size;
        DateTime DateCreated;
        DateTime DateAccessed;
        DateTime DateWritten;

        public FileElement()
        {
        }

        public FileElement(string path)
        {
            this.Path = path;
            FileInfo file_info = new FileInfo(path);
            this.Size = file_info.Length;
            this.DateAccessed = file_info.LastAccessTime;
            this.DateCreated = file_info.CreationTime;
            this.DateWritten = file_info.LastWriteTime;
        }
    }

    public class Parameter
    {
        public string main_path;
        public List<string> directory_exceptions;
        public DateTime begin_sync_from;

        List<FileElement> all_files = new List<FileElement>();
        List<FileElement> sync_files = new List<FileElement>();

        public Parameter()
        {
            directory_exceptions = new List<string>();
        }

        public void GetAllFiles()
        {
            if (Directory.Exists(main_path) == false)
            {
                Console.Error.WriteLine("Hauptverzeichnis {0} nicht gefunden!", main_path);
                return;
            }

            GetSubDirectoryFiles(main_path);
        }

        void GetSubDirectoryFiles(string directory)
        {
            string[] sub_directories = Directory.GetDirectories(directory);
            SortStringAscending(sub_directories);
            foreach (string sub_directory in sub_directories)
            {
                if (IsDirectoryUsable(sub_directory))
                {
                    GetSubDirectoryFiles(sub_directory);
                    GetDirectoryFiles(sub_directory);
                }
            }
        }

        void GetDirectoryFiles(string directory)
        {
            string[] files = Directory.GetFiles(directory);
            SortStringAscending(files);
            foreach (string file in files)
            {
                FileElement file_element = new FileElement(file);
                all_files.Add(file_element);
            }
        }

        void SortStringAscending(string[] strings_to_be_sorted)
        {
            Array.Sort(strings_to_be_sorted, (x, y) => String.Compare(x, y));
        }

        bool IsDirectoryUsable(string directory)
        {
            // To-Do: Falls eine Verzeichnis-Ausnahme aus mehr Ebenen besteht, muss der Code angepasst werden
            string[] single_folders = directory.Split('\\');
            foreach (string directory_exception in directory_exceptions)
            {
                if (single_folders[single_folders.Length - 1].ToLower() == directory_exception.TrimEnd('\\').ToLower())
                {
                    return (false);
                }
            }
            return (true);
        }
    }
}
