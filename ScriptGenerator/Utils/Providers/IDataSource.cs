using System.Data;

namespace ScriptGenerator.Utils.Providers
{
    public interface IDataSource
    {
        string Name { get; }
        string FilterExtension { get; }
        string Extensions { get; }
        DataTable LoadFromFile(string filePath, int? limit = 10);
        bool Enabled { get; }
    }
}