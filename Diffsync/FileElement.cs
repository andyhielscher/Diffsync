using System;
using System.Collections.Generic;
using System.IO;

namespace FileElementNamespace
{
    [Serializable]
    public class FileElement
    {
        string _path;
        double _size; // in Byte
        DateTime _date_created;
        DateTime _date_written;
        int _root_directory_path_length;

        public string Path
        {
            get
            {
                return (this._path);
            }
        }
        public string Relative_path
        {
            get
            {
                return (_path.Substring(_root_directory_path_length));
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
        public bool Will_be_copied { get; set; }
        //public bool Conflict { get; set; }

        public FileElement(string path, int root_directory_path_length)
        {
            this._path = path;
            FileInfo file_info = new FileInfo(path);
            this._size = file_info.Length;
            //this.DateAccessed = file_info.LastAccessTime;
            this._date_created = file_info.CreationTime;
            this._date_written = file_info.LastWriteTime;
            this._root_directory_path_length = root_directory_path_length;
            this.Will_be_copied = false;

        }

        public string PathToPrint(int num_letters)
        {
            if (this._path.Length < num_letters)
            {
                return (String.Format("{0,-" + num_letters + "}", this._path));
            }
            else
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
                    } while (i < parts.Length - 1);
                    path_shortend = String.Format("{0}{1}", path_shortend, parts[parts.Length - 1]);
                }
                else
                {
                    path_shortend = String.Format("{0}...{1}", parts[parts.Length - 1].Substring(0, num_letters / 2 - 2), parts[parts.Length - 1].Substring(path_shortend_length - num_letters / 2 + 1));
                }
                return (path_shortend);
            }
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
}
