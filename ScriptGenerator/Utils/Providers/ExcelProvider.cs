using System.Data;
using System.IO;
using System.Linq;
using ExcelDataReader;
using DataTable = System.Data.DataTable;

namespace ScriptGenerator.Utils.Providers
{
    public class ExcelProvider : IDataSource
    {
        public string Name => "Excel";
        public string FilterExtension => "Excel files (*.xls, *.xlsx)|*.xls;*.xlsx";
        public string Extensions => ".xls;.xlsx";
        public bool Enabled => true;

        public DataTable LoadFromFile(string filePath, int? limit)
        {
            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    var dataTable = reader.AsDataSet(new ExcelDataSetConfiguration()
                    {

                        // Gets or sets a value indicating whether to set the DataColumn.DataType 
                        // property in a second pass.
                        UseColumnDataType = true,

                        // Gets or sets a callback to obtain configuration options for a DataTable. 
                        ConfigureDataTable = (tableReader) => new ExcelDataTableConfiguration()
                        {

                            // Gets or sets a value indicating the prefix of generated column names.
                            EmptyColumnNamePrefix = "Column",

                            // Gets or sets a value indicating whether to use a row from the 
                            // data as column names.
                            UseHeaderRow = true,

                            // Gets or sets a callback to determine which row is the header row. 
                            // Only called when UseHeaderRow = true.
                            ReadHeaderRow = (rowReader) => {
                                // F.ex skip the first row and use the 2nd row as column headers:
                                //rowReader.Read();
                            }
                        }
                    }).Tables[0];

                    if (limit.HasValue)
                    {
                        dataTable = dataTable.Select().Take(limit.Value).CopyToDataTable();
                    }

                    return dataTable;
                }
            }
        }
    }
}
