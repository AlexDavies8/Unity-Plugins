using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Plugins.PluginManager
{
    public static class GitHelper
    {
        const string GitCommand = "git";
        const string SubmoduleStatusArgs = "submodule status";
        const string SubmoduleUpdateArgs = "submodule update --init {0}";

        public static List<string> GetAllSubmodules(string rootPath)
        {
            List<string> submodules = new List<string>();

            var process = new Process();
            process.StartInfo.FileName = GitCommand;
            process.StartInfo.Arguments = SubmoduleStatusArgs;
            process.StartInfo.WorkingDirectory = rootPath;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;
            process.Start();

            var stream = process.StandardOutput;
            while (!stream.EndOfStream)
            {
                string[] parts = stream.ReadLine().Trim().Split();
                submodules.Add(parts[1].Trim());
            }

            process.WaitForExit();

            return submodules;
        }
        public static List<string> GetInstalledSubmodules(string rootPath)
        {
            List<string> submodules = new List<string>();

            var process = new Process();
            process.StartInfo.FileName = GitCommand;
            process.StartInfo.Arguments = SubmoduleStatusArgs;
            process.StartInfo.WorkingDirectory = rootPath;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;
            process.Start();

            var stream = process.StandardOutput;
            while (!stream.EndOfStream)
            {
                string[] parts = stream.ReadLine().Trim().Split();
                if (parts[0][0] != '-') submodules.Add(parts[1].Trim());
            }

            process.WaitForExit();

            return submodules;
        }

        public static void GetAllSubmodulesAsync(string rootPath, Action<List<string>> callback)
        {
            List<string> submodules = new List<string>();

            var process = new Process();
            process.StartInfo.FileName = GitCommand;
            process.StartInfo.Arguments = SubmoduleStatusArgs;
            process.StartInfo.WorkingDirectory = rootPath;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;
            process.EnableRaisingEvents = true;
            process.Exited += (obj, e) =>
            {
                callback.Invoke(submodules);
                process.Close();
            };

            process.OutputDataReceived += new DataReceivedEventHandler((obj, e) =>
            {
                var lines = e.Data.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    string[] parts = lines[i].Trim().Split();
                    submodules.Add(parts[1].Trim());
                }
            }); 
            
            process.Start();
            process.BeginOutputReadLine();

            //process.WaitForExit();
        }
        public static void GetInstalledSubmodulesAsync(string rootPath, Action<List<string>> callback)
        {
            List<string> submodules = new List<string>();

            var process = new Process();
            process.StartInfo.FileName = GitCommand;
            process.StartInfo.Arguments = SubmoduleStatusArgs;
            process.StartInfo.WorkingDirectory = rootPath;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;
            process.EnableRaisingEvents = true;
            process.Exited += (obj, e) =>
            {
                callback.Invoke(submodules);
                process.Close();
            };

            process.OutputDataReceived += new DataReceivedEventHandler((obj, e) =>
            {
                var lines = e.Data.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    string[] parts = lines[i].Trim().Split();
                    if (parts[0][0] != '-') submodules.Add(parts[1].Trim());
                }
            });

            process.Start();
            process.BeginOutputReadLine();

            //process.WaitForExit();
        }
        public static void UpdateSubmoduleAsync(string rootPath, string modulePath, Action callback = null)
        {
            var process = new Process();
            process.StartInfo.FileName = GitCommand;
            process.StartInfo.Arguments = string.Format(SubmoduleUpdateArgs, modulePath);
            process.StartInfo.WorkingDirectory = rootPath;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.EnableRaisingEvents = true;
            process.Exited += (obj, e) =>
            {
                process.Close();
                AssetDatabase.Refresh();
                callback?.Invoke();
            };
            process.Start();
            AssetDatabase.Refresh();
        }
        public static void RemoveSubmodule(string rootPath, string modulePath)
        {
            Directory.Delete(Path.Combine(rootPath, modulePath), true);
            AssetDatabase.Refresh();
        }
    }
}
