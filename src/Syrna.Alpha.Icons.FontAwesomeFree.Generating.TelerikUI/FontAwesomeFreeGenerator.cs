using System.Text;
using CodeCasing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Threading;
using Syrna.Alpha.Icons.FontAwesomeFree.Generating.TelerikUI.Properties;
using Syrna.Alpha.Icons.Generating.TelerikUI;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Icons;
#pragma warning restore IDE0130 // Namespace does not match folder structure

[Generator(LanguageNames.CSharp)]
public class FontAwesomeFreeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var pipeline = context.AdditionalTextsProvider
            .Where(static text => text.Path.EndsWith(".ini"))
            .Select(static (text, cancellationToken) =>
            {
                var path = text.Path;
                var code = text.GetText(cancellationToken);
                return (path, code);
            });

        context.RegisterSourceOutput(pipeline,
            static (context, pair) =>
            {
                var ns = "Syrna.Alpha.Icons.FontAwesomeFree.TelerikUI";
                var dir = new DirectoryInfo(pair.path);
                var projectDir = dir.Parent;
                var solutionDir = projectDir.Parent;
                var solutionPath = solutionDir.FullName;
                var zipName = "FontAwesome-free-6.6.0-web.zip";
                var zipFileName = Path.Combine(solutionPath, zipName);
                using var taskContext = new JoinableTaskContext();
                var taskFactory = new JoinableTaskFactory(taskContext);
                var fakeAddress = "https://github.com/FortAwesome/FontAwesome-free/archive/refs/heads/6.6.0-web.zip";
                var downloader = new RepoDownloader(new Uri(fakeAddress/*Resources.DownloadAddress*/));
                taskFactory.Run(
                    async () =>
                    {
                        //https://github.com/FortAwesome/Font-Awesome/archive/refs/heads/6.x.zip
                        //Font-Awesome-6.x RepoName=Font-Awesome BranchName=6.x
                        //https://github.com/FortAwesome/FontAwesome-free/archive/refs/heads/6.6.0-web.zip
                        //FontAwesome-free-6.6.0-web RepoName=FontAwesome-free BranchName=6.6.0-web
                        //var zipFileName = await downloader.Download().ConfigureAwait(true);
                        await downloader.CopyZip(zipFileName);
                        await downloader.Extract(zipFileName, @"^svgs\/.*.svg$").ConfigureAwait(true);
                    });

                var packNames = pair.code.ToString().Split(',');
                foreach (var packName in packNames)
                {
                    var svgFolder = Path.Combine(downloader.ExtractedFolder, $"{downloader.RepoName}-{downloader.BranchName}", "svgs", packName);
                    var src = GeneratorExecutionContextExtensions.WriteIconClasses("FontAwesomeFree.Icons", packName.ToPascalCase(), ns, svgFolder);
                    context.AddSource(src.Hint, src.Source);

                    src = GeneratorExecutionContextExtensions.WriteIconsClass("FontAwesomeFree", packName.ToPascalCase(), ns, svgFolder);
                    context.AddSource(src.Hint, src.Source);
                }
            });
    }
}