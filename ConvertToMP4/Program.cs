using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ConvertToMP4
{
    class Program
    {
        static void Main(string[] args)
        {
            string tmp;
            //string command = "-i \"{0}\" -s 740x416 -c:v libx264 -crf:v 26 -preset:v medium -ac 1 -c:a aac -strict -2 -cutoff 15000 -b:a 64k \"{1}\"";
            string command = "-i \"{0}\" -s 740x416 -c:v libx264 -crf:v 26 -preset:v medium -ac 1 -c:a aac -strict -2 -cutoff 15000 -b:a 64k \"{1}\"";

            command = "-y -i \"{0}\" -map 0:v -map 0:{3} -c:v libx264 -crf:v 21 -preset:v fast -ac 1 -c:a copy -vf \"ass='{2}'\" \"{1}\""; //no resizing for mobile, just hardsubbing

            command = "-y -i \"{0}\" -s {4} -map 0:v -map 0:{3} -c:v libx264 -crf:v 26 -preset:v veryfast -ac 1 -c:a libfdk_aac -b:a 64k -strict -2 -cutoff 15000 -vf \"ass='{2}'\" \"{1}\"";

            string commandnoSubs = "-y -i \"{0}\" -s {3} -map 0:v -map 0:{2} -c:v libx264 -crf:v 26 -preset:v veryfast -ac 1 -c:a libfdk_aac -b:a 64k -strict -2 -cutoff 15000 \"{1}\"";
            List<string> cleanup = new List<string>();

            foreach (var item in args)
            {
                FileInfo fileinfo = new FileInfo(item);
                var dir = Path.GetDirectoryName(item);
                if (dir == "")
                    dir = ".";
                List<string> fileParts = item.Split('.').ToList();
                string filename = "";
                fileParts.Insert(fileParts.Count - 1, "MOBILE");
                fileParts.RemoveAt(fileParts.Count - 1);
                fileParts.Add("mp4");
                filename = string.Join(".", fileParts.ToArray());

                //string mkvmergeOutput = ExecuteAndReturn("mkvmerge", "--identify \"" + item + "\"");

                //string[] mkvmergeLines = mkvmergeOutput.Split(new string[]{System.Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries);
                //int SubtitleTrack = -1;
                //string subFormat = "";


                string subtitlefile = filename + ".ass";

                //foreach (var line in mkvmergeLines)
                //{
                //    if (line.IndexOf("TEXT/SSA") != -1 || line.IndexOf("TEXT/SRT") != -1 || line.IndexOf("TEXT/ASS") != -1)
                //    {
                //        if (line.IndexOf("TEXT/SSA") != -1)
                //            subFormat = "SSA";
                //        if (line.IndexOf("TEXT/ASS") != -1)
                //            subFormat = "ASS";
                //        if (line.IndexOf("TEXT/SRT") != -1)
                //            subFormat = "SRT";
                //        SubtitleTrack = int.Parse(line.Substring(9, line.IndexOf(':') - 9));
                //        break;
                //    }
                //}

                //Regex r = new Regex("Attachment ID ([0-9]*): type 'application/.+font', size [0-9]+ bytes, file name '(.+)'");
                ////System.Diagnostics.Debugger.Break();

                //foreach (var line in mkvmergeLines)
                //{
                //    var result = r.Match(line);
                //    if (result.Success)
                //    {
                //        ExecuteAndReturn("mkvextract", string.Format("attachments \"{0}\" {1}:\"{2}\"", item, result.Groups[1].Value, result.Groups[2].Value));
                //        if (!File.Exists("C:\\tools\\ffmpeg\\fonts\\" + result.Groups[2].Value))
                //            File.Copy(result.Groups[2].Value, "C:\\tools\\ffmpeg\\fonts\\" + result.Groups[2].Value);
                //        cleanup.Add(result.Groups[2].Value);
                //    }

                //}

                tmp = ExecuteAndReturn("ffmpeg", string.Format(" -i \"{0}\"", item));
                Regex r = new Regex("Input #0, .+?, from '" + Regex.Escape(item) + "':.+", RegexOptions.Singleline);

                var result = r.Match(tmp);
                tmp = result.Value;
                r = new Regex("\\s{4}Stream #0:(?<TrackID>[0-9])+(\\((?<language>[^()]+)\\))?: (?<Type>Video|Audio|Subtitle|Attachment):[^\\r\\n]+\\r\\n(\\s{4}Metadata:\\r\\n\\s{6}filename\\s*:\\s*(?<FileName>[^\\r\\n]+)\\r\\n\\s{6}mimetype[^\\r\\n]+)?", RegexOptions.Singleline);
                var resultCollection = r.Matches(tmp);
                //Stream #0:0: Video: mpeg4 (XVID / 0x44495658), yuv420p, 640x480, 23.98 fps, 23.98 tbr, 23.98 tbn, 23.98 tbc
                //Stream #0:1(English[eng]): Audio: vorbis, 48000 Hz, stereo, fltp, 176 kb/s
                //Stream #0:2(Japanese[jpn]): Audio: vorbis, 48000 Hz, stereo, fltp, 176 kb/s
                //Stream #0:3(English[eng]): Subtitle: text
                Match[] a = new Match[resultCollection.Count];
                resultCollection.CopyTo(a, 0);

                string audioTrack = "a";
                if (a.Count(g => g.Groups["Type"].Value == "Audio") > 1)
                {
                    var audioResult = a.Where(g => g.Groups["Type"].Value == "Audio").FirstOrDefault(g => g.Value.Contains("jpn") || g.Value.Contains("Japanese"));
                    if (audioResult != null)
                        audioTrack = audioResult.Groups["TrackID"].Value;
                }
                bool hasSubs = false;

                var subtitleResult = a.FirstOrDefault(g => g.Groups["Type"].Value == "Subtitle");
                if (File.Exists(item+".ass"))
                {
                    hasSubs = true;

                    subtitlefile = item + ".ass";

                }
                else if (subtitleResult != null)
                {
                    ExecuteAndReturn("ffmpeg", string.Format("-i \"{0}\" -y  -vn -an -codec:s:0.{1} ssa  \"{2}\"", item, subtitleResult.Groups["TrackID"].Value, subtitlefile));//-codec:s:0.{1}
                    hasSubs = true;
                    cleanup.Add(subtitlefile);
                }

                if (a.Count(g => g.Groups["Type"].Value == "Attachment") > 0)
                {
                    ExecuteAndReturn("ffmpeg", string.Format("-dump_attachment:t \"\" -i \"{0}\" -y", item), dir);

                    foreach (var f in a.Where(g => g.Groups["Type"].Value == "Attachment").Select(g => g.Groups["FileName"].Value))
                    {
                        cleanup.Add(dir + "\\" + f);
                        if (!File.Exists("C:\\Applications\\ffmpeg\\fonts\\" + f))
                            File.Copy(dir + "\\" + f, "C:\\Applications\\ffmpeg\\fonts\\" + f);

                    }
                }

                var videoStream =a.First(g => g.Groups["Type"].Value == "Video");
                r = new Regex("([^,]+,)+\\s(?<Height>[0-9]+)x(?<Width>[0-9]+)");
                result = r.Match(videoStream.Value);
                int width = int.Parse(result.Groups["Height"].Value);
                int height = int.Parse(result.Groups["Width"].Value);
                double aspectRation =(double) width / (double)height;

                int targetWidth = 640;
                int targetHeight = 360;
                targetHeight =((int) (320 / aspectRation) )* 2;
                if (targetHeight > 360)
                {
                    targetHeight = 360;
                    targetWidth = ((int)((180) * aspectRation)) * 2;
                }

                string targetSize = targetWidth.ToString() + "x" + targetHeight.ToString();

                //ExecuteAndReturn("mkvextract", string.Format("tracks \"{0}\" {1}:\"{2}\"", item, SubtitleTrack.ToString(), subtitlefile));
                subtitlefile = subtitlefile.Replace("\\", "\\\\").Replace(":", "\\:").Replace("'","\\'\\'\\'");



                var process = new System.Diagnostics.Process();

                Console.WriteLine(string.Format(command, item, filename, subtitlefile, audioTrack, targetSize));

                if (hasSubs)
                    process.StartInfo = new System.Diagnostics.ProcessStartInfo("ffmpeg", string.Format(command, item, filename, subtitlefile, audioTrack, targetSize));
                else
                    process.StartInfo = new System.Diagnostics.ProcessStartInfo("ffmpeg", string.Format(commandnoSubs, item, filename, audioTrack, targetSize));
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

        static string ExecuteAndReturn(string command, string args,string startAt = null)
        {
            var process = new System.Diagnostics.Process();


            process.StartInfo = new System.Diagnostics.ProcessStartInfo(command, args);
            if(startAt!=null)
                process.StartInfo.WorkingDirectory = startAt;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.OutputDataReceived += process_OutputDataReceived;
            process.Start();
            StringBuilder q = new StringBuilder();
            while (!process.HasExited)
            {
                //if (process.StandardError.EndOfStream)
                q.Append(process.StandardError.ReadToEnd());
                //if (process.StandardOutput.EndOfStream)
                q.Append(process.StandardOutput.ReadToEnd());

                Thread.Sleep(0);
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
