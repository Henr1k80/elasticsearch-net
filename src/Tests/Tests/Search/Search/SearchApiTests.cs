﻿using System;
using System.Collections.Generic;
using System.Linq;
using Elastic.Xunit.XunitPlumbing;
using Elasticsearch.Net;
using FluentAssertions;
using Nest;
using Tests.Core.Extensions;
using Tests.Core.ManagedElasticsearch.Clusters;
using Tests.Domain;
using Tests.Framework;
using Tests.Framework.Integration;

namespace Tests.Search.Search
{
	public class SearchApiTests
		: ApiIntegrationTestBase<ReadOnlyCluster, ISearchResponse<Project>, ISearchRequest, SearchDescriptor<Project>, SearchRequest<Project>>
	{
		public SearchApiTests(ReadOnlyCluster cluster, EndpointUsage usage) : base(cluster, usage) { }

		protected override LazyResponses ClientUsage() => Calls(
			fluent: (c, f) => c.Search(f),
			fluentAsync: (c, f) => c.SearchAsync(f),
			request: (c, r) => c.Search<Project>(r),
			requestAsync: (c, r) => c.SearchAsync<Project>(r)
		);

		protected override int ExpectStatusCode => 200;
		protected override bool ExpectIsValid => true;
		protected override HttpMethod HttpMethod => HttpMethod.POST;
		protected override string UrlPath => $"/project/doc/_search";

		protected override object ExpectJson => new
		{
			from = 10,
			size = 20,
			query = new
			{
				match_all = new { }
			},
			aggs = new
			{
				startDates = new
				{
					terms = new
					{
						field = "startedOn"
					}
				}
			},
			post_filter = new
			{
				term = new
				{
					state = new
					{
						value = "Stable"
					}
				}
			}
		};

		protected override void ExpectResponse(ISearchResponse<Project> response)
		{
			response.Hits.Count().Should().BeGreaterThan(0);
			response.Hits.First().Should().NotBeNull();
			response.Hits.First().Source.Should().NotBeNull();
			response.Aggregations.Count.Should().BeGreaterThan(0);
			response.Took.Should().BeGreaterThan(0);
			var startDates = response.Aggregations.Terms("startDates");
			startDates.Should().NotBeNull();

			foreach (var document in response.Documents) document.ShouldAdhereToSourceSerializerWhenSet();
		}

		protected override Func<SearchDescriptor<Project>, ISearchRequest> Fluent => s => s
			.From(10)
			.Size(20)
			.Query(q => q
				.MatchAll()
			)
			.Aggregations(a => a
				.Terms("startDates", t => t
					.Field(p => p.StartedOn)
				)
			)
			.PostFilter(f => f
				.Term(p => p.State, StateOfBeing.Stable)
			);

		protected override SearchRequest<Project> Initializer => new SearchRequest<Project>()
		{
			From = 10,
			Size = 20,
			Query = new QueryContainer(new MatchAllQuery()),
			Aggregations = new TermsAggregation("startDates")
			{
				Field = "startedOn"
			},
			PostFilter = new QueryContainer(new TermQuery
			{
				Field = "state",
				Value = "Stable"
			})
		};
	}

	public class SearchApiFieldsTests : SearchApiTests
	{
		public SearchApiFieldsTests(ReadOnlyCluster cluster, EndpointUsage usage) : base(cluster, usage) { }

		protected override object ExpectJson => new
		{
			from = 10,
			size = 20,
			query = new
			{
				match_all = new { }
			},
			aggs = new
			{
				startDates = new
				{
					terms = new
					{
						field = "startedOn"
					}
				}
			},
			post_filter = new
			{
				term = new
				{
					state = new
					{
						value = "Stable"
					}
				}
			},
			stored_fields = new[] { "name", "numberOfCommits" }
		};

		protected override Func<SearchDescriptor<Project>, ISearchRequest> Fluent => s => s
			.From(10)
			.Size(20)
			.Query(q => q
				.MatchAll()
			)
			.Aggregations(a => a
				.Terms("startDates", t => t
					.Field(p => p.StartedOn)
				)
			)
			.PostFilter(f => f
				.Term(p => p.State, StateOfBeing.Stable)
			)
			.StoredFields(fs => fs
				.Field(p => p.Name)
				.Field(p => p.NumberOfCommits)
			);

