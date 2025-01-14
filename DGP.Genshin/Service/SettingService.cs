﻿using DGP.Genshin.Helper;
using DGP.Genshin.Service.Abstraction;
using Snap.Core.DependencyInjection;
using Snap.Core.Logging;
using Snap.Data.Json;
using System;
using System.Collections.Concurrent;
using System.IO;

namespace DGP.Genshin.Service
{
    /// <summary>
    /// 设置服务的默认实现
    /// </summary>
    [Service(typeof(ISettingService), InjectAs.Singleton)]
    internal class SettingService : ISettingService
    {
        private readonly string settingFile = PathContext.Locate("settings.json");

        private ConcurrentDictionary<string, object?> settings = new();

        public T Get<T>(SettingDefinition<T> definition)
        {
            string key = definition.Name;
            T defaultValue = definition.DefaultValue;
            Func<object, T>? converter = definition.Converter;

            if (!settings.TryGetValue(key, out object? value))
            {
                settings[key] = defaultValue;
                return defaultValue;
            }
            else
            {
                if (value is T tValue)
                {
                    return tValue;
                }
                else
                {
                    if (converter is null)
                    {
                        return (T)value!;
                    }
                    else
                    {
                        return converter.Invoke(value!);
                    }
                }
            }
        }

        public void Set<T>(SettingDefinition<T> definition, object? value, bool log = false)
        {
            string key = definition.Name;
            if (log)
            {
                this.Log($"setting {key} to {value} internally without notify");
            }
            settings[key] = value;
        }

        public void Initialize()
        {
            if (File.Exists(settingFile))
            {
                settings = Json.ToObjectOrNew<ConcurrentDictionary<string, object?>>(File.ReadAllText(settingFile));
            }
        }

        public void UnInitialize()
        {
            string settingString = Json.Stringify(settings);
            this.Log(settingString);
            File.WriteAllText(settingFile, settingString);
        }
    }
}
