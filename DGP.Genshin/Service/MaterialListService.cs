﻿using DGP.Genshin.DataModel.Promotion;
using DGP.Genshin.Helper;
using DGP.Genshin.Service.Abstraction;
using Snap.Core.DependencyInjection;
using Snap.Data.Json;
using System.Collections.Generic;

namespace DGP.Genshin.Service
{
    [Service(typeof(IMaterialListService), InjectAs.Transient)]
    internal class MaterialListService : IMaterialListService
    {
        private const string MaterialListFileName = "MaterialList.json";

        public MaterialList Load()
        {
            return new(Json.FromFileOrNew<List<CalculableConsume>>(PathContext.Locate(MaterialListFileName)));
        }

        public void Save(MaterialList? materialList)
        {
            Json.ToFile(MaterialListFileName, materialList);
        }
    }
}
