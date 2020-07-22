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
        public IEnumerable<ColumnMapping> GetColumnMappings<TModel>() where TModel : IKustoTableRow
        {
            DefaultContractResolver contractResolver = new DefaultContractResolver();
            JsonObjectContract? contract = contractResolver.ResolveContract(typeof(TModel)) as JsonObjectContract;

            if (contract == null)
            {
                throw new ArgumentException($"Failed to resolve contract. Automatic column mapping is not possible with this type {typeof(TModel)}.");
            }

            foreach (JsonProperty property in contract.Properties.Where(p => !p.Ignored && p.Readable))
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
