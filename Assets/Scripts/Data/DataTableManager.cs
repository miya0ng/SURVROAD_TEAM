using System.Collections.Generic;
using UnityEngine;

public static class DataTableManger
{
    //private static readonly string ItemCsv = "Items";
    private static readonly Dictionary<string, DataTable> tables =
        new Dictionary<string, DataTable>();

    static DataTableManger()
    {
        Init();
    }

    private static void Init()
    {
        //#if UNITY_EDITOR
        //        foreach (var id in DataTableIds.StringTableIds)
        //        {
        //            var table = new StringTable();
        //            table.Load(id);
        //            tables.Add(id, table);
        //        }
        //#else
        //        var stringTable = new StringTable();
        //        stringTable.Load(DataTableIds.String);
        //        tables.Add(DataTableIds.String, stringTable);
        //#endif

        var weaponTable = new WeaponDataTable();
        weaponTable.Load(WeaponDataTable.WeaponTableId);
        tables.Add(WeaponDataTable.WeaponTableId, weaponTable);
    }

    ////public static StringTable StringTable => Get<StringTable>(DataTableIds.String);
    //public static ItemTable ItemTable => Get<ItemTable>(ItemTable.ItemTableId);

    public static T Get<T>(string id) where T : DataTable
    {
        if (!tables.ContainsKey(id))
        {
            Debug.LogError("테이블 없음");
            return null;
        }
        return tables[id] as T;
    }
}