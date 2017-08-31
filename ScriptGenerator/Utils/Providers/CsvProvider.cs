using System;
using System.Data;
using Microsoft.VisualBasic.FileIO;

namespace ScriptGenerator.Utils.Providers
{
    public class CsvDataSource : IDataSource
    {
        public string Name => "CSV";
        public string FilterExtension => "CSV Files (*.csv)|*.csv";
        public string Extensions => ".csv";
        public bool Enabled => true;
        public DataTable LoadFromFile(string filePath, int? limit)
        {
            var csvData = new DataTable();

            try
            {
                using (var csvReader = new TextFieldParser(filePath))
                {
                    csvReader.SetDelimiters(",");
                    csvReader.HasFieldsEnclosedInQuotes = true;

                    var colFields = csvReader.ReadFields();

                    foreach (var column in colFields)
                    {
                        var serialno = new DataColumn(column) {AllowDBNull = true};
                        csvData.Columns.Add(serialno);
                    }
                    var count = 0;
                    while (!csvReader.EndOfData)
                    {
                        if (limit.HasValue && ++count > limit)
                        {
                            return csvData;
                        }

                        var fieldData = csvReader.ReadFields();
                        var dr = csvData.NewRow();
                        //Making empty value as empty
                        for (var i = 0; i < fieldData.Length; i++)
                        {
                            if (fieldData[i] == null)
                                fieldData[i] = string.Empty;

                            dr[i] = fieldData[i];
                        }
                        csvData.Rows.Add(dr);
                    }

                }
            }
            catch (Exception ex)
            {
                //ignore
            }
            return csvData;
        }
    }
}