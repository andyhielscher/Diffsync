﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datatypes;

namespace Diffsync
{
    class Program
    {
        static void Main(string[] args)
        {
            Parameter parameter = new Parameter();

            // Console: Breite einstellen
            Console.WindowWidth = 130;

            // Arbeitsvariablen (aktuell hardgecodet):
            parameter.Main_path = @"C:\Users\AndreasHielscher\OneDrive - SmartSim GmbH\Dokumente\";
            parameter.Directory_exceptions = new List<string>();
            parameter.Directory_exceptions.Add("Benutzerdefinierte Office-Vorlagen");
            parameter.Directory_exceptions.Add("EON");
            parameter.Begin_sync_from = new DateTime(2018, 7, 10, 0, 0, 0);
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
        }

        static void PrintFilesToCopy(ref List<FileElement> files)
        {
            Console.WriteLine("Es folgt die Ausgabe aller Dateien, die kopiert werden:");
            foreach (FileElement file_element in files)
            {
                Console.WriteLine("{0}  {1}  {2}  {3:0.000} MB",
                    file_element.Path,
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
}
