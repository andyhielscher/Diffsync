using System;
using System.Collections;
using System.Collections.Generic;
using FileElementNamespace;
using ParameterNamespace;
using System.Runtime.Serialization;
using System.Xml;
using System.IO;
using CommandLine;
using System.IO.Compression;

namespace Diffsync
{
    class Program
    {
        class Options
        {
            [Option("DatabaseDirectory", Required = true, HelpText = "Pfad zum Verzeichnis der Datenbank. Z.B. DatabaseDirectory=\"C:\\Users\\Name\\Documents\\Diffsync\\Sync Projekt xy.dsdx\". Hinweis: Falls Datenbank existiert, wird die Angabe \"DateSync\" ignoriert und alle Eigenschaften aus der Datenbank eingelesen.")]
            public string DatabaseDirectory { get; set; }

            [Option("CompleteDirectory", Required = false, HelpText = "Pfad zum vollständigen Verzeichnis, welches synchronisiert werden soll. Z.B. CompleteDirectory=\"C:\\Users\\Name\\Documents\\vollständiges Verzeichnis\". Hinweis: Verzeichnis darf NICHT mit \"\\\" enden!")]
            public string CompleteDirectory { get; set; }

            [Option("ExchangeDirectory", Required = false, HelpText = "Pfad zum Austausch-Verzeichnis, welches die Änderungen des anderen PCs enthält. Z.B. ExchangeDirectory=\"C:\\Users\\Name\\Documents\\Austausch-Verzeichnis\". Hinweis: Verzeichnis darf NICHT mit \"\\\" enden!")]
            public string ExchangeDirectory { get; set; }

            [Option("DirectoryExceptions", Separator = ';', Required = false, HelpText = "Auflistung von (verschachtelten) Verzeichnissen, welche nicht synchronisiert werden sollen. Z.B. DirectoryExceptions=\"\\Erstes Verzeichnis;\\Test\\Test\". Trennzeichen: \";\"")]
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

            // initialisieren
            Parameter parameter = new Parameter();

