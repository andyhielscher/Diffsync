using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using FileElementNamespace;

namespace ParameterNamespace
{
    public class Parameter
    {
        string _path_complete_dir;
        string _path_exchange_dir;
        List<FileElement> _all_files = new List<FileElement>();
        List<FileElement> _sync_files = new List<FileElement>();

        public string Path_complete_dir
        {
            get
            {
                return _path_complete_dir;
            }
            set
            {
                _path_complete_dir = AddBackslash(value);
            }
        }
        public string Path_exchange_dir
        {
            get
            {
                return _path_exchange_dir;
            }
            set
            {
                _path_exchange_dir = AddBackslash(value);
            }
        }
        public List<string> Directory_exceptions { get; set; }
        public DateTime Begin_sync_from { get; set; }
        public string Project_name { get; set; }

        public Parameter()
        {
            Directory_exceptions = new List<string>();
        }

        public void GetAllFiles()
        {
            if (Directory.Exists(_path_complete_dir) == false)
            {
                Console.Error.WriteLine("Hauptverzeichnis {0} nicht gefunden!", _path_complete_dir);
                return;
            }

            GetSubDirectoryFiles(_path_complete_dir);
        }

        public List<FileElement> GetFilesToCopy()
        {
            List<FileElement> files_to_copy = new List<FileElement>();

            foreach (FileElement file_element in _all_files)
            {
                if (file_element.IsNewOrChanged(Begin_sync_from))
                {
                    file_element.Will_be_copied = true;
                    files_to_copy.Add(file_element);
                }
            }

            return (files_to_copy);
        }

        public void CopyFilesToExchangeDir()
        {
            string new_path;
            int start_idx = _path_complete_dir.Length;
            FileInfo file;

            DirectoryCheckExistsAndCreate(_path_exchange_dir);

            foreach (FileElement file_element in _all_files)
            {
                if (file_element.Will_be_copied)
                {
                    new_path = String.Format("{0}{1}", _path_exchange_dir, file_element.Path.Substring(start_idx));
                    file = new FileInfo(new_path);
                    DirectoryCheckExistsAndCreate(file.DirectoryName);
                    file = new FileInfo(file_element.Path);
                    file.CopyTo(new_path);
                }
            }
        }

        void GetSubDirectoryFiles(string directory)
        {
            string[] sub_directories = Directory.GetDirectories(directory);

            SortStringsAscending(sub_directories);

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

            SortStringsAscending(files);

            foreach (string file in files)
            {
                FileElement file_element = new FileElement(file);
                _all_files.Add(file_element);
            }
        }

        void SortStringsAscending(string[] strings_to_be_sorted)
        {
            Array.Sort(strings_to_be_sorted, (x, y) => String.Compare(x, y));
        }

        void DirectoryCheckExistsAndCreate(string directory)
        {
            if (Directory.Exists(directory) == false)
            {
                Directory.CreateDirectory(directory);
            }
        }

        bool IsDirectoryUsable(string directory)
        {
            // To-Do: Falls eine Verzeichnis-Ausnahme aus mehr Ebenen besteht, muss der Code angepasst werden
            string[] single_folders = directory.Split('\\');

            foreach (string directory_exception in Directory_exceptions)
            {
                if (single_folders[single_folders.Length - 1].ToLower() == directory_exception.TrimEnd('\\').ToLower())
                {
                    return (false);
                }
            }
            return (true);
        }

        string AddBackslash(string val)
        {
            val = val.TrimEnd('\\');
            val = val + '\\';
            return val;
        }

    }
}
