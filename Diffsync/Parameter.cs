using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using FileElementNamespace;

namespace ParameterNamespace
{
    [Serializable]
    [XmlRoot("Parameter", IsNullable = true)]
    public class Parameter
    {
        // Variablen
        [XmlAttribute]
        string _path_complete_dir;
        [XmlAttribute]
        string _path_exchange_dir;
        [XmlAttribute]
        string _path_database_file;
        [XmlAttribute]
        string _file_hook;
        [XmlAttribute]
        DateTime _begin_from_sync;
        [XmlArray]
        List<FileElement> _all_files_complete_dir = new List<FileElement>();
        [XmlArray]
        List<FileElement> _all_files_exchange_dir = new List<FileElement>();
        [XmlArray]
        List<FileElement> _all_files_database = new List<FileElement>();
        [XmlArray]
        public List<string> DirectoryExceptions { get; set; }
        [XmlArray]
        public List<string> FileExtensionExceptions { get; set; }

        // Methoden und Funktionen
        public string PathCompleteDir
        {
            get {
                return _path_complete_dir;
            }
        }
        public string PathExchangeDir
        {
            get {
                return _path_exchange_dir;
            }
        }
        public DateTime BeginSyncFrom
        {
            get {
                return _begin_from_sync;
            }
        }
        public string DatabaseFile {
            get {
                return _path_database_file;
            }
        }

        public Parameter()
        {

        }

        public Parameter(string path_database_dir, string path_complete_dir, string path_exchange_dir, DateTime begin_sync_from)
        {
            this._path_complete_dir = AddBackslash(path_complete_dir);
            if (Directory.Exists(_path_complete_dir) == false) {
                Console.Error.WriteLine("Hauptverzeichnis {0} nicht gefunden!", _path_complete_dir);
                Console.WriteLine("Programm wird beendet. Bitte beliebige Taste drücken.");
                Console.ReadLine();
                Environment.Exit(0);
            }
            this._path_exchange_dir = AddBackslash(path_exchange_dir);
            if (Directory.Exists(_path_exchange_dir) == false) {
                Directory.CreateDirectory(_path_exchange_dir);
            }
            this._path_database_file = path_database_dir;
            this._begin_from_sync = begin_sync_from;

            DirectoryExceptions = new List<string>();
            FileExtensionExceptions = new List<string>();

            //File_extension_exceptions.Add("dsdel"); // Dateiendung für Löschen von Dateien mit diesem Programm (DiffSync DELete)
            string project_name = path_database_dir.Substring(path_database_dir.LastIndexOf('\\') + 1, path_database_dir.LastIndexOf('.') - path_database_dir.LastIndexOf('\\') - 1);
            this._file_hook = String.Format("{0}{1}.dshook", _path_exchange_dir, project_name); // DiffSyncHook
        }

        public void SetPathCompleteDir(string path)
        {
            _path_complete_dir = AddBackslash(path);
            foreach (FileElement file_element in this._all_files_database) {
                file_element.UpdateDirectory(_path_complete_dir, _path_complete_dir.Length);
            }
        }

        public void SetPathExchangeDir(string path)
        {
            string old_path = _path_exchange_dir;
            _path_exchange_dir = AddBackslash(path);

            _file_hook = _file_hook.Replace(old_path, _path_exchange_dir);

            foreach (FileElement file_element in this._all_files_exchange_dir) {
                file_element.UpdateDirectory(_path_exchange_dir, _path_exchange_dir.Length);
            }
        }
        public void SetDatabaseFile(string path)
        {
            _path_database_file = path;
        }

        public void GetAllFiles()
        {
            // alle Dateien des vollständigen Verzeichnisses einlesen
            _all_files_complete_dir.Clear();
            GetSubDirectoryFiles(_path_complete_dir, ref _all_files_complete_dir, _path_complete_dir.Length, true);

            // alle Dateien des Austausch-Verzeichnisses einlesen, falls das Austausch-Verzeichnis bereits existiert
            _all_files_exchange_dir.Clear();
            GetSubDirectoryFiles(_path_exchange_dir, ref _all_files_exchange_dir, _path_exchange_dir.Length, false);
        }

