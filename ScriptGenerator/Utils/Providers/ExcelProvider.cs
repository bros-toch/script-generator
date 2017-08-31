using DataTable = System.Data.DataTable;

namespace ScriptGenerator.Utils.Providers
{
    public class ExcelProvider : IDataSource
    {
        public string Name => "Excel";
        public string FilterExtension => "Excel files (*.xls)|*.xls";
        public string Extensions => ".xls";
        public bool Enabled => false;
        public DataTable LoadFromFile(string filePath, int? limit)
        {
            return null;
        }
    }
}
