﻿using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutoGitWatcher
{
    public class AutoGitWatcher
    {
        private readonly List<FileSystemWatcher> fileSystemWatcherList = new();
        private readonly Dictionary<FileSystemWatcher, DateTime> fileSystemWatcherLastChangeDictionary = new ();
        private Task? gitTask = null;

        public void StartWatch(string[] directoryArray) 
        {
            StopWatch();
            foreach (string directory in directoryArray)
            {
                FileSystemWatcher fileSystemWatcher = new (directory);
                fileSystemWatcher.IncludeSubdirectories = true;
                fileSystemWatcher.Changed += FileSystemWatcher_Changed;
                fileSystemWatcher.Created += FileSystemWatcher_Created;
                fileSystemWatcher.Renamed += FileSystemWatcher_Renamed;
                fileSystemWatcher.Deleted += FileSystemWatcher_Deleted;
                fileSystemWatcher.Error += FileSystemWatcher_Error;
                fileSystemWatcher.EnableRaisingEvents = true;
                fileSystemWatcherList.Add(fileSystemWatcher);
            }
            if (gitTask == null)
            {
                gitTask = Task.Factory.StartNew(() => GitTaskRun());
            }
        }

        private void StopWatch() 
        {
            foreach (FileSystemWatcher fileSystemWatcher in fileSystemWatcherList)
            {
                fileSystemWatcher.EnableRaisingEvents = false;
                fileSystemWatcher.Changed -= FileSystemWatcher_Changed;
                fileSystemWatcher.Created -= FileSystemWatcher_Created;
                fileSystemWatcher.Renamed -= FileSystemWatcher_Renamed;
                fileSystemWatcher.Deleted -= FileSystemWatcher_Deleted;
                fileSystemWatcher.Error -= FileSystemWatcher_Error;
            }
            fileSystemWatcherList.Clear();
            lock (fileSystemWatcherLastChangeDictionary)
            {
                fileSystemWatcherLastChangeDictionary.Clear();
            }
        }

        private void FileSystemWatcher_Error(object sender, ErrorEventArgs e)
        {
            throw e.GetException();
        }

        private void FileSystemWatcher_Deleted(object sender, FileSystemEventArgs e)
        {
            RecordChange((FileSystemWatcher)sender, e.FullPath);
        }

        private void FileSystemWatcher_Renamed(object sender, RenamedEventArgs e)
        {
            RecordChange((FileSystemWatcher)sender, e.FullPath);
        }

        private void FileSystemWatcher_Created(object sender, FileSystemEventArgs e)
        {
            RecordChange((FileSystemWatcher)sender, e.FullPath);
        }

        private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            RecordChange((FileSystemWatcher)sender, e.FullPath);
        }

        private void RecordChange(FileSystemWatcher fileSystemWatcher, string path) 
        {
            // Ignore changes in the git repository because of the commit
            if (path.Contains(".git"))
            {
                return;
            }
            lock (fileSystemWatcherLastChangeDictionary)
            {
                fileSystemWatcherLastChangeDictionary[fileSystemWatcher] = DateTime.Now;
            }
        }

        private void GitTaskRun() 
        {
            while (true)
            {
                Thread.Sleep(1000);
                lock (fileSystemWatcherLastChangeDictionary)
                {
                    List<FileSystemWatcher> toDelete = new();
                    foreach (KeyValuePair<FileSystemWatcher, DateTime> keyValuePair in fileSystemWatcherLastChangeDictionary)
                    {
                        if (DateTime.Now - keyValuePair.Value > new TimeSpan(0, 0, 2))
                        {
                            string directory = keyValuePair.Key.Path;
                            Repository repository = new(directory);
                            Commands.Stage(repository, "*");
                            Signature signature = repository.Config.BuildSignature(DateTimeOffset.Now);
                            repository.Commit("AutoGitWatcher", signature, signature);
                            repository.Network.Push(repository.Head);
                            toDelete.Add(keyValuePair.Key);
                        }
                    }
                    toDelete.ForEach(x => fileSystemWatcherLastChangeDictionary.Remove(x));
                }
            }
        }
    }
}