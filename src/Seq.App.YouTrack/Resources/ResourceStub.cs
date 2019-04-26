// Seq.App.YouTrack - Copyright (c) 2019 CaptiveAire

using System;
using System.Reflection;

using EmbeddedResources;

namespace Seq.App.YouTrack.Resources
{
    public static class TemplateResources
    {
        static TemplateResources()
        {
            _resourceLocator = new AssemblyResourceLocator(Assembly.GetExecutingAssembly());
            _defaultIssueBodyTemplate = new Lazy<string>(() => GetEmbeddedResource($"{_templateNamespace}.DefaultIssueBodyTemplate.md"));
            _defaultIssueSummeryTemplate =
                new Lazy<string>(() => GetEmbeddedResource($"{_templateNamespace}.DefaultIssueSummaryTemplate.md"));
        }

        public static string DefaultIssueBodyTemplate => _defaultIssueBodyTemplate.Value;
        public static string DefaultIssueSummaryTemplate => _defaultIssueSummeryTemplate.Value;

        static readonly AssemblyResourceLocator _resourceLocator;
        static readonly Lazy<string> _defaultIssueBodyTemplate;
        static readonly string _templateNamespace = typeof(TemplateResources).Namespace;
        static readonly Lazy<string> _defaultIssueSummeryTemplate;

        static string GetEmbeddedResource(string name)
        {
            return new EmbeddedResourceLoader(_resourceLocator).LoadText(name);
        }
    }
}