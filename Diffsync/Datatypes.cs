using System;
using System.Collections.Generic;
using System.IO;

namespace Datatypes
{
    public class FileElement
    {
        public string Path {
            get
            {
                const int num_letters = 80;
                if (this._path.Length < num_letters)
                {
                    return (String.Format("{0,-" + num_letters + "}", this._path));
                } else
                {
                    string[] parts = this._path.Split('\\');
                    string path_shortend = "";
                    int path_shortend_length;

                    path_shortend_length = parts[parts.Length - 1].Length;
                    if (path_shortend_length < num_letters)
                    {
                        int i = 0;
                        do
                        {
                            if (parts[i].Length < num_letters - path_shortend_length - 4)
                            {
                                path_shortend = String.Format("{0}{1}\\", path_shortend, parts[i]);
                                path_shortend_length += parts[i].Length + 1;
                            }
                            else
                            {
                                path_shortend = String.Format("{0}{1}...\\", path_shortend, parts[i].Substring(0, num_letters - path_shortend_length - 4));
                                break;
                            }
                            i++;
                        } while (i < parts.Length-1);
                        path_shortend = String.Format("{0}{1}", path_shortend, parts[parts.Length - 1]);
                    } else
                    {
                        path_shortend = String.Format("{0}...{1}", parts[parts.Length - 1].Substring(0, num_letters / 2 - 2), parts[parts.Length - 1].Substring(path_shortend_length - num_letters / 2 + 1));
                    }
                    return (path_shortend);
                }
            }
        }
        public double Size {
            get
            {
                return (this._size);
            }
        }
        public DateTime Date_created {
            get
            {
                return (this._date_created);
            }
        }
        /* This method may return an inaccurate value, because it uses native functions whose values may not be continuously updated by the operating system.
         * https://msdn.microsoft.com/en-us/library/system.io.filesysteminfo.lastaccesstime(v=vs.110).aspx */
        //DateTime DateAccessed; 
        public DateTime Date_written {
            get
            {
                return (this._date_written);
            }
        }

        string _path;
        double _size;
        DateTime _date_created;
        DateTime _date_written;

        public FileElement()
        {
        }

        public FileElement(string path)
        {
            this._path = path;
            FileInfo file_info = new FileInfo(path);
            this._size = file_info.Length;
            //this.DateAccessed = file_info.LastAccessTime;
            this._date_created = file_info.CreationTime;
            this._date_written = file_info.LastWriteTime;
        }

        public bool IsNewOrChanged(DateTime new_datetime)
        {
            if (this.Date_created > new_datetime || this.Date_written > new_datetime)
            {
                return (true);
            } else
            {
                return (false);
            }
        }
    }

    public class Parameter
    {
        public string Main_path { get; set; }
        public List<string> Directory_exceptions { get; set; }
        public DateTime Begin_sync_from { get; set; }
        public string Project_name { get; set; }

        List<FileElement> _all_files = new List<FileElement>();
        List<FileElement> _sync_files = new List<FileElement>();

        public Parameter()
        {
            Directory_exceptions = new List<string>();
        }

        public void GetAllFiles()
        {
            if (Directory.Exists(Main_path) == false)
            {
                Console.Error.WriteLine("Hauptverzeichnis {0} nicht gefunden!", Main_path);
                return;
            }

            GetSubDirectoryFiles(Main_path);
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
                _all_files.Add(file_element);
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

            foreach (string directory_exception in Directory_exceptions)
            {
                if (single_folders[single_folders.Length - 1].ToLower() == directory_exception.TrimEnd('\\').ToLower())
                {
                    return (false);
                }
            }
            return (true);
        }

        public List<FileElement> GetFilesToCopy()
        {
            List<FileElement> files_to_copy = new List<FileElement>();

            foreach (FileElement file_element in _all_files)
            {
                if (file_element.IsNewOrChanged(Begin_sync_from))
                {
                    files_to_copy.Add(file_element);
                }
            }

            return (files_to_copy);
        }
    }
}