            // Argumente verarbeiten
            bool error = false;
            bool binary_format = false;
            var result = Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(options => {
                    // verschiedene Checks der Argumente
                    if (options.DatabaseDirectory.EndsWith(".dsdb") == true) {
                        // alte binäre Datenbank, wird ggf eingelesen und dann konvertiert
                        binary_format = true;
                    } else if (options.DatabaseDirectory.EndsWith(".dsdx") == false) {
                        Console.Error.WriteLine("Falsche Dateiendung der Datenbank. Datenbank muss auf \".dsdx\" enden.");
                        error = true;
                    }
                    if (Directory.Exists(options.CompleteDirectory) == false) {
                        Console.Error.WriteLine("Hauptverzeichnis {0} nicht gefunden!", options.CompleteDirectory);
                        error = true;
                    }
                    if (Directory.Exists(options.ExchangeDirectory) == false) {
                        Console.Error.WriteLine("Austauschverzeichnis {0} nicht gefunden!", options.ExchangeDirectory);
                        error = true;
                    }
                    DateTime date_sync = new DateTime(2000, 1, 1, 0, 0, 0);
                    if (options.DateSync != null) {
                        try {
                            date_sync = new DateTime(Convert.ToInt32(options.DateSync.Substring(0, 4)),
                                Convert.ToInt32(options.DateSync.Substring(5, 2)),
                                Convert.ToInt32(options.DateSync.Substring(8, 2)),
                                Convert.ToInt32(options.DateSync.Substring(11, 2)),
                                Convert.ToInt32(options.DateSync.Substring(14, 2)),
                                0);
                        } catch {
                            Console.Error.WriteLine("Fehler beim Verarbeiten des Datums. Datum muss vom Format \"YYYY-MM-TT hh:mm\" sein.");
                            error = true;
                        }
                    }
                    foreach (string directory_exception in options.DirectoryExceptions) {
                        if (directory_exception.StartsWith("\\") == false) {
                            Console.WriteLine(String.Format("Fehler in DirectoryExceptions {0}. Beginnt nicht mit \"\\\".", directory_exception));
                            error = true;
                        }
                    }

                    // Datenbank auf Existenz prüfen
                    FileInfo file = new FileInfo(options.DatabaseDirectory);
                    if (error == false && file.Exists) {
                        // Datenbank Backup erstellen und dann laden
                        DatabaseCreateBackup(options.DatabaseDirectory);
                        if (binary_format) {
                            // Extension dsdb = DiffSync Database Binary
                            // aktualisieren zu dsdx
                            try {
                                parameter = BinarySerialization.ReadFromBinaryFile<Parameter>(options.DatabaseDirectory);
                                parameter.SetDatabaseFile(parameter.DatabaseFile.Replace(".dsdb", ".dsdx"));
                                options.DatabaseDirectory = options.DatabaseDirectory.Replace(".dsdb", ".dsdx"); // zum Vergleich der Pfade
                                Console.WriteLine("Datenbank im alten Format eingelesen. Wird zu neuem Format konvertiert.");
                            } catch (Exception e) {
                                Console.Error.WriteLine("Fehler beim Einlesen der binären Datenbank.");
                                Console.Error.Write("{0}", e);
                                error = true;
                            }
                        } else {
                            // Extension dsdx = DiffSync Database Xml
                            try {
                                parameter = ArchivedDataContractSerialization.ReadParameter(options.DatabaseDirectory);
                            } catch (Exception e) {
                                Console.Error.WriteLine("Fehler beim Einlesen der XML-Datenbank.");
                                Console.Error.Write("{0}", e);
                                error = true;
                            }
                        }

                        // Check, ob Verzeichnisse noch gleich sind
                        if (error == false && string.Compare(parameter.PathCompleteDir.TrimEnd('\\'), options.CompleteDirectory, true) != 0) {
                            // vollständiges Verzeichnis nicht gleich
                            Console.WriteLine("Das vollständige Verzeichnis in der Datenbank entspricht nicht dem als Parameter angegebenen Verzeichnis.");
                            Console.WriteLine("Datenbank: {0}", parameter.PathCompleteDir.TrimEnd('\\'));
                            Console.WriteLine("Parameter: {0}", options.CompleteDirectory);
                            Console.WriteLine("Soll das Verzeichnis der Datenbank ersetzt werden (j/n)?");
                            if (UserInputIsYes()) {
                                parameter.SetPathCompleteDir(options.CompleteDirectory);
                            }
                        }
                        if (error == false && string.Compare(parameter.PathExchangeDir.TrimEnd('\\'), options.ExchangeDirectory, true) != 0) {
                            // vollständiges Verzeichnis nicht gleich
                            Console.WriteLine("Das Austausch-Verzeichnis in der Datenbank entspricht nicht dem als Parameter angegebenen Verzeichnis.");
                            Console.WriteLine("Datenbank: {0}", parameter.PathExchangeDir.TrimEnd('\\'));
                            Console.WriteLine("Parameter: {0}", options.ExchangeDirectory);
                            Console.WriteLine("Soll das Verzeichnis der Datenbank ersetzt werden (j/n)?");
                            if (UserInputIsYes()) {
                                parameter.SetPathExchangeDir(options.ExchangeDirectory);
                            }
                        }

                        // Check, ob Verzeichnis der Datenbank noch gleich ist
                        if (error == false && string.Compare(parameter.DatabaseFile, options.DatabaseDirectory, true) != 0) {
                            // vollständiges Verzeichnis nicht gleich
                            Console.WriteLine("Die Datenbank-Datei entspricht nicht der als Parameter angegebenen Datei.");
                            Console.WriteLine("Datenbank: {0}", parameter.DatabaseFile);
                            Console.WriteLine("Parameter: {0}", options.DatabaseDirectory);
                            Console.WriteLine("Soll der Pfad zur Datenbank durch Parameter ersetzt werden (j/n)?");
                            if (UserInputIsYes()) {
                                parameter.SetDatabaseFile(options.DatabaseDirectory);
                            }
                        }

                        // fertig
                        if (error == false) {
                            Console.WriteLine("Datenbank geladen. Synchronisierungsvorgang wird gestartet. Bitte warten.");
                        }
                    } else if (error == false) {
                        // Parameter neu erstellen
                        parameter = new Parameter(options.DatabaseDirectory, options.CompleteDirectory, options.ExchangeDirectory, date_sync);
                        Console.WriteLine("Keine Datenbank gefunden. Synchronisierungsvorgang wird anhand von Datumsänderungen gestartet. Bitte warten.");
                    }

                    if (error == false) {
                        // Parameter Exceptions neu laden
                        parameter.DirectoryExceptions.Clear();
                        foreach (string directory_exception in options.DirectoryExceptions) {
                            parameter.DirectoryExceptions.Add(directory_exception);
                        }
                        parameter.FileExtensionExceptions.Clear();
                        foreach (string file_extension_exception in options.FileExtensionExceptions) {
                            parameter.FileExtensionExceptions.Add(file_extension_exception);
                        }
                    }
                });