		protected override SearchRequest<Project> Initializer => new SearchRequest<Project>()
		{
			From = 10,
			Size = 20,
			Query = new QueryContainer(new MatchAllQuery()),
			Aggregations = new TermsAggregation("startDates")
			{
				Field = "startedOn"
			},
			PostFilter = new QueryContainer(new TermQuery
			{
				Field = "state",
				Value = "Stable"
			}),
			StoredFields = Infer.Fields<Project>(p => p.Name, p => p.NumberOfCommits)
		};

		protected override void ExpectResponse(ISearchResponse<Project> response)
		{
			response.Hits.Count().Should().BeGreaterThan(0);
			response.Hits.First().Should().NotBeNull();
			response.Hits.First().Fields.ValueOf<Project, string>(p => p.Name).Should().NotBeNullOrEmpty();
			response.Hits.First().Fields.ValueOf<Project, int?>(p => p.NumberOfCommits).Should().BeGreaterThan(0);
			response.Aggregations.Count.Should().BeGreaterThan(0);
			var startDates = response.Aggregations.Terms("startDates");
			startDates.Should().NotBeNull();
		}
	}

	public class SearchApiContainingConditionlessQueryContainerTests : SearchApiTests
	{
		public SearchApiContainingConditionlessQueryContainerTests(ReadOnlyCluster cluster, EndpointUsage usage) : base(cluster, usage) { }

		protected override object ExpectJson => new
		{
			query = new
			{
				@bool = new
				{
					must = new object[] { new { query_string = new { query = "query" } } },
					should = new object[] { new { query_string = new { query = "query" } } },
					must_not = new object[] { new { query_string = new { query = "query" } } }
				}
			}
		};

		protected override Func<SearchDescriptor<Project>, ISearchRequest> Fluent => s => s
			.Query(q => q
				.Bool(b => b
					.Must(
						m => m.QueryString(qs => qs.Query("query")),
						m => m.QueryString(qs => qs.Query(string.Empty)),
						m => m.QueryString(qs => qs.Query(null)),
						m => new QueryContainer(),
						null
					)
					.Should(
						m => m.QueryString(qs => qs.Query("query")),
						m => m.QueryString(qs => qs.Query(string.Empty)),
						m => m.QueryString(qs => qs.Query(null)),
						m => new QueryContainer(),
						null
					)
					.MustNot(
						m => m.QueryString(qs => qs.Query("query")),
						m => m.QueryString(qs => qs.Query(string.Empty)),
						m => m.QueryString(qs => qs.Query(null)),
						m => new QueryContainer(),
						null
					)
				)
			);

		protected override SearchRequest<Project> Initializer => new SearchRequest<Project>()
		{
			Query = new BoolQuery
			{
				Must = new List<QueryContainer>
				{
					new QueryStringQuery { Query = "query" },
					new QueryStringQuery { Query = string.Empty },
					new QueryStringQuery { Query = null },
					new QueryContainer(),
					null
				},
				Should = new List<QueryContainer>
				{
					new QueryStringQuery { Query = "query" },
					new QueryStringQuery { Query = string.Empty },
					new QueryStringQuery { Query = null },
					new QueryContainer(),
					null
				},
				MustNot = new List<QueryContainer>
				{
					new QueryStringQuery { Query = "query" },
					new QueryStringQuery { Query = string.Empty },
					new QueryStringQuery { Query = null },
					new QueryContainer(),
					null
				}
			}
		};

		protected override void ExpectResponse(ISearchResponse<Project> response) => response.ShouldBeValid();
	}

	public class SearchApiNullQueryContainerTests : SearchApiTests
	{
		public SearchApiNullQueryContainerTests(ReadOnlyCluster cluster, EndpointUsage usage) : base(cluster, usage) { }

		protected override object ExpectJson => new { };

		protected override Func<SearchDescriptor<Project>, ISearchRequest> Fluent => s => s
			.Query(q => q
				.Bool(b => b
					.Must((Func<QueryContainerDescriptor<Project>, QueryContainer>)null)
					.Should((Func<QueryContainerDescriptor<Project>, QueryContainer>)null)
					.MustNot((Func<QueryContainerDescriptor<Project>, QueryContainer>)null)
				)
			);

		protected override SearchRequest<Project> Initializer => new SearchRequest<Project>()
		{
			Query = new BoolQuery
			{
				Must = null,
				Should = null,
				MustNot = null
			}
		};

