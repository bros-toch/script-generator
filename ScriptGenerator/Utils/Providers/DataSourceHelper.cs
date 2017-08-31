using System;
using System.Linq;

namespace ScriptGenerator.Utils.Providers
{
    public static class DataSourceHelper
    {
        public static IDataSource[] LoadeDataSources
        {
            get
            {
                var type = typeof(IDataSource);
                var types = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(s => s.GetTypes())
                    .Where(p => type.IsAssignableFrom(p) && p != type);

                return types.Select(x => (IDataSource) Activator.CreateInstance(x)).Where(x=> x.Enabled).ToArray();
            }
        }

        public static IDataSource FindByExtension(string extension)
        {
            var dataSource = LoadeDataSources.SingleOrDefault(x => x.Extensions.Split(';').Contains(extension));

            if (dataSource == null)
            {
                throw  new NotSupportedException(string.Format("{0} doesn't support", extension));
            }

            return dataSource;
        }
    }
}
