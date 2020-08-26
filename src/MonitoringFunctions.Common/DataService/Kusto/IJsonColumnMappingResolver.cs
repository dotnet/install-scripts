// Copyright (c) Microsoft. All rights reserved.

using Kusto.Data.Common;
using System.Collections.Generic;

namespace MonitoringFunctions.DataService.Kusto
{
    internal interface IJsonColumnMappingResolver
    {
        /// <summary>
        /// Returns Kusto column mapping objects for the given type where each column in Kusto table is mapped
        /// to a property when data of the given type is serialized to JSON.
        /// </summary>
        /// <typeparam name="T">The model class that will be mapped to a Kusto table after serialization.</typeparam>
        /// <returns>Column mapping for each of the columns in the Kusto table</returns>
        IEnumerable<ColumnMapping> GetColumnMappings<T>() where T : IKustoTableRow;
    }
}
