using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Avalonia.Input;
using JetBrains.Annotations;

namespace engenious.ContentTool.Avalonia
{
    public class RecentFiles : ObservableCollection<RecentFiles.RecentFile>
    {
        public class RecentFile : INotifyPropertyChanged
        {
            private int _index = -1;

            public RecentFile(string fileName)
            {
                FileName = fileName;
            }

            public int Index
            {
                get => _index;
                internal set
                {
                    if (_index == value) return;
                    _index = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HotKey));
                }
            }

            public string FileName { get; }
            
            [CanBeNull] public KeyGesture HotKey => Index == 0 ? KeyGesture.Parse("Ctrl+Shift+O") : null;
            
            public event PropertyChangedEventHandler? PropertyChanged;

            [NotifyPropertyChangedInvocator]
            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            public override string ToString()
            {
                return $"{Index} - {FileName}";
            }
        }
        
        private readonly string _path;
        private readonly int _maxRecent;
        private bool _silent;
        public RecentFiles(string path, int maxRecent = 10)
        {
            _silent = false;
            _path = path;
            _maxRecent = maxRecent;
        }

        private void AddSilent(RecentFile recentFile)
        {
            _silent = true;
            base.Add(recentFile);
            _silent = false;
        }
        
        public static RecentFiles Deserialize(string path, int maxRecent = 10)
        {
            
            var des = new RecentFiles(path, maxRecent);
            if (!File.Exists(path))
                return des;

            using var fs = new FileStream(path, FileMode.Open);
            using var sw = new StreamReader(fs, Encoding.UTF8);
            string? line = null;
            while ((line = sw.ReadLine()) != null)
            {
                if (File.Exists(line))
                    des.AddSilent(new RecentFile(line));
            }

            return des;
        }

        public void Serialize(string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (dir != null)
                Directory.CreateDirectory(dir);
            
            using var fs = new FileStream(path, FileMode.Create);
            using var sw = new StreamWriter(fs, Encoding.UTF8);
            foreach(var file in this)
                sw.WriteLine(file.FileName);
        }

        public void Serialize()
        {
            Serialize(_path);
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnCollectionChanged(e);

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Move:
                    if (e.NewItems == null)
                        return;
                    for (int i = 0; i < e.NewItems.Count; i++)
                    {
                        if (!(e.NewItems[i] is RecentFile item))
                            continue;
                        item.Index = e.NewStartingIndex + i;
                    }
                    break;
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems == null)
                        return;
                    for (int i = 0; i < e.NewItems.Count; i++)
                    {
                        if (!(e.NewItems[i] is RecentFile item))
                            continue;
                        item.Index = e.NewStartingIndex + i;
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    if (e.NewItems == null)
                        return;
                    for (int i = 0; i < e.NewItems.Count; i++)
                    {
                        if (!(e.NewItems[i] is RecentFile item))
                            continue;
                        item.Index = e.NewStartingIndex + i;
                    }
                    break;
                default:
                    break;
            }
            if (!_silent)
                Serialize();
        }

        private void CutOffMax()
        {
            if (Count > _maxRecent)
            {
                for (int i = Count - 1; i >= _maxRecent; i--)
                    RemoveAt(i);
            }
        }

        public void AddRecent(string path)
        {
            for (int i = 0; i < Count; i++)
            {
                if (Items[i].FileName == path)
                {
                    if (i > 0)
                        MoveItem(i, 0);
                    return;
                }
            }
            
            // Element does not already exist
            
            Insert(0, new RecentFile(path));


            CutOffMax();
        }

        public void RemoveRecent(string path)
        {
            for (int i = 0; i < Count; i++)
            {
                if (Items[i].FileName == path)
                {
                    RemoveAt(i);
                    return;
                }
            }
        }
    }
}