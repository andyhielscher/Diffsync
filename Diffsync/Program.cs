﻿using System;
using System.Collections.Generic;
using FileElementNamespace;
using ParameterNamespace;
using System.IO;
using CommandLine;

namespace Diffsync
{
    class Program
    {
        class Options
        {
            [Option("DatabaseDirectory", Required = true, HelpText = "Pfad zum Verzeichnis der Datenbank. Falls Datenbank existiert, werden die anderen Angaben ignoriert und alle Eigenschaften aus der Datenbank eingelesen. Z.B. DatabaseDirectory=\"C:\\Users\\Name\\Documents\\Diffsync\\Sync Projekt xy.dsdb\"")]
            public string DatabaseDirectory { get; set; }

            [Option("CompleteDirectory", Required = false, HelpText = "Pfad zum vollständigen Verzeichnis, welches synchronisiert werden soll. Z.B. CompleteDirectory=\"C:\\Users\\Name\\Documents\\vollständiges Verzeichnis\". Hinweis: Verzeichnis darf NICHT mit \"\\\" enden!")]
            public string CompleteDirectory { get; set; }

            [Option("ExchangeDirectory", Required = false, HelpText = "Pfad zum Austausch-Verzeichnis, welches die Änderungen des anderen PCs enthält. Z.B. ExchangeDirectory=\"C:\\Users\\Name\\Documents\\Austausch-Verzeichnis\". Hinweis: Verzeichnis darf NICHT mit \"\\\" enden!")]
            public string ExchangeDirectory { get; set; }

            [Option("DirectoryExceptions", Separator = ';', Required = false, HelpText = "Auflistung von (verschachtelten) Verzeichnissen, welche nicht synchronisiert werden sollen. Z.B. DirectoryExceptions=\"\\Erstes Verzeichnis\";\\Test\\Test\". Trennzeichen: \";\"")]
            public IEnumerable<string> DirectoryExceptions { get; set; }

            [Option("FileExtensionExceptions", Separator = ';', Required = false, HelpText = "Auflistung von Dateiendungen, welche nicht synchronisiert werden sollen. Z.B. FileExtensionExceptions=\".exe\". Trennzeichen: \";\" Noch nicht unterstützt!")]
            public IEnumerable<string> FileExtensionExceptions { get; set; }
            
