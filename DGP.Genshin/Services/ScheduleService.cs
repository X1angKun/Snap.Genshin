﻿using DGP.Genshin.Messages;
using DGP.Genshin.Services.Abstratcions;
using Microsoft.Toolkit.Mvvm.Messaging;
using Snap.Core.DependencyInjection;
using System;
using System.Windows.Threading;

namespace DGP.Genshin.Services
{
    [Service(typeof(IScheduleService), InjectAs.Singleton)]
    internal class ScheduleService : IScheduleService, IRecipient<SettingChangedMessage>
    {
        private readonly DispatcherTimer timer;
        public ScheduleService(ISettingService settingService)
        {
            timer = new();
            double minutes = settingService.GetOrDefault(Setting.ResinRefreshMinutes, 8d);
            timer.Interval = TimeSpan.FromMinutes(minutes);
            timer.Tick += (s, e) => App.Messenger.Send(new TickScheduledMessage());
        }

        public void Initialize()
        {
            timer.Start();
        }

        public void Receive(SettingChangedMessage message)
        {
            if (message.Value.Key == Setting.ResinRefreshMinutes)
            {
                //unbox to double
                double minutes = (double)message.Value.Value!;
                timer.Interval = TimeSpan.FromMinutes(minutes);
            }
        }

        public void UnInitialize()
        {
            timer.Stop();
        }
    }
}