        void GetSubDirectoryFiles(string directory, ref List<FileElement> file_elements, int root_directory_path_length, bool complete_dir)
        {
            string[] sub_directories = Directory.GetDirectories(directory);

            SortStringsAscending(sub_directories);

            foreach (string sub_directory in sub_directories) {
                if (IsDirectoryUsable(sub_directory)) {
                    GetSubDirectoryFiles(sub_directory, ref file_elements, root_directory_path_length, complete_dir);
                }
            }

            // IsDirectoryUsable == true, da schon geprüft in vorheriger Ebene der Rekursion
            GetDirectoryFiles(directory, ref file_elements, root_directory_path_length, complete_dir);
        }

        void GetDirectoryFiles(string directory, ref List<FileElement> file_elements, int root_directory_path_length, bool complete_dir)
        {
            // To-Do Filter für verschiedene Dateiendungen einbauen
            string[] files = Directory.GetFiles(directory);

            SortStringsAscending(files);

            foreach (string file in files) {
                FileElement file_element = new FileElement(file, root_directory_path_length, complete_dir);
                file_elements.Add(file_element);
            }
        }

        public List<FileElement> GetFilesToSync()
        {
            if (!_all_files_database.Any()) {
                // noch keine Datenbank verfügbar, einfach nach Datum entscheiden
                return (GetFilesToSyncSinceDate());
            } else {
                // Datenbank verfügbar, einfach mit Datenbank vergleichen
                return (GetFilesToSyncDatabase());
            }
        }

        List<FileElement> GetFilesToSyncSinceDate()
        {
            List<FileElement> files_to_sync = new List<FileElement>();

            foreach (FileElement file_element in _all_files_complete_dir) {
                if (file_element.IsNewOrChanged(BeginSyncFrom)) {
                    files_to_sync.Add(file_element);
                }
            }

            // alle Dateien aus Austausch-Verzeichnis kopieren
            foreach (FileElement file_element in _all_files_exchange_dir) {
                files_to_sync.Add(file_element);
            }

            return (files_to_sync);
        }

        List<FileElement> GetFilesToSyncDatabase()
        {
            int index_database;
            List<FileElement> files_to_sync = new List<FileElement>();

            foreach (FileElement file_element in _all_files_complete_dir) {
                index_database = _all_files_database.FindIndex(x => x.Path == file_element.Path); // es wird auf exakt gleichen String geprüft
                if (index_database > -1) {
                    // Element in Datenbank enthalten, hat sich das Element geändert?
                    if (file_element.Size != _all_files_database[index_database].Size || file_element.DateCreated != _all_files_database[index_database].DateCreated || file_element.DateWritten != _all_files_database[index_database].DateWritten) {
                        files_to_sync.Add(file_element);
                    }
                } else {
                    // neues Element
                    files_to_sync.Add(file_element);
                }
            }

            foreach (FileElement file_element in _all_files_database) {
                if (!_all_files_complete_dir.Exists(x => x.Path == file_element.Path)) { // es wird auf exakt gleichen String geprüft
                    // Datei existiert nicht mehr im vollständigen Verzeichnis und wird als zu löschen markiert
                    file_element.SetToDelete();
                    files_to_sync.Add(file_element);
                }
            }

            // files_to_sync sortieren?
            files_to_sync.Sort((x, y) => String.Compare(x.Path, y.Path));

            // das Austausch-Verzeichnis ändert sich nicht in der Bearbeitung --> alle Dateien aus Austausch-Verzeichnis kopieren
            foreach (FileElement file_element in _all_files_exchange_dir) {
                files_to_sync.Add(file_element);
            }

            // Konflikte betrachten:
            // beide Dateien haben sich seit dem letzten Mal geändert
            int i = 0;
            while (i < files_to_sync.Count()) {
                bool conflict = false;
                int j = i + 1;
                while (j < files_to_sync.Count() && !conflict) {
                    if (files_to_sync[i].RelativePath == files_to_sync[j].RelativePath) {
                        // Konflikt der Dateien, auf beiden Seiten geändert
                        Console.WriteLine("");
                        Console.WriteLine("Konflikt zwischen folgenden Dateien:");
                        Console.WriteLine(String.Format("[1] {0,7:##0.000} MB | Erstellt: {1} | Geändert: {2} | {3}",
                            files_to_sync[i].Size,
                            files_to_sync[i].DateCreated.ToString("yyyy-MM-dd HH:mm"),
                            files_to_sync[i].DateWritten.ToString("yyyy-MM-dd HH:mm"),
                            files_to_sync[i].Path));
                        Console.WriteLine(String.Format("[2] {0,7:##0.000} MB | Erstellt: {1} | Geändert: {2} | {3}",
                            files_to_sync[j].Size,
                            files_to_sync[j].DateCreated.ToString("yyyy-MM-dd HH:mm"),
                            files_to_sync[j].DateWritten.ToString("yyyy-MM-dd HH:mm"),
                            files_to_sync[j].Path));
                        Console.WriteLine("Welche Datei soll verwendet werden (1/2)?");
                        bool false_input;
                        string input;
                        do {
                            input = Console.ReadLine();
                            if (input == "1" || input == "2") {
                                false_input = false;
                            } else {
                                false_input = true;
                            }
                        } while (false_input);
                        if (input == "1") {
                            files_to_sync.Remove(files_to_sync[j]);
                            Console.WriteLine("Datei 1 wird verwendet.");
                            conflict = true;
                        } else {
                            files_to_sync.Remove(files_to_sync[i]);
                            Console.WriteLine("Datei 2 wird verwendet.");
                            i = i - 1;
                            conflict = true;
                        }
                    }
                    j++;
                }
                i++;
            }

            return (files_to_sync);
        }

