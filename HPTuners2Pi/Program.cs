using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HPTuners2Pi
{
    internal class Program
    {
        static string CleanUpUnits(string units)
        {
            // Replace common unit abbreviations with their full forms or symbols
            units = units.Replace("deg", "\xB0"); // Degree symbol
            if (units == "[g]") units = "[G]";
            if (units == "[C]") units = "[\xB0\x43]"; // degrees Celsius symbol
            return units;
        }
        static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(unixTimeStamp);
        }
        static void Main(string[] args)
        {
            //Check argumants and open files
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: HPTuners2Pi <data_file>");
                return;
            }
            if (!File.Exists(args[0]))
            {
                Console.WriteLine("File not found: " + args[0]);
                return;
            }

            string infile = args[0];
            string outfile = args[0] + ".txt";

            // Read all lines from the CSV file
            var buffer = File.ReadAllLines(infile);

            //filter out header info
            var lines = buffer.Where(line => line.Contains(",")).ToArray();

            // Split each line into fields
            var rows = lines.Select(line => line.Split(',')).ToList();


            // Transpose rows to columns
            int columnCount = rows[0].Length;
            var columns = new List<List<string>>();

            for (int col = 0; col < columnCount; col++)
            {
                var column = new List<string>();
                foreach (var row in rows)
                {
                    column.Add(row[col]);
                }
                columns.Add(column);
            }


            // Write the Pi file header
            var writer = new StreamWriter(outfile, false, Encoding.GetEncoding(1252));      //Important to use the 1252 encoding to match Pi Toolbox ASCII format      
            // File header

            writer.WriteLine("PiToolboxVersionedASCIIDataSet");
            writer.WriteLine("Version\t2");
            writer.WriteLine();
            writer.WriteLine("{OutingInformation}");
            writer.WriteLine($"CarName\tHP Tuners");
            writer.WriteLine("FirstLapNumber\t0");

            int iName = 1;
            int iUnit = 2;
            int iStart = 3;

            // Cycle through and create channel blocks
            for (int i = 1; i < columns.Count; i++)
            {
                //extract units and name from string

                string channelName = columns[i][iName];
                string units = columns[i][iUnit];

                //fix units representation
                units = CleanUpUnits(units);

                writer.WriteLine();
                writer.WriteLine("{ChannelBlock}");
                writer.WriteLine($"Time\t{channelName}[{units}]");              

                for (int j = iStart; j < columns[i].Count; j++)
                {

                    if (double.TryParse(columns[0][j], out double time))
                    {
                        // Convert Unix timestamp to DateTime
                        DateTime dtTime = UnixTimeStampToDateTime(time);
                        // Calculate elapsed time from the first timestamp
                        TimeSpan elapsedTime = dtTime - UnixTimeStampToDateTime(double.Parse(columns[0][iStart]));
                        //correctedTime = elapsedTime;
                        if (double.TryParse(columns[i][j], out double value))
                        {
                            writer.WriteLine($"{elapsedTime.TotalSeconds}\t{value}");
                        }
                        else
                        {
                            // Handle non-numeric data (optional)
                            writer.WriteLine($"{elapsedTime.TotalSeconds}\t{columns[i][j]}");
                        }
                    }

                    else
                    {
                        // Handle non-numeric data (optional)
                        writer.WriteLine($"{columns[0][j]}\t{columns[i][j]}");
                    }
                }
            }
            writer.Close();
        }
    }
}
