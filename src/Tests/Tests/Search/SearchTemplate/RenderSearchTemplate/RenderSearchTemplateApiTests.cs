﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elastic.Xunit.XunitPlumbing;
using Elasticsearch.Net;
using FluentAssertions;
using Nest;
using Tests.Core.ManagedElasticsearch.Clusters;
using Tests.Framework;
using Tests.Framework.Integration;

namespace Tests.Search.SearchTemplate.RenderSearchTemplate
{
	public class RenderSearchTemplateApiTests
		: ApiIntegrationTestBase<ReadOnlyCluster, IRenderSearchTemplateResponse, IRenderSearchTemplateRequest, RenderSearchTemplateDescriptor,
			RenderSearchTemplateRequest>
	{
		public RenderSearchTemplateApiTests(ReadOnlyCluster cluster, EndpointUsage usage) : base(cluster, usage) { }

		protected override LazyResponses ClientUsage() => Calls(
			fluent: (c, f) => c.RenderSearchTemplate(f),
			fluentAsync: (c, f) => c.RenderSearchTemplateAsync(f),
			request: (c, r) => c.RenderSearchTemplate(r),
			requestAsync: (c, r) => c.RenderSearchTemplateAsync(r)
		);

		protected override HttpMethod HttpMethod => HttpMethod.POST;
		protected override string UrlPath => $"/_render/template";
		protected override int ExpectStatusCode => 200;
		protected override bool ExpectIsValid => true;

		private static readonly string inlineSearchTemplate = @"
{
	""query"": {
	  ""terms"": {
		""status"": [
		  ""{{#status}}"",
		  ""{{.}}"",
		  ""{{/status}}""
		]
	  }
	}
  }";

		private readonly string[] statusValues = new[] { "pending", "published" };

		protected override Func<RenderSearchTemplateDescriptor, IRenderSearchTemplateRequest> Fluent => s => s
			.Source(inlineSearchTemplate)
			.Params(p => p
				.Add("status", statusValues)
			);


		protected override RenderSearchTemplateRequest Initializer => new RenderSearchTemplateRequest
		{
			Source = inlineSearchTemplate,
			Params = new Dictionary<string, object>
			{
				{ "status", statusValues }
			}
		};

		[I] public Task AssertResponse() => AssertOnAllResponses(r =>
		{
			r.TemplateOutput.Should().NotBeNull();
			var searchRequest = r.TemplateOutput.As<ISearchRequest>();
			searchRequest.Should().NotBeNull();

			searchRequest.Query.Should().NotBeNull();
		});
	}
}
