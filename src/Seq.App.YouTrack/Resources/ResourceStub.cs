// Copyright 2014-2019 CaptiveAire Systems
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
