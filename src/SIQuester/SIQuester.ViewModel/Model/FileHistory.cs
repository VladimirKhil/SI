using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;

namespace SIQuester.Model
{
    public sealed class FileHistory
    {
        public ObservableCollection<string> Files { get; set; }

        public FileHistory()
        {
            Files = new ObservableCollection<string>();
        }

        public void Add(string path)
        {
            var index = Files.IndexOf(path);
            if (index > -1)
            {
                Files.Move(index, 0);
            }
            else
            {
                if (Files.Count == 10)
                    Files.RemoveAt(9);

                Files.Insert(0, path);
            }
        }

        public void Remove(string path)
        {
            Files.Remove(path);
        }
    }
}
