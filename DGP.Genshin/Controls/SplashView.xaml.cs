﻿using DGP.Genshin.Models.MiHoYo;
using DGP.Genshin.Services;
using DGP.Genshin.Services.Settings;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace DGP.Genshin.Controls
{
    /// <summary>
    /// SplashView.xaml 的交互逻辑
    /// </summary>
    public partial class SplashView : UserControl, INotifyPropertyChanged
    {
        public MetaDataService MetaDataService { get; set; } = MetaDataService.Instance;
        private bool integrityCheckCompleted = false;
        private bool isCookieVisible = true;
        private bool isLauncherPathVisible = true;
        private bool hasCheckCompleted;

        public bool IsCookieVisible
        {
            get => this.isCookieVisible; set
            {
                Set(ref this.isCookieVisible, value);
                OnInitializeStateChanged();
            }
        }
        public bool IsLauncherPathVisible
        {
            get => this.isLauncherPathVisible; set
            {
                Set(ref this.isLauncherPathVisible, value);
                OnInitializeStateChanged();
            }
        }

        public bool HasCheckCompleted { get => this.hasCheckCompleted; set => Set(ref this.hasCheckCompleted, value); }

        public SplashView()
        {
            this.DataContext = this;
            MetaDataService.Instance.CompleteStateChanged += async isCompleted =>
            {
                if (isCompleted)
                {
                    //wait for animation
                    await Task.Delay(1000);
                    this.integrityCheckCompleted = true;
                    OnInitializeStateChanged();
                }
            };
            this.IsCookieVisible = !CookieManager.IsCookieAvailable;
            this.IsLauncherPathVisible = SettingService.Instance.Equals<string>(Setting.LauncherPath, null, null) ?? true;
            InitializeComponent();
        }

        private async void UserControlLoaded(object sender, RoutedEventArgs e) => await MetaDataService.Instance.CheckAllIntegrityAsync();

        public event Action InitializeCompleted;

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected void Set<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return;
            }

            storage = value;
            OnPropertyChanged(propertyName);
        }

        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        #endregion

        private void CookieButtonClick(object sender, RoutedEventArgs e) => this.IsCookieVisible = !CookieManager.IsCookieAvailable;

        private void LauncherPathButtonClick(object sender, RoutedEventArgs e)
        {
            string launcherPath = SettingService.Instance.GetOrDefault<string>(Setting.LauncherPath, null);
            if (!File.Exists(launcherPath) || Path.GetFileNameWithoutExtension(launcherPath) != "launcher")
            {
                OpenFileDialog openFileDialog = new OpenFileDialog()
                {
                    Filter = "启动器|launcher.exe",
                    Title = "选择启动器文件",
                    CheckPathExists = true,
                    FileName = $"launcher.exe"
                };
                if (openFileDialog.ShowDialog() == true)
                {
                    launcherPath = openFileDialog.FileName;
                    SettingService.Instance[Setting.LauncherPath] = launcherPath;
                    this.IsLauncherPathVisible = SettingService.Instance.Equals<string>(Setting.LauncherPath, null, null) ?? true;
                }
            }
        }

        private void OnInitializeStateChanged()
        {
            if (this.IsCookieVisible == false && this.IsLauncherPathVisible == false && this.integrityCheckCompleted)
            {
                this.HasCheckCompleted = true;
                InitializeCompleted?.Invoke();
            }
        }
    }
}
