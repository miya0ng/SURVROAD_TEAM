using CsvHelper;
using NUnit.Framework;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

public abstract class DataTable
{
    public static readonly string dataTablePath = "Data/{0}";
    public abstract void Load(string fileName);

    public static List<T> LoadCSV<T>(string csvText)
    {
        using (var reader = new StringReader(csvText))
        {
            using (var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                var records = csvReader.GetRecords<T>();
                return records.ToList();
            }
        }
    }
}
