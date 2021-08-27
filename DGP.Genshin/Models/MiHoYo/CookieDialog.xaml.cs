﻿using DGP.Snap.Framework.Extensions.System;
using ModernWpf.Controls;
using System;
using System.Threading.Tasks;

namespace DGP.Genshin.Models.MiHoYo
{
    /// <summary>
    /// CookieDialog.xaml 的交互逻辑
    /// </summary>
    public partial class CookieDialog : ContentDialog
    {
        public CookieDialog()
        {
            InitializeComponent();
            this.Log("initialized");
        }

        public async Task<string> GetInputCookieAsync() =>
            await ShowAsync() == ContentDialogResult.Primary ? this.InputText.Text : String.Empty;
    }
}
