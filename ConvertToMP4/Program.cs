using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConvertToMP4
{
    class Program
    {
        static void Main(string[] args)
        {
            //string command = "-i \"{0}\" -s 740x416 -c:v libx264 -crf:v 26 -preset:v medium -ac 1 -c:a aac -strict -2 -cutoff 15000 -b:a 64k \"{1}\"";
            string command = "-i \"{0}\" -s 740x416 -c:v libx264 -crf:v 26 -preset:v medium -ac 1 -c:a aac -strict -2 -cutoff 15000 -b:a 64k \"{1}\"";
            command = "-y -i \"{0}\" -s 640x360 -c:v libx264 -crf:v 26 -preset:v veryslow -ac 1 -c:a libfdk_aac -b:a 64k -strict -2 -cutoff 15000 -vf \"ass='{2}'\" \"{1}\"";
            List<string> cleanup = new List<string>();
            
            foreach (var item in args)
            {
                List<string> fileParts = item.Split('.').ToList();
                string filename = "";
                fileParts.Insert(fileParts.Count - 1, "MOBILE");
                fileParts.RemoveAt(fileParts.Count - 1);
                fileParts.Add("mp4");
                filename = string.Join(".", fileParts.ToArray());

                string mkvmergeOutput = ExecuteAndReturn("mkvmerge", "--identify \"" + item + "\"");

                string[] mkvmergeLines = mkvmergeOutput.Split(new string[]{System.Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries);
                int SubtitleTrack = -1;
                string subFormat = "";
                foreach (var line in mkvmergeLines)
                {
                    if (line.IndexOf("TEXT/SSA") != -1 || line.IndexOf("TEXT/SRT") != -1 || line.IndexOf("TEXT/ASS") != -1)
                    {
                        if (line.IndexOf("TEXT/SSA") != -1)
                            subFormat = "SSA";
                        if (line.IndexOf("TEXT/ASS") != -1)
                            subFormat = "ASS";
                        if (line.IndexOf("TEXT/SRT") != -1)
                            subFormat = "SRT";
                        SubtitleTrack = int.Parse(line.Substring(9, line.IndexOf(':') - 9));
                        break;
                    }
                }

                Regex r = new Regex("Attachment ID ([0-9]*): type 'application/.+font', size [0-9]+ bytes, file name '(.+)'");
                //System.Diagnostics.Debugger.Break();

                foreach (var line in mkvmergeLines)
                {
                    var result = r.Match(line);
                    if (result.Success)
                    {
                        ExecuteAndReturn("mkvextract", string.Format("attachments \"{0}\" {1}:\"{2}\"", item, result.Groups[1].Value, result.Groups[2].Value));
                        if (!File.Exists("C:\\tools\\ffmpeg\\fonts\\" + result.Groups[2].Value))
                            File.Copy(result.Groups[2].Value, "C:\\tools\\ffmpeg\\fonts\\" + result.Groups[2].Value);
                        cleanup.Add(result.Groups[2].Value);
                    }

                }



                string subtitlefile = filename + "." + subFormat;
                cleanup.Add(subtitlefile);

                ExecuteAndReturn("mkvextract", string.Format("tracks \"{0}\" {1}:\"{2}\"", item, SubtitleTrack.ToString(), subtitlefile));
                subtitlefile= subtitlefile.Replace("\\","\\\\").Replace(":","\\:");
                

                var process = new System.Diagnostics.Process();

                Console.WriteLine(string.Format(command, item, filename, subtitlefile));

                process.StartInfo = new System.Diagnostics.ProcessStartInfo("ffmpeg", string.Format(command, item, filename, subtitlefile));
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                //process.OutputDataReceived += process_OutputDataReceived;
                process.Start();
                //StringBuilder q = new StringBuilder();
                //while (!process.HasExited)
                //{
                //    Console.Write(process.StandardOutput.ReadToEnd());
                //}
                //string r = q.ToString();

                

                process.WaitForExit();
            }
            
            Console.Read();
            foreach (var record in cleanup.Distinct())
            {
                File.Delete(record);
            }

        }

        static string ExecuteAndReturn(string command, string args)
        {
            var process = new System.Diagnostics.Process();


            process.StartInfo = new System.Diagnostics.ProcessStartInfo(command, args);
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            //process.OutputDataReceived += process_OutputDataReceived;
            process.Start();
            StringBuilder q = new StringBuilder();
            while (!process.HasExited)
            {
                q.Append(process.StandardOutput.ReadToEnd());
                //Console.Write(process.StandardOutput.ReadToEnd());
            }
            return q.ToString();
        }

        static void process_OutputDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);
        }
    }
}