            // wurden Argumente korrekt verarbeitet?
            if (error || result.Tag == ParserResultType.NotParsed) {
                // Fehler bzw. der Block wird auch erreicht bei Eingabe von "--help" oder "--version"
                Console.WriteLine("Zum Beenden Enter drücken.");
                Console.ReadLine();
                Environment.Exit(0);
            }

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
            PrintFilesToSync(ref files_to_copy, ref parameter);

            // Auf Bestätigung des Users zum weiteren Programmablauf wartenConsole.WriteLine();
            Console.WriteLine("");
            Console.WriteLine("Soll mit dem Kopieren begonnen werden (j/n)?");
            if (UserInputIsYes() == false) {
                Console.WriteLine("Programm wird beendet. Bitte Enter drücken.");
                Console.ReadLine();
                Environment.Exit(0);
            } else {
                Console.WriteLine("Kopiervorgang wird gestartet. Bitte warten.");
            }

            // Alle zu kopierenden und zu löschenden Dateien synchronisieren
            SyncFiles(ref files_to_copy, parameter.PathCompleteDir, parameter.PathExchangeDir);

            // leere Ordner im Austausch-Verzeichnis löschen
            parameter.DeleteEmptyExchangeDirectories();

            // Datenbank speichern und Datenbank-Backup löschen
            parameter.PrepareSaveToDatabase();
            ArchivedDataContractSerialization.WriteParameter(parameter.DatabaseFile, ref parameter);
            TryToDeleteFile(String.Format("{0}.backup", parameter.DatabaseFile));

            // Filehook setzen
            parameter.SetFileHook();

            // Programm abgeschlossen, beenden
            Console.WriteLine("");
            Console.WriteLine("Synchronisierung erfolgreich abgeschlossen. Alle Änderungen wurden in der Datenbank aktualisiert.");
            Console.WriteLine("Zum Beenden Enter drücken.");
            Console.ReadLine();
            Environment.Exit(0);
        }

