using System;
using System.IO;
using System.Runtime.Serialization;

namespace FileElementNamespace
{
    [Serializable]
    [DataContract()]
    public class FileElement
    {
        // Variablen
        [DataMember]
        string _path;
        [DataMember]
        double _size; // in Byte
        [DataMember]
        DateTime _date_created;
        [DataMember]
        DateTime _date_written;
        [DataMember]
        int _root_directory_path_length;
        [DataMember]
        bool _will_be_copied;
        [DataMember]
        bool _will_be_deleted;
        [DataMember]
        bool _from_complete_dir;

        // Methoden und Funktionen
        public string Path
        {
            get {
                return (this._path);
            }
        }
        public string RelativePath
        {
            get {
                // exklusive vorgestelltem "\"
                return (_path.Substring(_root_directory_path_length));
            }
        }
        public double Size
        {
            get {
                return (this._size);
            }
        }
        public DateTime DateCreated
        {
            get {
                return (this._date_created);
            }
        }
        /* This method may return an inaccurate value, because it uses native functions whose values may not be continuously updated by the operating system.
         * https://msdn.microsoft.com/en-us/library/system.io.filesysteminfo.lastaccesstime(v=vs.110).aspx */
        //DateTime DateAccessed; 
        public DateTime DateWritten
        {
            get {
                return (this._date_written);
            }
        }
        public bool FromCompleteDir
        {
            get {
                return (this._from_complete_dir);
            }
        }
        public bool WillBeCopied
        {
            get {
                return (this._will_be_copied);
            }
        }
        public bool WillBeDeleted
        {
            get {
                return (this._will_be_deleted);
            }
        }

        public FileElement(string path, int root_directory_path_length, bool complete_dir)
        {
            this._path = path;
            FileInfo file_info = new FileInfo(path);
            this._size = (double)file_info.Length / 1024 / 1024; // in MB
            //this.DateAccessed = file_info.LastAccessTime;
            this._date_created = file_info.CreationTime;
            this._date_written = file_info.LastWriteTime;
            this._root_directory_path_length = root_directory_path_length;
            this._from_complete_dir = complete_dir;
            if (path.EndsWith(".dsdel")) {
                this._will_be_deleted = true;
            }
        }

        public void UpdateDirectory(string path, int root_directory_path_length)
        {
            this._path = String.Format("{0}{1}", path, this.RelativePath);
            this._root_directory_path_length = root_directory_path_length;
        }

        public string PathToPrint(int num_letters)
        {
            return (PreparePathToPrint(this._path, num_letters));
        }

        public string RelativePathToPrint(int num_letters)
        {
            return (PreparePathToPrint(this.RelativePath, num_letters));
        }

        string PreparePathToPrint(string path, int num_letters)
        {
            if (path.Length < num_letters) {
                return (String.Format("{0,-" + num_letters + "}", path));
            } else {
                string[] parts = path.Split('\\');
                string path_shortend = "";
                int path_shortend_length;

                path_shortend_length = parts[parts.Length - 1].Length;
                if (path_shortend_length < num_letters - 4) {
                    int i = 0;
                    do {
                        if (parts[i].Length < num_letters - path_shortend_length - 4) {
                            path_shortend = String.Format("{0}{1}\\", path_shortend, parts[i]);
                            path_shortend_length += parts[i].Length + 1;
                        } else {
                            path_shortend = String.Format("{0}{1}...\\", path_shortend, parts[i].Substring(0, num_letters - path_shortend_length - 4));
                            break;
                        }
                        i++;
                    } while (i < parts.Length - 1);
                    path_shortend = String.Format("{0}{1}", path_shortend, parts[parts.Length - 1]);
                } else {
                    if (path_shortend_length < num_letters - 2) {
                        path_shortend = String.Format("..\\{0}", parts[parts.Length - 1]);
                        for (int i = path_shortend_length + 3; i < num_letters; i++) {
                            path_shortend = String.Format("{0} ", path_shortend);
                        }
                    } else {
                        double result = Convert.ToDouble(num_letters - 6) / 2;
                        int substring1_length = Convert.ToInt16(Math.Floor(result));
                        int substring2_start = path_shortend_length - (num_letters - 6 - substring1_length);
                        path_shortend = String.Format("..\\{0}...{1}", parts[parts.Length - 1].Substring(0, substring1_length), parts[parts.Length - 1].Substring(substring2_start));
                    }
                }
                return (path_shortend);
            }
        }

        public bool IsNewOrChanged(DateTime new_datetime)
        {
            if (this.DateCreated > new_datetime || this.DateWritten > new_datetime) {
                this._will_be_copied = true;
                return (true);
            } else {
                return (false);
            }
        }

        public void SetToDelete()
        {
            this._will_be_deleted = true;
        }
    }
}
