using BlazorMonaco.Editor;
using ColorCode;
using Microsoft.AspNetCore.Components;
using Linq2GraphQL.Client;
using System.Text.Json;
using TabBlazor;

namespace Linq2GraphQL.Docs.Components.Samples
{
    public partial class SamplesViewer<T, TResult>
    {
        private GraphQLRequest request;
        private string requestJson;

        [Parameter] public GraphQueryExecute<T, TResult> QueryExecute { get; set; }
        [Parameter] public string Query { get; set; }
        [Parameter] public string Title { get; set; }
        [Parameter] public RenderFragment Description { get; set; }

        private string queryHtml;
        private JsonSerializerOptions jsonOptions = new();
        private bool isExecuting;
        private bool isExpanded;
        private string id = Guid.NewGuid().ToString("N");

        private Tabs tabs;

        protected override async Task OnInitializedAsync()
        {
            jsonOptions.WriteIndented = true;

            queryHtml = FormatHtml(Query);

            request = await QueryExecute.GetRequestAsync();
            requestJson = JsonSerializer.Serialize(request, jsonOptions);



            await base.OnInitializedAsync();
        }


        private string FormatHtml(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                source = "No Code....";
            }

            var formatter = new HtmlClassFormatter();
            var htmlCode = formatter.GetHtmlString(source, Languages.CSharp);
            htmlCode = Wrap(htmlCode, "ExecuteAsync()", "queryExecute");
            htmlCode = Wrap(htmlCode, "Query", "queryExecute");
            htmlCode = Wrap(htmlCode, "Select", "select");
            htmlCode = Wrap(htmlCode, "Include", "select");

            return htmlCode;
        }

        private string Wrap(string source, string keyword, string cssClass)
        {
            return source.Replace(keyword, $@"<span class=""{cssClass}"">{keyword}</span>");
        }



        private async Task ShowDetailsAsync()
        {
            isExpanded = !isExpanded;
        }


        private async Task ExecuteAsync()
        {
            try
            {
                isExecuting = true;
                await QueryExecute.ExecuteAsync();




            }
            catch (Exception ex)
            {

                throw;
            }
            finally
            {
                isExecuting = false;
            }
        }

        private StandaloneEditorConstructionOptions RequestConstructionOptions(StandaloneCodeEditor editor)
        {
            return new StandaloneEditorConstructionOptions
            {
                AutomaticLayout = true,
                Language = "json",
                Value = requestJson,
                WordWrap = "on"
            };
        }

        private StandaloneEditorConstructionOptions QueryConstructionOptions(StandaloneCodeEditor editor)
        {
            return new StandaloneEditorConstructionOptions
            {
                AutomaticLayout = true,
                Language = "graphql",
                Value = request.Query,
                WordWrap = "on"
            };
        }

        private StandaloneEditorConstructionOptions ResponseConstructionOptions(StandaloneCodeEditor editor)
        {
            return new StandaloneEditorConstructionOptions
            {
                AutomaticLayout = true,
                Language = "json",
                Value = JsonSerializer.Serialize(QueryExecute.BaseResult, jsonOptions)
            };
        }

        private StandaloneEditorConstructionOptions ResultConstructionOptions(StandaloneCodeEditor editor)
        {
            return new StandaloneEditorConstructionOptions
            {
                AutomaticLayout = true,
                Language = "json",
                Value = JsonSerializer.Serialize(QueryExecute.ConvertResult(QueryExecute.BaseResult), jsonOptions)
            };
        }

    }
}