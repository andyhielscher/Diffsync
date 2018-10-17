using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileElementNamespace;
using ParameterNamespace;
using System.IO;

namespace Diffsync
{
    class Program
    {
        static void Main(string[] args)
        {
            Parameter parameter = new Parameter();

            // Console: Breite einstellen
            Console.WindowWidth = 140;
            // TO-DO: Wie kann man die gespeicherte Anzahl Zeilen erhöhen? Damit kann dann die Liste der zu kopierenden Dateien durchgescrollt werden...

            // Arbeitsvariablen (aktuell hardgecodet):
            parameter.Path_complete_dir = @"C:\Users\AndreasHielscher\OneDrive - SmartSim GmbH\Dokumente\";
            parameter.Path_exchange_dir = @"C:\Users\AndreasHielscher\test";
            parameter.Directory_exceptions.Add("Benutzerdefinierte Office-Vorlagen");
            parameter.Directory_exceptions.Add("EON");
            parameter.Begin_sync_from = new DateTime(2018, 8, 20, 0, 0, 0);
            parameter.Project_name = "Sync Andis PC";

            // Filehook überprüfen; falls Datei mit Namen == Project_name existiert, kann nicht nochmal kopiert werden
            if (FileHookExists(ref parameter))
            {
                // Programm wurde schon ausgeführt und kann nicht noch einmal gestartet werden
                Console.WriteLine("Das Programm wurde bereits ausgeführt. Es muss auf die Ausführung der anderen Seite gewartet werden, bis das Programm erneut ausgeführt werden kann.");
                Console.WriteLine("Zum Beenden beliebige Taste drücken.");
                Console.ReadLine();
                Environment.Exit(0);
            } else
            {
                // Programm wurde auf der Gegenseite schon ausgeführt, der FileHook des anderen Projekts wird gelöscht
                DeleteOtherFileHook(ref parameter);
            }

            // Verzeichnis einlesen
            parameter.GetAllFiles();

            // Synchronisierung wird fallweise anders durchgeführt
            if (parameter.Is_database_used_to_sync)
            {
                // to do
            } else
            {
                // Dateien auflisten, welche kopiert werden sollen
                List<FileElement> files_to_copy = parameter.GetFilesToCopyToExchangeDir();

                // Dateien ausgeben, welche kopiert werden
                PrintFilesToCopy(ref files_to_copy);

                // Auf Bestätigung des Users zum weiteren Programmablauf warten
                if (UserWantsToContinue() == false)
                {
                    Environment.Exit(0);
                }

                // Alle zu kopierenden Dateien kopieren
                CopyFiles(ref files_to_copy, parameter.Path_exchange_dir, parameter.Path_complete_dir);
            }

            // Datenbank speichern
            parameter.PrepareSaveToDatabase();
            BinarySerialization.WriteToBinaryFile<Parameter>(String.Format("{0}\\{1}.dsdb", Environment.CurrentDirectory, parameter.Project_name), parameter); // Extension = DiffSync DataBase

            // Filehook setzen
            SetFileHook(ref parameter);

            // Datenbank laden (zum Test)
            Parameter parameter_2 = BinarySerialization.ReadFromBinaryFile<Parameter>(String.Format("{0}\\{1}.dsdb", Environment.CurrentDirectory, parameter.Project_name)); // Extension = DiffSync DataBase
        }

        static bool FileHookExists(ref Parameter parameter)
        {
            string new_path;
            FileInfo file;

            new_path = parameter.FileHook();
            file = new FileInfo(new_path);

            return file.Exists;
        }

        static void DeleteOtherFileHook(ref Parameter parameter)
        {
            string[] all_files;
            all_files = Directory.GetFiles(parameter.Path_exchange_dir, "*.dshook");

            foreach (string file_element in all_files)
            {
                try
                {
                    File.Delete(file_element);
                }
                catch (IOException delete_error)
                {
                    Console.WriteLine("Die Datei {} kann nicht gelöscht werden. Bitte manuell löschen", file_element);
                    Console.WriteLine(delete_error.Message);
                }
            }
        }

        static void PrintFilesToCopy(ref List<FileElement> files)
        {
            Console.WriteLine("Es folgt die Ausgabe aller Dateien, die kopiert werden:");
            Console.WriteLine("Dateipfad                                                                        | Erstelldatum     | Schreibdatum     | Größe");
            Console.WriteLine("-----------------------------------------------------------------------------------------------------------------------------------");
            foreach (FileElement file_element in files)
            {
                Console.WriteLine("{0} | {1} | {2} | {3,7:##0.000} MB",
                    file_element.PathToPrint(80),
                    file_element.Date_created.ToString("yyyy-MM-dd HH:mm"),
                    file_element.Date_written.ToString("yyyy-MM-dd HH:mm"),
                    file_element.Size / 1024 / 1024);
            }
        }

        static bool UserWantsToContinue()
        {
            bool false_input;
            string input;
            do
            {
                Console.WriteLine();
                Console.WriteLine("Soll mit dem Kopieren begonnen werden (j/n)?");
                input = Console.ReadLine();
                if (input == "j" || input == "n")
                {
                    false_input = false;
                }
                else
                {
                    false_input = true;
                }
            } while (false_input);
            if (input == "j")
            {
                return (true);
            } else
            {
                return (false);
            }
        }
 
        static void CopyFiles(ref List<FileElement> files_to_copy, string destination_dir, string source_dir)
        {
            string destination_path,
                source_path;
            FileInfo file;

            DirectoryCheckExistsAndCreate(destination_dir);

            foreach (FileElement file_element in files_to_copy)
            {
                destination_path = String.Format("{0}{1}", destination_dir, file_element.Relative_path);
                source_path = String.Format("{0}{1}", source_dir, file_element.Relative_path);
                file = new FileInfo(destination_path);
                DirectoryCheckExistsAndCreate(file.DirectoryName);
                file = new FileInfo(source_path);
                file.CopyTo(destination_path, true);
            }
        }

        static void DirectoryCheckExistsAndCreate(string directory)
        {
            if (Directory.Exists(directory) == false)
            {
                Directory.CreateDirectory(directory);
            }
        }

        static void SetFileHook(ref Parameter parameter)
        {
            string new_path;
            FileInfo file;

            new_path = parameter.FileHook();
            file = new FileInfo(new_path);
            file.Create();
        }
    }

    public static class BinarySerialization
    {
        public static void WriteToBinaryFile<T>(string filePath, T objectToWrite, bool append = false)
        {
            using (Stream stream = File.Open(filePath, append ? FileMode.Append : FileMode.Create))
            {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                binaryFormatter.Serialize(stream, objectToWrite);
            }
        }

        public static T ReadFromBinaryFile<T>(string filePath)
        {
            using (Stream stream = File.Open(filePath, FileMode.Open))
            {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                return (T)binaryFormatter.Deserialize(stream);
            }
        }
    }
}
