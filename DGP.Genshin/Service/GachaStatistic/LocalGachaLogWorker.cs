﻿using DGP.Genshin.DataModel.GachaStatistic;
using DGP.Genshin.Helper;
using DGP.Genshin.MiHoYoAPI.Gacha;
using DGP.Genshin.Service.Abstraction;
using Microsoft.AppCenter.Crashes;
using ModernWpf.Controls;
using Newtonsoft.Json;
using OfficeOpenXml;
using Snap.Core.Logging;
using Snap.Data.Json;
using Snap.Data.Utility;
using Snap.Exception;
using Snap.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DGP.Genshin.Service.GachaStatistic
{
    /// <summary>
    /// 本地抽卡记录工作器
    /// </summary>
    internal class LocalGachaLogWorker
    {
        /// <summary>
        /// 导入导出操作锁
        /// </summary>
        private readonly object processing = new();
        private const string localFolder = "GachaStatistic";
        private const string metadataSheetName = "原始数据";

        #region Initialization
        /// <summary>
        /// 初始化 <see cref="LocalGachaLogWorker"/>的一个实例，并初始化本地数据
        /// 本地数据在初始化完成后随即可用
        /// </summary>
        /// <param name="data"></param>
        public LocalGachaLogWorker()
        {
            Directory.CreateDirectory(localFolder);
        }
        public async Task LoadAllAsync(GachaDataCollection gachaData)
        {
            await Task.Yield();
            foreach (string uidFolder in Directory.EnumerateDirectories($@"{localFolder}"))
            {
                string uid = new DirectoryInfo(uidFolder).Name;
                LoadLogOf(uid, gachaData);
            }
        }
        private void LoadLogOf(string uid, GachaDataCollection gachaData)
        {
            gachaData.Add(uid, new GachaData());
            foreach (string p in Directory.EnumerateFiles($@"{localFolder}\{uid}"))
            {
                FileInfo fileInfo = new(p);
                string pool = fileInfo.Name.Replace(".json", "");
                if (gachaData[uid] is GachaData user)
                {
                    user[pool] = Json.FromFile<List<GachaLogItem>>(fileInfo);
                }
            }
        }
        #endregion

        #region save
        public void SaveAll(GachaDataCollection gachaData)
        {
            foreach (UidGachaData entry in gachaData)
            {
                SaveLogOf(entry.Uid, gachaData);
            }
        }

        private void SaveLogOf(string uid, GachaDataCollection gachaData)
        {
            string uidFolder = PathContext.Locate(localFolder, uid);
            Directory.CreateDirectory(uidFolder);
            foreach ((string uidName, List<GachaLogItem>? logs) in gachaData[uid]!)
            {
                Json.ToFile($@"{uidFolder}\{uidName}.json", logs);
            }
        }
        #endregion

        #region import

        #region UIGF.W
        public async Task<(bool isOk, string uid)> ImportFromUIGFWAsync(string filePath, GachaDataCollection gachaData)
        {
            bool successful = true;
            string uid = "";
            try
            {
                using (FileStream fs = File.OpenRead(filePath))
                {
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    using (ExcelPackage package = new(fs))
                    {
                        ExcelWorksheets? sheets = package.Workbook.Worksheets;
                        if (sheets.Any(s => s.Name == metadataSheetName))
                        {
                            ExcelWorksheet metadataSheet = sheets[metadataSheetName]!;
                            int columnIndex = 1;
                            //记录属性的列号
                            Dictionary<string, int> propertyColumn = new()
                            {
                                { "uid", 0 },
                                { "gacha_type", 0 },
                                { "item_id", 0 },
                                { "count", 0 },
                                { "time", 0 },
                                { "name", 0 },
                                { "lang", 0 },
                                { "item_type", 0 },
                                { "rank_type", 0 },
                                { "id", 0 },
                                { "uigf_gacha_type", 0 }
                            };
                            columnIndex = DetectColumn(metadataSheet, columnIndex, propertyColumn);
                            List<UIGFItem> gachaLogs = EnumerateSheetData(metadataSheet, propertyColumn);
                            ImportableGachaData importData = BuildImportableDataByList(gachaLogs);
                            uid = await ImportImportableGachaDataAsync(importData, gachaData);
                        }
                        else
                        {
                            successful = false;
                        }
                    }
                }
                SaveAll(gachaData);
            }
            catch (Exception ex)
            {
                this.Log(ex);
                Crashes.TrackError(ex);
                successful = false;
            }
            return (successful, uid);
        }

        private ImportableGachaData BuildImportableDataByList(List<UIGFItem> gachaLogs)
        {
            ImportableGachaData importData = new();
            importData.Data = new();
            foreach (GachaLogItem item in gachaLogs)
            {
                importData.Uid ??= item.Uid;
                string? type = item.GachaType;
                //refactor 400 type here to redirect list addition
                type = type == "400" ? "301" : type;
                _ = type ?? throw new UnexpectedNullException("卡池类型不应为 null");
                if (!importData.Data.ContainsKey(type))
                {
                    importData.Data.Add(type, new());
                }
                importData.Data[type]!.Add(item);
            }
            foreach ((string poolId, List<GachaLogItem>? logs) in importData.Data)
            {
                importData.Data[poolId] = logs?.OrderByDescending(x => x.Id).ToList();
            }

            return importData;
        }

        private List<UIGFItem> EnumerateSheetData(ExcelWorksheet metadataSheet, Dictionary<string, int> propertyColumn)
        {
            int row = 2;
            List<UIGFItem> gachaLogs = new();
            //read data
            while (true)
            {
                //判定当前行首列的值，为空则记录已经到达末尾
                if (metadataSheet.Cells[row, 1].Value == null)
                {
                    break;
                }
                UIGFItem item = new();
                //reflection magic here.
                item.ForEachPropertyInfoWithAttribute<JsonPropertyAttribute>((itemProperty, attribute) =>
                {
                    int matchedPropertyColumn = propertyColumn[attribute.PropertyName!];
                    if (matchedPropertyColumn != 0)
                    {
                        switch (itemProperty.Name)
                        {
                            case nameof(item.Time):
                                {
                                    DateTime value = Convert.ToDateTime(metadataSheet.Cells[row, matchedPropertyColumn].GetValue<string>());
                                    itemProperty.SetValue(item, value);
                                    break;
                                }
                            default:
                                itemProperty.SetValue(item, metadataSheet.Cells[row, matchedPropertyColumn].Value);
                                break;
                        }
                    }
                });

                gachaLogs.Add(item);
                row++;
            }

            return gachaLogs;
        }

        private int DetectColumn(ExcelWorksheet metadataSheet, int columnIndex, Dictionary<string, int> propertyColumn)
        {
            //detect column property name
            while (true)
            {
                string header = metadataSheet.Cells[1, columnIndex].GetValue<string>();
                if (header is null)
                {
                    break;
                }
                propertyColumn[header] = columnIndex;
                columnIndex++;
            }

            return columnIndex;
        }

        #endregion

        #region UIGF.J
        public async Task<(bool isOk, string uid)> ImportFromUIGFJAsync(string filePath, GachaDataCollection gachaData)
        {
            return await ImportFromExternalDataAsync<UIGF>(filePath, gachaData, file =>
            {
                _ = file ?? throw new SnapGenshinInternalException("不正确的祈愿记录文件格式");
                ImportableGachaData importData = new();
                importData.Uid = file.Info!.Uid;
                importData.Data = new();
                if (file.List is not null)
                {
                    foreach (GachaLogItem? item in file.List)
                    {
                        if (item is not null)
                        {
                            item.Uid ??= importData.Uid;
                            string? type = item.GachaType;
                            //refactor 400 type here to prevent 400 list json file creation
                            type = type == ConfigType.CharacterEventWish2 ? ConfigType.CharacterEventWish : type;
                            _ = type ?? throw new UnexpectedNullException("卡池类型不应为 null");
                            if (!importData.Data.ContainsKey(type))
                            {
                                importData.Data.Add(type, new());
                            }
                            importData.Data[type]!.Add(item);
                        }
                    }
                    foreach ((string poolId, List<GachaLogItem>? logs) in importData.Data)
                    {
                        if (logs is not null)
                        {
                            //importData.Data[kvp.Key] = ;Fix ordered list got ignored issue
                            importData.Data[poolId] = logs.OrderByDescending(x => x.TimeId).ToList();
                        }
                    }
                }
                return importData;
            });
        }

        #region import core method
        /// <summary>
        /// 泛式方法，将其他类型的数据转化为可导入的类型后调用 <see cref="ImportImportableGachaDataAsync"/> 进行导入的实际操作
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath"></param>
        /// <param name="converter"></param>
        /// <returns></returns>
        public async Task<(bool isOk, string uid)> ImportFromExternalDataAsync<T>(string filePath, GachaDataCollection gachaData, Func<T?, ImportableGachaData> converter)
        {
            bool successful = true;
            string uid = "";

            try
            {
                T? file = Json.FromFile<T>(filePath);
                uid = await ImportImportableGachaDataAsync(converter.Invoke(file), gachaData);
                SaveAll(gachaData);
            }
            catch (Exception ex)
            {
                this.Log(ex);
                Crashes.TrackError(ex);
                successful = false;
            }

            return (successful, uid);
        }

        private async Task<string> ImportImportableGachaDataAsync(ImportableGachaData importable, GachaDataCollection gachaData)
        {
            await Task.Yield();
            if (importable.Uid is null)
            {
                throw new InvalidOperationException("未提供Uid");
            }

            if (importable.Data is GachaData data)
            {
                if (gachaData[importable.Uid] is not null)
                {
                    //we need to perform merge operation
                    foreach ((string poolId, List<GachaLogItem>? logs) in data)
                    {
                        TrimToBackIncrement(importable.Uid, poolId, gachaData, logs);
                        MergeBackIncrement(importable.Uid, poolId, gachaData, logs ?? new());
                    }
                }
                else
                {
                    //is new uid
                    gachaData.Add(importable.Uid, data);
                }
                return importable.Uid;
            }
            else
            {
                throw new InvalidOperationException("提供的数据不包含抽卡记录");
            }
        }

        /// <summary>
        /// 从数据尾部选出需要合并的部分
        /// 并裁剪列表
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="poolType"></param>
        /// <param name="importList"></param>
        /// <returns>经过修改的原列表</returns>
        private void TrimToBackIncrement(string uid, string poolType, GachaDataCollection gachaData, List<GachaLogItem>? importList)
        {
            GachaData source = gachaData[uid]!;
            List<GachaLogItem>? sourceItems = source[poolType];

            if (sourceItems?.Count > 0)
            {
                long sourceLastTimeId = sourceItems.Last().TimeId;
                //首个比当前最后的物品id早的物品
                int? index = importList?.FindIndex(i => i.TimeId < sourceLastTimeId);
                //fix repeat item issues
                if (index < 0)
                {
                    //查找相等
                    index = importList?.FindIndex(i => i.TimeId == sourceLastTimeId);
                    if (index < 0)
                    {
                        return;
                    }
                    importList?.RemoveRange(0, index!.Value + 1);
                    return;
                }
                //修改了原先的列表
                if (index is not null)
                {
                    importList?.RemoveRange(0, index.Value);
                }
            }
        }

        /// <summary>
        /// 合并老数据
        /// </summary>
        /// <param name="type">卡池</param>
        /// <param name="backIncrement">增量</param>
        private void MergeBackIncrement(string uid, string type, GachaDataCollection gachaData, List<GachaLogItem>? backIncrement)
        {
            GachaData dict = gachaData[uid]!;

            if (dict.ContainsKey(type) && backIncrement is not null)
            {
                dict[type]?.AddRange(backIncrement);
            }
            else
            {
                dict[type] = backIncrement;
            }
        }
        #endregion

        #endregion

        #endregion

        #region export
        public void ExportToUIGFJ(string uid, string fileName, GachaDataCollection gachaData)
        {
            lock (processing)
            {
                EnsureGachaItemId(uid, gachaData);

                UIGF? exportData = new()
                {
                    Info = new()
                    {
                        Uid = uid,
                        Language = "zh-cn",
                        ExportTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                        ExportTimeStamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        ExportApp = "Snap Genshin",
                        ExportAppVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
                        UIGFVersion = "v2.2"
                    },
                    List = gachaData[uid]!
                    .SelectMany(pair => pair.Value!
                    .Select(item => item.ToChild<GachaLogItem, UIGFItem>(u => u.UIGFGachaType = pair.Key)))
                };
                Json.ToFile(fileName, exportData);
            }
        }

        private void EnsureGachaItemId(string uid, GachaDataCollection gachaData)
        {
            foreach (List<GachaLogItem>? banner in gachaData[uid]!.Values)
            {
                if (banner is not null)
                {
                    //seek last valid Id item index
                    int index = 0;
                    while (index < banner.Count && !string.IsNullOrEmpty(banner[index].Id))
                    {
                        index++;
                    }
                    if (index < banner.Count)
                    {
                        index--;
                        //prepare id for generation,this id is separated for each banner
                        long preparedId = 1612303200000000000L;
                        if (index >= 0 && long.TryParse(banner[index].Id, out long lastValidId))
                        {
                            preparedId = lastValidId;
                        }
                        //fullfill empty id
                        index++;
                        for (int i = index; i < banner.Count; i++)
                        {
                            banner[i].Id = preparedId.ToString();
                        }
                    }
                }
            }
        }

        #region Excel
        public void ExportToUIGFW(string uid, string fileName, GachaDataCollection gachaData)
        {
            lock (processing)
            {
                EnsureGachaItemId(uid, gachaData);

                if (gachaData[uid] is not null)
                {
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    //FileStream can't be disposed by ExcelPackage,so we need to dispose it ourselves
                    using (FileStream fs = File.Create(fileName))
                    {
                        using (ExcelPackage package = new(fs))
                        {
                            foreach (string pool in gachaData[uid]!.Keys)
                            {
                                ExcelWorksheet sheet = package.Workbook.Worksheets.Add(ConfigType.Known[pool]);
                                IEnumerable<GachaLogItem>? logs = gachaData[uid]![pool];
                                //fix issue with compatibility
                                logs = logs?.Reverse();
                                InitializeGachaLogSheetHeader(sheet);
                                FillSheetWithGachaData(sheet, logs);
                                //freeze the title
                                sheet.View.FreezePanes(2, 1);
                            }

                            AddInterchangeableSheet(package, gachaData[uid]!);

                            package.Workbook.Properties.Title = "祈愿记录";
                            package.Workbook.Properties.Author = "Snap Genshin";
                            package.Save();
                        }
                    }
                }
            }
        }
        private void AddInterchangeableSheet(ExcelPackage package, GachaData data)
        {
            ExcelWorksheet interchangeSheet = package.Workbook.Worksheets.Add(metadataSheetName);
            //header
            InitializeInterchangeSheetHeader(interchangeSheet);
            IOrderedEnumerable<GachaLogItem> combinedLogs = data.SelectMany(x => x.Value ?? new()).OrderBy(x => x.Id);
            FillInterChangeSheet(interchangeSheet, combinedLogs);
        }
        private void InitializeInterchangeSheetHeader(ExcelWorksheet sheet)
        {
            sheet.Cells[1, 1].Value = "count";
            sheet.Cells[1, 2].Value = "gacha_type";
            sheet.Cells[1, 3].Value = "id";
            sheet.Cells[1, 4].Value = "item_id";
            sheet.Cells[1, 5].Value = "item_type";
            sheet.Cells[1, 6].Value = "lang";
            sheet.Cells[1, 7].Value = "name";
            sheet.Cells[1, 8].Value = "rank_type";
            sheet.Cells[1, 9].Value = "time";
            sheet.Cells[1, 10].Value = "uid";
            sheet.Cells[1, 11].Value = "uigf_gacha_type";
        }
        private void InitializeGachaLogSheetHeader(ExcelWorksheet sheet)
        {
            //header
            sheet.Cells[1, 1].Value = "时间";
            sheet.Cells[1, 2].Value = "名称";
            sheet.Cells[1, 3].Value = "物品类型";
            sheet.Cells[1, 4].Value = "星级";
            sheet.Cells[1, 5].Value = "祈愿类型";
        }

        /// <summary>
        /// 填充单个sheet
        /// </summary>
        /// <param name="sheet"></param>
        /// <param name="logs">包含单个 概率共享卡池 的物品的列表</param>
        private void FillSheetWithGachaData(ExcelWorksheet sheet, IEnumerable<GachaLogItem>? logs)
        {
            //content
            int? count = logs?.Count();
            int j = 1;
            if (count > 0 && logs is not null)
            {
                foreach (GachaLogItem item in logs)
                {
                    j++;
                    sheet.Cells[j, 1].Value = item.Time.ToString("yyyy-MM-dd HH:mm:ss");
                    sheet.Cells[j, 2].Value = item.Name;
                    sheet.Cells[j, 3].Value = item.ItemType;
                    sheet.Cells[j, 4].Value = int.Parse(item.Rank!);
                    sheet.Cells[j, 5].Value = item.GachaType?.ToString();

                    using (ExcelRange range = sheet.Cells[j, 1, j, 5])
                    {
                        range.Style.Font.Color.SetColor(ToDrawingColor(int.Parse(item.Rank!)));
                    }
                }
            }
            //自适应
            sheet.Cells[1, 1, j, 6].AutoFitColumns(0);
        }

        /// <summary>
        /// 填充单个sheet
        /// </summary>
        /// <param name="sheet"></param>
        /// <param name="logs">包含单个 概率共享卡池 的物品的列表</param>
        private void FillInterChangeSheet(ExcelWorksheet sheet, IEnumerable<GachaLogItem>? logs)
        {
            //content
            int? count = logs?.Count();
            int j = 1;
            if (count > 0 && logs is not null)
            {
                foreach (GachaLogItem item in logs)
                {
                    j++;
                    sheet.Cells[j, 1].Value = item.Count;
                    sheet.Cells[j, 2].Value = item.GachaType;
                    sheet.Cells[j, 3].Value = item.Id;
                    sheet.Cells[j, 4].Value = string.Empty;
                    sheet.Cells[j, 5].Value = item.ItemType;
                    sheet.Cells[j, 6].Value = item.Language;
                    sheet.Cells[j, 7].Value = item.Name;
                    sheet.Cells[j, 8].Value = item.Rank;
                    sheet.Cells[j, 9].Value = item.Time.ToString("yyyy-MM-dd HH:mm:ss");
                    sheet.Cells[j, 10].Value = item.Uid;
                    sheet.Cells[j, 11].Value = ConfigType.UIGFGachaTypeMap[item.GachaType!];

                    using (ExcelRange range = sheet.Cells[j, 1, j, 11])
                    {
                        range.Style.Font.Color.SetColor(ToDrawingColor(int.Parse(item.Rank!)));
                    }
                }
            }
            //自适应
            sheet.Cells[1, 1, j, 11].AutoFitColumns(0);
        }
        public System.Drawing.Color ToDrawingColor(int rank)
        {
            return rank switch
            {
                1 => System.Drawing.Color.FromArgb(255, 114, 119, 139),
                2 => System.Drawing.Color.FromArgb(255, 42, 143, 114),
                3 => System.Drawing.Color.FromArgb(255, 81, 128, 203),
                4 => System.Drawing.Color.FromArgb(255, 161, 86, 224),
                5 => System.Drawing.Color.FromArgb(255, 188, 105, 50),
                _ => throw new ArgumentOutOfRangeException(nameof(rank)),
            };
        }
        #endregion

        #endregion
    }
}