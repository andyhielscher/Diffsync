using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using FileElementNamespace;

namespace ParameterNamespace
{
    [Serializable]
    public class Parameter
    {
        string _path_complete_dir;
        string _path_exchange_dir;
        List<FileElement> _all_files_complete_dir;
        List<FileElement> _all_files_exchange_dir = new List<FileElement>();
        List<FileElement> _all_files_database = new List<FileElement>();
        bool _database_used_to_sync;

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
        public List<string> File_extension_exceptions { get; set; }
        public DateTime Begin_sync_from { get; set; }
        public string Project_name { get; set; }
        public bool Is_database_used_to_sync
        {
            get
            {
                return _database_used_to_sync;
            }
        }

        public Parameter()
        {
            Directory_exceptions = new List<string>();
            File_extension_exceptions = new List<string>();
            File_extension_exceptions.Add("dsdel"); // Dateiendung für Löschen von Dateien mit diesem Programm (DiffSync DELete)
        }

        public void GetAllFiles()
        {
            // alle Dateien des vollständigen Verzeichnisses einlesen
            _all_files_complete_dir = new List<FileElement>();
            if (Directory.Exists(_path_complete_dir) == false)
            {
                Console.Error.WriteLine("Hauptverzeichnis {0} nicht gefunden!", _path_complete_dir);
                return;
            }
            GetSubDirectoryFiles(_path_complete_dir, ref _all_files_complete_dir, _path_complete_dir.Length);

            // alle Dateien des Austausch-Verzeichnisses einlesen, falls das Austausch-Verzeichnis bereits existiert
            _all_files_exchange_dir = new List<FileElement>();
            if (Directory.Exists(_path_complete_dir) == true)
            {
                GetSubDirectoryFiles(_path_exchange_dir, ref _all_files_exchange_dir, _path_exchange_dir.Length);
            }
        }
        void GetSubDirectoryFiles(string directory, ref List<FileElement> file_elements, int root_directory_path_length)
        {
            string[] sub_directories = Directory.GetDirectories(directory);

            SortStringsAscending(sub_directories);

            foreach (string sub_directory in sub_directories)
            {
                if (IsDirectoryUsable(sub_directory))
                {
                    GetSubDirectoryFiles(sub_directory, ref file_elements, root_directory_path_length);
                    GetDirectoryFiles(sub_directory, ref file_elements, root_directory_path_length);
                }
            }
        }

        void GetDirectoryFiles(string directory, ref List<FileElement> file_elements, int root_directory_path_length)
        {
            // To-Do Filter für verschiedene Dateiendungen einbauen
            string[] files = Directory.GetFiles(directory);

            SortStringsAscending(files);

            foreach (string file in files)
            {
                FileElement file_element = new FileElement(file, root_directory_path_length);
                file_elements.Add(file_element);
            }
        }

        public List<FileElement> GetFilesToCopyToExchangeDir()
        {
            if (!_all_files_database.Any())
            {
                // noch keine Datenbank verfügbar, einfach nach Datum entscheiden
                return (GetFilesToCopySinceDate());
            } else
            {
                // Datenbank verfügbar, einfach mit Datenbank vergleichen
                return (GetFilesToCopyDatabase());
            }
        }

        List<FileElement> GetFilesToCopySinceDate()
        {
            List<FileElement> files_to_copy = new List<FileElement>();

            foreach (FileElement file_element in _all_files_complete_dir)
            {
                if (file_element.IsNewOrChanged(Begin_sync_from))
                {
                    file_element.Will_be_copied = true;
                    files_to_copy.Add(file_element);
                }
            }

            return (files_to_copy);
        }

        List<FileElement> GetFilesToCopyDatabase()
        {
            List<FileElement> files_to_copy = new List<FileElement>();

            return (files_to_copy);
        }

        public string FileHook()
        {
            return String.Format("{0}{1}.dshook", _path_exchange_dir, Project_name); // DiffSyncHook
        }

        public void PrepareSaveToDatabase()
        {
            // Speichern in Datenbank. Alte Datenbank löschen und durch aktuellen Stand überschreiben
            // es werden nur _all_files_database gespeichert, alles andere kann gelöscht werden
            GetAllFiles();
            _all_files_database = _all_files_complete_dir;
            _all_files_complete_dir.Clear();
            _all_files_exchange_dir.Clear();
            _database_used_to_sync = true;
        }

        void SortStringsAscending(string[] strings_to_be_sorted)
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

        string AddBackslash(string val)
        {
            val = val.TrimEnd('\\');
            val = val + '\\';
            return val;
        }

    }
}