        static void PrintFilesToSync(ref List<FileElement> files, ref Parameter parameter)
        {
            Console.WriteLine("");
            Console.WriteLine("Übersicht der beiden Verzeichnisse zur Synchronisierung:");
            Console.WriteLine("vollständiges Verzeichnis (\\\\FULL\\): {0}", parameter.PathCompleteDir);
            Console.WriteLine("Austausch-Verzeichnis     (\\\\EXCH\\): {0}", parameter.PathExchangeDir);
            Console.WriteLine("");
            Console.WriteLine("Übersicht der ausgeschlossenen Verzeichnisse:");
            foreach (string directory_exception in parameter.DirectoryExceptions) {
                Console.WriteLine(String.Format("  {0}", directory_exception));
            }
            // To-Do: File-Exceptions ausgeben
            Console.WriteLine("");
            Console.WriteLine("Es folgt die Ausgabe aller Dateien, die neu erstellt und überschrieben (+) oder gelöscht (del) werden:");
            Console.WriteLine("Aktion | Dateipfad                                                                                                               | Erstelldatum     | Schreibdatum     | Größe");
            Console.WriteLine("-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------");
            foreach (FileElement file_element in files) {
                string action;
                string short_dir;
                if (file_element.WillBeDeleted == true) {
                    action = "del";
                } else {
                    action = "  +";
                }
                if (file_element.FromCompleteDir == true) {
                    short_dir = "\\\\FULL\\";
                } else {
                    short_dir = "\\\\EXCH\\";
                }
                Console.WriteLine("   {0} | {1}{2} | {3} | {4} | {5,7:##0.000} MB",
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
                        // Hinweise: 
                        // 1. Es kann ein Konflikt entstehen, wenn Datei im vollständigen Verzeichnis neuer ist. In diesem Fall wird eine Abfrage an den Benutzer gestellt.
                        // 2. Konfliktbehandlung bereits Parameter.GetFilesToSyncDatabase durchgeführt.
                        TryToDeleteFile(destination_path);
                        file.MoveTo(destination_path);
                    }
                } else {
                    if (file_element.FromCompleteDir == true) {
                        // Datei ist im vollständigen Verzeichnis nicht mehr vorhanden, folglich im Austausch-Verzeichnis als zu löschen markieren
                        file = new FileInfo(String.Format("{0}{1}.dsdel", exchange_dir, file_element.RelativePath));
                        file.Create();
                    } else {
                        // "Markierung" im Austausch-Verzeichnis löschen und im vollständigen Verzeichnis richtige Datei löschen
                        destination_path = String.Format("{0}{1}", complete_dir, file_element.RelativePath.Substring(0, file_element.RelativePath.Length - 6)); // Endung ".dsdel" wird abgeschnitten
                        source_path = String.Format("{0}{1}", exchange_dir, file_element.RelativePath);
                        TryToDeleteFile(destination_path);
                        TryToDeleteFile(source_path);
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
            if (file.Exists) {
                try {
                    file.Delete();
                } catch (IOException delete_error) {
                    Console.WriteLine(String.Format("Die Datei {0} kann nicht gelöscht werden. Bitte manuell löschen", path));
                    Console.WriteLine(delete_error.Message);
                }
            }
        }

        static void DatabaseCreateBackup(string file)
        {
            string backup_path = String.Format("{0}.backup", file);
            FileInfo file_info = new FileInfo(file);
            file_info.CopyTo(backup_path, true);
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

    public sealed class ArchivedDataContractSerialization
    {
        private ArchivedDataContractSerialization() { }

        public static void WriteParameter(string filename, ref Parameter parameter)
        {
            try {
                MemoryStream ms = new MemoryStream();
                DataContractSerializer dcs = new DataContractSerializer(typeof(Parameter));
                dcs.WriteObject(ms, parameter);
                
                using (FileStream zip_file = new FileStream(filename, FileMode.Create)) {
                    using (ZipArchive archive = new ZipArchive(zip_file, ZipArchiveMode.Create)) {
                        ZipArchiveEntry database = archive.CreateEntry("diffsyncdatabase.xml", CompressionLevel.Optimal);
                        using (BinaryWriter writer = new BinaryWriter(database.Open())) { // new BinaryWriter(database.Open(), encoding: System.Text.Encoding.UTF8) --> UTF8 ist standard-enconding (https://docs.microsoft.com/de-de/dotnet/api/system.io.binarywriter?view=netframework-4.8)
                            // Position zum Schreiben auf 0 setzen
                            ms.Position = 0;
                            writer.Write(ms.ToArray());
                        }
                    }
                }
                ms.Close();

            } catch (SerializationException serExc) {
                Console.WriteLine("Serialization Failed");
                Console.WriteLine(serExc.Message);
            } catch (Exception exc) {
                Console.WriteLine(
                "The serialization operation failed: {0} StackTrace: {1}",
                exc.Message, exc.StackTrace);
            }
        }

        public static Parameter ReadParameter(string filename)
        {
            try {
                MemoryStream ms = new MemoryStream();
                using (ZipArchive archive = ZipFile.OpenRead(filename)) {
                    foreach (ZipArchiveEntry database in archive.Entries) {
                        if (string.Compare(database.FullName, "diffsyncdatabase.xml") == 0) {
                            Stream unzipped_database_stream = database.Open();
                            unzipped_database_stream.CopyTo(ms);
                            // Position zum Lesen auf 0 setzen
                            ms.Position = 0;
                        }
                    }
                }
                XmlDictionaryReader reader = XmlDictionaryReader.CreateTextReader(ms, new XmlDictionaryReaderQuotas()); // leider kann ich das Standard-Encoding nicht finden
                DataContractSerializer ser = new DataContractSerializer(typeof(Parameter));

                // Deserialize the data and read it from the instance.
                Parameter parameter;
                parameter = (Parameter)ser.ReadObject(reader, true);
                reader.Close();
                ms.Close();
                return parameter;

            } catch (SerializationException serExc) {
                Console.WriteLine("Serialization Failed");
                Console.WriteLine(serExc.Message);
                Parameter parameter = new Parameter();
                return parameter;
            } catch (Exception exc) {
                Console.WriteLine(
                "The serialization operation failed: {0} StackTrace: {1}",
                exc.Message, exc.StackTrace);
                Parameter parameter = new Parameter();
                return parameter;
            }
        }
    }
}