		protected override void ExpectResponse(ISearchResponse<Project> response) => response.ShouldBeValid();
	}

	public class SearchApiNullQueriesInQueryContainerTests : SearchApiTests
	{
		public SearchApiNullQueriesInQueryContainerTests(ReadOnlyCluster cluster, EndpointUsage usage) : base(cluster, usage) { }

		// when we serialize we write and empty bool, when we read the fact it was verbatim is lost so while
		// we technically DO support deserialization here (and empty bool will get set) when we write it a second
		// time it will NOT write that bool because the is verbatim did not carry over.
		protected override bool SupportsDeserialization => false;

		protected override object ExpectJson => new
		{
			query = new
			{
				@bool = new { }
			}
		};

		// There is no *direct equivalent* to a query container collection only with a null querycontainer
		// since the fluent methods filter them out
		protected override Func<SearchDescriptor<Project>, ISearchRequest> Fluent => s => s
			.Query(q => q
				.Bool(b =>
				{
					b.Verbatim();
					IBoolQuery bq = b;
					bq.Must = new QueryContainer[] { null };
					bq.Should = new QueryContainer[] { null };
					bq.MustNot = new QueryContainer[] { null };
					return bq;
				})
			);

		protected override SearchRequest<Project> Initializer => new SearchRequest<Project>()
		{
			Query = new BoolQuery
			{
				IsVerbatim = true,
				Must = new QueryContainer[] { null },
				Should = new QueryContainer[] { null },
				MustNot = new QueryContainer[] { null }
			}
		};

		protected override void ExpectResponse(ISearchResponse<Project> response) => response.ShouldBeValid();
	}


	[SkipVersion("<6.2.0", "OpaqueId introduced in 6.2.0")]
	public class OpaqueIdApiTests
		: ApiIntegrationTestBase<ReadOnlyCluster, IListTasksResponse, IListTasksRequest, ListTasksDescriptor, ListTasksRequest>
	{
		public OpaqueIdApiTests(ReadOnlyCluster cluster, EndpointUsage usage) : base(cluster, usage) { }

		protected override object ExpectJson => null;
		protected override bool SupportsDeserialization => false;
		protected override int ExpectStatusCode => 200;
		protected override bool ExpectIsValid => true;
		protected override HttpMethod HttpMethod => HttpMethod.GET;
		protected override string UrlPath => $"/_tasks?pretty=true&error_trace=true";

		protected override Func<ListTasksDescriptor, IListTasksRequest> Fluent => s => s
			.RequestConfiguration(r => r.OpaqueId(CallIsolatedValue));

		protected override ListTasksRequest Initializer => new ListTasksRequest()
		{
			RequestConfiguration = new RequestConfiguration { OpaqueId = CallIsolatedValue },
		};

		protected override LazyResponses ClientUsage() => Calls(
			fluent: (c, f) => c.ListTasks(f),
			fluentAsync: (c, f) => c.ListTasksAsync(f),
			request: (c, r) => c.ListTasks(r),
			requestAsync: (c, r) => c.ListTasksAsync(r)
		);

		protected override void OnBeforeCall(IElasticClient client)
		{
			var searchResponse = client.Search<Project>(s => s
					.RequestConfiguration(r => r.OpaqueId(CallIsolatedValue))
					.Scroll("10m") // Create a scroll in order to keep the task around.
			);

			searchResponse.ShouldBeValid();
		}

		protected override void ExpectResponse(IListTasksResponse response)
		{
			response.ShouldBeValid();
			foreach (var node in response.Nodes)
			foreach (var task in node.Value.Tasks)
			{
				task.Value.Headers.Should().NotBeNull();
				if (task.Value.Headers.TryGetValue(RequestData.OpaqueIdHeader, out var opaqueIdValue))
					opaqueIdValue.Should()
						.Be(CallIsolatedValue,
							$"OpaqueId header {opaqueIdValue} did not match {CallIsolatedValue}");
				// TODO: Determine if this is a valid assertion i.e. should all tasks returned have an OpaqueId header?
//				else
//				{
//					Assert.True(false,
//						$"No OpaqueId header for task {task.Key} and OpaqueId value {this.CallIsolatedValue}");
//				}
			}
		}
	}
}
