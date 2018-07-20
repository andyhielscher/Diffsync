using System;
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

            // Arbeitsvariablen (aktuell hardgecodet):
            parameter.main_path = @"C:\Users\AndreasHielscher\OneDrive - SmartSim GmbH\Dokumente\";
            parameter.directory_exceptions = new List<string>();
            parameter.directory_exceptions.Add("Benutzerdefinierte Office-Vorlagen");
            parameter.directory_exceptions.Add("EON");
            parameter.begin_sync_from = new DateTime(2018, 7, 10, 0, 0, 0);

            // Verzeichnis einlesen
            parameter.GetAllFiles();
        }
    }
}