            [Option("DateSync", Required = false, HelpText = "Einmalige Eingabe. Gibt den Startzeitpunkt der ersten Synchronsierung an. Z.B. DateSync=\"YYYY-MM-TT hh:mm\"")]
            public string DateSync { get; set; }

        }
        static void Main(string[] args)
        {
            // Console: Breite und gespeicherte Anzahl Zeilen einstellen
            Console.WindowWidth = 180;
            Console.BufferHeight = 10000; // TO-DO: Wie kann man die gespeicherte Anzahl Zeilen erhöhen? Damit kann dann die Liste der zu kopierenden Dateien durchgescrollt werden...

            // Argumente verarbeiten
            string database_directory;
            string complete_directory;
            string exchange_directory;
            List<string> directory_exceptions = new List<string>();
            List<string> file_extension_exceptions = new List<string>();
            DateTime date_sync;
            var result = Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(options => {
                    database_directory = options.DatabaseDirectory;
                    complete_directory = options.CompleteDirectory;
                    exchange_directory = options.ExchangeDirectory;
                    foreach (string directory_exception in options.DirectoryExceptions) {
                        directory_exceptions.Add(directory_exception);
                    }
                    foreach (string file_extension_exception in options.FileExtensionExceptions) {
                        file_extension_exceptions.Add(file_extension_exception);
                    }
                    if (options.DateSync != null) {
                        date_sync = new DateTime(Convert.ToInt32(options.DateSync.Substring(0, 4)),
                            Convert.ToInt32(options.DateSync.Substring(5, 2)),
                            Convert.ToInt32(options.DateSync.Substring(8, 2)),
                            Convert.ToInt32(options.DateSync.Substring(11, 2)),
                            Convert.ToInt32(options.DateSync.Substring(14, 2)),
                            0);
                    }
                });
            // wurden Argumente korrekt verarbeitet?
            if (result.Tag == ParserResultType.NotParsed) {
                // Fehler bzw. der Block wird auch erreicht bei Eingabe von "--help" oder "--version"
                Console.WriteLine("Zum Beenden Enter drücken.");
                Console.ReadLine();
                Environment.Exit(0);
            }

            // initialisieren
            // TO-DO: Verzeichnis der Datenbank übergeben statt Projektname
            Parameter parameter = new Parameter(@"C:\Users\AndreasHielscher\OneDrive - SmartSim GmbH\Dokumente\", @"C:\Users\AndreasHielscher\test", new DateTime(2018, 8, 20, 0, 0, 0), "Sync Andis PC");
            parameter.DirectoryExceptions.Add("\\Benutzerdefinierte Office-Vorlagen"); // Groß-Kleinschreibung wird ignoriert, Backslash am Ende wird ignoriert
            parameter.DirectoryExceptions.Add("\\EON");
            
            // Filehook überprüfen; falls Datei mit Namen == Project_name existiert, kann nicht nochmal kopiert werden
            if (parameter.FileHookExists()) {
                // Programm wurde schon ausgeführt und kann nicht noch einmal gestartet werden
                Console.WriteLine("Das Programm wurde bereits ausgeführt. Es muss auf die Ausführung der anderen Seite gewartet werden, bis das Programm erneut ausgeführt werden kann.");
                Console.WriteLine("Zum Beenden Enter drücken.");
                Console.ReadLine();
                Environment.Exit(0);
            } else {
                // Programm wurde auf der Gegenseite schon ausgeführt, der FileHook des anderen Projekts wird gelöscht
                parameter.DeleteOtherFileHook();
            }

            // Verzeichnisse einlesen
            parameter.GetAllFiles();

            // Dateien auflisten, welche kopiert werden sollen (enthält Dateien aus complete_dir und exchange_dir)
            List<FileElement> files_to_copy = parameter.GetFilesToSync();

            // Dateien ausgeben, welche kopiert werden
            PrintFilesToSync(ref files_to_copy, parameter.PathCompleteDir, parameter.PathExchangeDir);

            // Auf Bestätigung des Users zum weiteren Programmablauf wartenConsole.WriteLine();
            Console.WriteLine("");
            Console.WriteLine("Soll mit dem Kopieren begonnen werden (j/n)?");
            Console.WriteLine("(HINWEIS: Bei Konflikten wird stets auf den User-Input gewartet)");
            if (UserInputIsYes() == false) {
                Console.WriteLine("Programm wird beendet. Bitte Enter drücken.");
                Console.ReadLine();
                Environment.Exit(0);
            }

            // Alle zu kopierenden und zu löschenden Dateien synchronisieren
            SyncFiles(ref files_to_copy, parameter.PathCompleteDir, parameter.PathExchangeDir);

            // Datenbank speichern
            parameter.PrepareSaveToDatabase();
            BinarySerialization.WriteToBinaryFile<Parameter>(String.Format("{0}\\{1}.dsdb", Environment.CurrentDirectory, parameter.ProjectName), parameter); // Extension = DiffSync DataBase

            // leere Ordner im Austausch-Verzeichnis löschen
            parameter.DeleteEmptyExchangeDirectories();

            // Filehook setzen
            parameter.SetFileHook();

            // Datenbank laden (zum Test)
            Parameter parameter_2 = BinarySerialization.ReadFromBinaryFile<Parameter>(String.Format("{0}\\{1}.dsdb", Environment.CurrentDirectory, parameter.ProjectName)); // Extension = DiffSync DataBase
        }

        static void PrintFilesToSync(ref List<FileElement> files, string complete_dir, string exchange_dir)
        {
            Console.WriteLine("Übersicht der beiden Verzeichnisse zur Synchronisierung:");
            Console.WriteLine("vollständiges Verzeichnis (\\\\FULL\\): {0}", complete_dir);
            Console.WriteLine("Austausch-Verzeichnis     (\\\\EXCH\\): {0}", exchange_dir);
            Console.WriteLine("");
            Console.WriteLine("Es folgt die Ausgabe aller Dateien, die neu erstellt und überschrieben (+) oder gelöscht (-) werden:");
            Console.WriteLine("Aktion | Dateipfad                                                                                                               | Erstelldatum     | Schreibdatum     | Größe");
            Console.WriteLine("-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------");
            foreach (FileElement file_element in files) {
                string action;
                string short_dir;
                if (file_element.WillBeDeleted == true) {
                    action = "-";
                } else {
                    action = "+";
                }
                if (file_element.FromCompleteDir == true) {
                    short_dir = "\\\\FULL\\";
                } else {
                    short_dir = "\\\\EXCH\\";
                }
                Console.WriteLine("     {0} | {1}{2} | {3} | {4} | {5,7:##0.000} MB",
                action,
                short_dir, file_element.RelativePathToPrint(112),
                file_element.DateCreated.ToString("yyyy-MM-dd HH:mm"),
                file_element.DateWritten.ToString("yyyy-MM-dd HH:mm"),
                file_element.Size);
            }
        }

        static bool UserInputIsYes()
        {
            bool false_input;
            string input;
            do {
                input = Console.ReadLine();
                if (input == "j" || input == "n") {
                    false_input = false;
                } else {
                    false_input = true;
                }
            } while (false_input);
            if (input == "j") {
                return (true);
            } else {
                return (false);
            }
        }

        static void SyncFiles(ref List<FileElement> files_to_sync, string complete_dir, string exchange_dir)
        {
            string destination_path,
                source_path;
            FileInfo file;

            DirectoryCheckExistsAndCreate(exchange_dir);

            foreach (FileElement file_element in files_to_sync) {
                if (file_element.WillBeDeleted == false) {
                    if (file_element.FromCompleteDir == true) {
                        destination_path = String.Format("{0}{1}", exchange_dir, file_element.RelativePath);
                        source_path = String.Format("{0}{1}", complete_dir, file_element.RelativePath);
                    } else {
                        destination_path = String.Format("{0}{1}", complete_dir, file_element.RelativePath);
                        source_path = String.Format("{0}{1}", exchange_dir, file_element.RelativePath);
                    }

                    DirectoryCheckExistsAndCreate(new FileInfo(destination_path).DirectoryName);
                    file = new FileInfo(source_path);

                    if (file_element.FromCompleteDir == true) {
                        // von vollständigem Verzeichnis in Austausch-Verzeichnis kopieren
                        file.CopyTo(destination_path, true);
                    } else {
                        // aus Austausch-Verzeichnis in vollständiges Verzeichnis verschieben
                        // Hinweis: Es kann ein Konflikt entstehen, wenn Datei im vollständigen Verzeichnis neuer ist. In diesem Fall wird eine Abfrage an den Benutzer gestellt.
                        FileInfo file_dest = new FileInfo(destination_path);
                        if (file_dest.Exists) {
                            // die Datei existiert bereits im vollständigen Verzeichnis, auf Konflikt prüfen
                            if (file_dest.CreationTime > file_element.DateCreated || file_dest.LastWriteTime > file_element.DateWritten) {
                                // Konflikt
                                Console.WriteLine("KONFLIKT: Datei {0} ist neuer als im Austausch-Verzeichnis", destination_path);
                                Console.WriteLine("    Datei in \\\\FULL\\: {0} | {1} | {2,7:##0.000} MB", 
                                    file_dest.CreationTime.ToString("yyyy-MM-dd HH:mm"),
                                    file_dest.LastWriteTime.ToString("yyyy-MM-dd HH:mm"),
                                    file_dest.Length / 1024 / 1024);
                                Console.WriteLine("    Datei in \\\\EXCH\\: {0} | {1} | {2,7:##0.000} MB",
                                    file_element.DateCreated.ToString("yyyy-MM-dd HH:mm"),
                                    file_element.DateWritten.ToString("yyyy-MM-dd HH:mm"),
                                    file_element.Size);
                                Console.WriteLine("    Soll die Datei im vollständigen Verzeichnis überschrieben werden (j/n)?");
                                if (UserInputIsYes()) {
                                    file_dest.Delete();
                                    file.MoveTo(destination_path);
                                } else {
                                    TryToDeleteFile(source_path);
                                }
                            } else {
                                file_dest.Delete();
                                file.MoveTo(destination_path);
                            }
                        } else {
                            file.MoveTo(destination_path);
                        }
                    }
                } else {
                    // dieser Block kann nur mit Vergleich der Datenbank erreicht werden
                    if (file_element.FromCompleteDir == true) {
                        // Datei ist im vollständigen Verzeichnis nicht mehr vorhanden, folglich im Austausch-Verzeichnis als zu löschen markieren
                        file = new FileInfo(String.Format("{0}{1}.dsdel", exchange_dir, file_element.RelativePath));
                        file.Create();
                    } else {
                        // "Markierung" im Austausch-Verzeichnis löschen und im vollständigen Verzeichnis richtige Datei löschen
                        destination_path = String.Format("{0}{1}", exchange_dir, file_element.RelativePath);
                        source_path = String.Format("{0}{1}", complete_dir, file_element.RelativePath);
                        TryToDeleteFile(source_path);
                        TryToDeleteFile(destination_path.Substring(0, destination_path.Length - 6)); // Endung ".dsdel" wird abgeschnitten
                    }
                }
            }

            Console.WriteLine("Kopiervorgang erfolgreich.");
        }

        static void DirectoryCheckExistsAndCreate(string directory)
        {
            if (Directory.Exists(directory) == false) {
                Directory.CreateDirectory(directory);
            }
        }

        static void TryToDeleteFile(string path)
        {
            FileInfo file = new FileInfo(path);
            try {
                file.Delete();
            } catch (IOException delete_error) {
                Console.WriteLine("Die Datei {} kann nicht gelöscht werden. Bitte manuell löschen", path);
                Console.WriteLine(delete_error.Message);
            }
        }
    }

    static class BinarySerialization
    {
        public static void WriteToBinaryFile<T>(string filePath, T objectToWrite, bool append = false)
        {
            using (Stream stream = File.Open(filePath, append ? FileMode.Append : FileMode.Create)) {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                binaryFormatter.Serialize(stream, objectToWrite);
            }
        }

        public static T ReadFromBinaryFile<T>(string filePath)
        {
            using (Stream stream = File.Open(filePath, FileMode.Open)) {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                return (T)binaryFormatter.Deserialize(stream);
            }
        }
    }
}
