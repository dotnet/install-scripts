// Copyright (c) Microsoft. All rights reserved.

using Kusto.Data.Common;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MonitoringFunctions.DataService.Kusto
{
    /// <summary>
    /// Creates column mapping for objects whose json property names directly match the column name in the corresponding Kusto table.
    /// </summary>
    internal sealed class DirectJsonMappingResolver : IJsonColumnMappingResolver
    {
        public IEnumerable<ColumnMapping> GetColumnMappings<T>() where T : IKustoTableRow
        {
            DefaultContractResolver contractResolver = new DefaultContractResolver();
            var contract = contractResolver.ResolveContract(typeof(T)) as JsonObjectContract;

            if (contract == null)
            {
                throw new Exception($"Failed to resolve contract. Automatic column mapping is not possible with this type {typeof(T)}.");
            }

            foreach (var property in contract.Properties.Where(p => !p.Ignored && p.Readable))
            {
                yield return new ColumnMapping()
                {
                    ColumnName = property.PropertyName,
                    Properties = new Dictionary<string, string>() { { "Path", $"$.{property.PropertyName}" } }
                };
            }
        }
    }
}
