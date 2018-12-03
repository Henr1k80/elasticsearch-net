using System.Collections.Generic;
using System.Runtime.Serialization;
using Elasticsearch.Net;
using Utf8Json;

namespace Nest
{
	public class MetadataIndexState
	{
		[DataMember(Name = "aliases")]
		public IEnumerable<string> Aliases { get; internal set; }

		[DataMember(Name = "mappings")]
		public IMappings Mappings { get; internal set; }

		// TODO: Why this uses DynamicBody
		[DataMember(Name = "settings")]
		[JsonFormatter(typeof(VerbatimDictionaryKeysFormatter<string, object>))]
		public DynamicBody Settings { get; internal set; }

		[DataMember(Name = "state")]
		public string State { get; internal set; }
	}
}