        public void PrepareSaveToDatabase()
        {
            // Speichern in Datenbank. Alte Datenbank löschen und durch aktuellen Stand überschreiben
            // es werden nur _all_files_database gespeichert, alles andere kann gelöscht werden
            GetAllFiles();
            _all_files_database.Clear();
            _all_files_database.AddRange(_all_files_complete_dir);
            _all_files_complete_dir.Clear();
            _all_files_exchange_dir.Clear();
        }

        public bool FileHookExists()
        {
            FileInfo file = new FileInfo(this._file_hook);
            return file.Exists;
        }

        public void DeleteOtherFileHook()
        {
            string[] all_files;
            all_files = Directory.GetFiles(this._path_exchange_dir, "*.dshook", SearchOption.TopDirectoryOnly);

            foreach (string file_element in all_files) {
                try {
                    File.Delete(file_element);
                } catch (IOException delete_error) {
                    Console.WriteLine("Die Datei {0} kann nicht gelöscht werden. Bitte manuell löschen", file_element);
                    Console.WriteLine(delete_error.Message);
                }
            }
        }

        public void SetFileHook()
        {
            FileInfo file = new FileInfo(this._file_hook);
            file.Create();
        }

        public void DeleteEmptyExchangeDirectories()
        {
            string[] sub_directories = Directory.GetDirectories(_path_exchange_dir);

            foreach (string sub_directory in sub_directories) {
                DeleteEmptySubDirectories(sub_directory);
            }
        }

        void DeleteEmptySubDirectories(string directory)
        {
            string[] sub_directories = Directory.GetDirectories(directory);

            foreach (string sub_directory in sub_directories) {
                DeleteEmptySubDirectories(sub_directory);
            }

            if (!Directory.EnumerateFileSystemEntries(directory).Any()) {
                try {
                    Directory.Delete(directory);
                } catch (IOException delete_error) {
                    Console.WriteLine(String.Format("Das Verzeichnis {0} kann nicht gelöscht werden. Bitte manuell löschen", directory));
                    Console.WriteLine(delete_error.Message);
                }
            }
        }

        void SortStringsAscending(string[] strings_to_be_sorted)
        {
            Array.Sort(strings_to_be_sorted, (x, y) => String.Compare(x, y));
        }

        bool IsDirectoryUsable(string directory)
        {
            foreach (string directory_exception in DirectoryExceptions) {
                if (directory.ToLower().Contains(directory_exception.TrimEnd('\\').ToLower())) {
                    // es wird ein Vergleich ohne Prüfung der Groß-Kleinschreibung durchgeführt
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
