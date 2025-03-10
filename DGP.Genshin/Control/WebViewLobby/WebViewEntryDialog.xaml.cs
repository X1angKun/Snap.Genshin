﻿using DGP.Genshin.DataModel.WebViewLobby;
using Microsoft.VisualStudio.Threading;
using ModernWpf.Controls;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace DGP.Genshin.Control.WebViewLobby
{
    public partial class WebViewEntryDialog : ContentDialog
    {
        public WebViewEntryDialog(WebViewEntry? entry = null)
        {
            if (entry is not null)
            {
                NavigateUrl = entry.NavigateUrl;
                EntryName = entry.Name;
                IconUrl = entry.IconUrl;
                JavaScript = entry.JavaScript;
                ShowInNavView = entry.ShowInNavView;
            }
            DataContext = this;
            InitializeComponent();
        }

        public string NavigateUrl
        {
            get => (string)GetValue(NavigateUrlProperty);

            set => SetValue(NavigateUrlProperty, value);
        }
        public static readonly DependencyProperty NavigateUrlProperty =
            DependencyProperty.Register(nameof(NavigateUrl), typeof(string), typeof(WebViewEntryDialog), new PropertyMetadata(null));

        public string EntryName
        {
            get => (string)GetValue(EntryNameProperty);

            set => SetValue(EntryNameProperty, value);
        }
        public static readonly DependencyProperty EntryNameProperty =
            DependencyProperty.Register(nameof(EntryName), typeof(string), typeof(WebViewEntryDialog), new PropertyMetadata(null));

        [AllowNull]
        public string IconUrl
        {
            get => (string)GetValue(IconUrlProperty);

            set => SetValue(IconUrlProperty, value);
        }
        public static readonly DependencyProperty IconUrlProperty =
            DependencyProperty.Register(nameof(IconUrl), typeof(string), typeof(WebViewEntryDialog), new PropertyMetadata(null));

        [AllowNull]
        public string JavaScript
        {
            get => (string)GetValue(JavaScriptProperty);

            set => SetValue(JavaScriptProperty, value);
        }
        public static readonly DependencyProperty JavaScriptProperty =
            DependencyProperty.Register(nameof(JavaScript), typeof(string), typeof(WebViewEntryDialog), new PropertyMetadata(null));

        public bool ShowInNavView
        {
            get => (bool)GetValue(ShowInNavViewProperty);

            set => SetValue(ShowInNavViewProperty, value);
        }
        public static readonly DependencyProperty ShowInNavViewProperty =
            DependencyProperty.Register("ShowInNavView", typeof(bool), typeof(WebViewEntryDialog), new PropertyMetadata(true));

        public async Task<WebViewEntry?> GetWebViewEntryAsync()
        {
            if (ContentDialogResult.Secondary != await ShowAsync())
            {
                if (NavigateUrl is not null)
                {
                    if (JavaScript is not null)
                    {
                        JavaScript = new Regex("(\r\n|\r|\n)").Replace(JavaScript, " ");
                        JavaScript = new Regex(@"\s+").Replace(JavaScript, " ");
                    }
                    return new WebViewEntry(EntryName, NavigateUrl, IconUrl, JavaScript, ShowInNavView);
                }
            }
            return null;
        }

        private void AutoTitleIconButtonClick(object sender, RoutedEventArgs e)
        {
            FindTitleIconAsync().Forget();
        }

        private async Task FindTitleIconAsync()
        {
            if (NavigateUrl is not null)
            {
                using (HttpClient client = new())
                {
                    if (Uri.TryCreate(NavigateUrl, UriKind.Absolute, out Uri? navigateUri))
                    {
                        string response;
                        try
                        {
                            response = await client.GetStringAsync(navigateUri);
                        }
                        catch
                        {
                            response = string.Empty;
                        }

                        Match m;
                        //匹配标题
                        m = new Regex("(?<=<title>)(.*)(?=</title>)").Match(response);
                        EntryName = m.Success ? m.Value : "自动获取失败";
                        //匹配图标
                        m = new Regex("(?<=rel[ =]+\"[shortcut icon]+\" href=\")(.*?)(?=\")").Match(response);
                        if (m.Success)
                        {
                            string? path = m.Value;
                            if (Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out Uri? pathUri))
                            {
                                Uri iconUri = pathUri.IsAbsoluteUri
                                    ? pathUri
                                    : new Uri(new Uri(NavigateUrl), pathUri);
                                IconUrl = iconUri.ToString();
                            }
                        }
                    }
                    else
                    {
                        NavigateUrl = "该Url无效";
                    }
                }
            }
        }
    }
}
