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

            // Arbeitsvariablen (aktuell hardgecodet):
            parameter.Path_complete_dir = @"C:\Users\AndreasHielscher\OneDrive - SmartSim GmbH\Dokumente\";
            parameter.Path_exchange_dir = @"C:\Users\AndreasHielscher\test";
            parameter.Directory_exceptions.Add("Benutzerdefinierte Office-Vorlagen");
            parameter.Directory_exceptions.Add("EON");
            parameter.Begin_sync_from = new DateTime(2018, 8, 20, 0, 0, 0);
            parameter.Project_name = "Sync Andis PC";

            // Verzeichnis einlesen
            parameter.GetAllFiles();

            // Dateien auflisten, welche kopiert werden sollen
            List<FileElement> files_to_copy = parameter.GetFilesToCopy();

            // Dateien ausgeben, welche kopiert werden
            PrintFilesToCopy(ref files_to_copy);

            // Auf Bestätigung des Users zum weiteren Programmablauf warten
            if (UserWantsToContinue() == false)
            {
                Environment.Exit(0);
            }

            // Alle zu kopierenden Dateien kopieren
            parameter.CopyFilesToExchangeDir();

            // Datenbank speichern
            BinarySerialization.WriteToBinaryFile<Parameter>(String.Format("{0}\\{1}.dsdb", Environment.CurrentDirectory, parameter.Project_name), parameter); // Extension = DiffSync DataBase

            // Datenbank laden (zum Test)
            Parameter parameter_2 = BinarySerialization.ReadFromBinaryFile<Parameter>(String.Format("{0}\\{1}.dsdb", Environment.CurrentDirectory, parameter.Project_name)); // Extension = DiffSync DataBase
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
