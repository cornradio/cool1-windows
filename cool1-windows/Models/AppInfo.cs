using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Cool1Windows.Models
{
    public class AppInfo : INotifyPropertyChanged
    {
        private bool _isRunning;
        private bool _isFavorite;
        private DateTime? _lastLaunched;

        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string? RealProcessName { get; set; } // 存储真正的进程名 (不含.exe)

        public bool IsFavorite
        {
            get => _isFavorite;
            set { _isFavorite = value; OnPropertyChanged(); }
        }

        public DateTime? LastLaunched
        {
            get => _lastLaunched;
            set { _lastLaunched = value; OnPropertyChanged(); }
        }

        public bool IsRunning
        {
            get => _isRunning;
            set { _isRunning = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override bool Equals(object? obj)
        {
            if (obj is AppInfo other)
            {
                return Path == other.Path;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Path.GetHashCode();
        }
    }
}
