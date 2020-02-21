﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Statiq.App;
using Statiq.Common;
using Statiq.Core;
using Statiq.Html;
using Statiq.Markdown;
using Statiq.Razor;
using Statiq.Yaml;

namespace Statiq.Web.Pipelines
{
    public class Content : Pipeline
    {
        public Content()
        {
            InputModules = new ModuleList
            {
                new ReadFiles("**/{!_,}*.{html,cshtml,md}")
            };

            ProcessModules = new ModuleList
            {
                new ProcessIncludes(),
                new ExtractFrontMatter(new ParseYaml()),
                new FilterDocuments(Config.FromDocument(doc => !doc.ContainsKey(WebKeys.ArchiveSources) && !doc.ContainsKey(WebKeys.ArchiveFilter) && !doc.ContainsKey(WebKeys.ArchiveKey))),
                new EnumerateValues(),
                new ExecuteIf(Config.FromDocument(doc => doc.MediaTypeEquals("text/markdown")))
                {
                    new RenderMarkdown().UseExtensions()
                },
                new AddTitle(),
                new SetDestination(".html"),
                new RenderRazor()
                    .WithLayout(Config.FromDocument((doc, ctx) =>
                    {
                        // Crawl up the tree looking for a layout
                        IDocument parent = doc;
                        while (parent != null)
                        {
                            if (parent.ContainsKey(WebKeys.Layout))
                            {
                                return parent.GetPath(WebKeys.Layout);
                            }
                            parent = parent.GetParent(ctx.Inputs);
                        }
                        return null;  // If no layout metadata, revert to default behavior
                    })),
                new ExecuteIf(Config.FromSetting<bool>(WebKeys.MirrorResources))
                {
                    new MirrorResources()
                }
            };

            OutputModules = new ModuleList
            {
                new WriteFiles()
            };
        }
    }
}