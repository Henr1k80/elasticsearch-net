using System.Collections.Generic;
using System.Runtime.Serialization;
using Utf8Json;

namespace Nest
{
	public class RoutingTableState
	{
		[DataMember(Name ="indices")]
		[JsonFormatter(typeof(VerbatimDictionaryKeysFormatter<string, IndexRoutingTable>))]
		public IReadOnlyDictionary<string, IndexRoutingTable> Indices { get; internal set; } = EmptyReadOnly<string, IndexRoutingTable>.Dictionary;
	}
}
