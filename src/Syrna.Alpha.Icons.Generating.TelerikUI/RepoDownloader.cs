using System.IO.Compression;
using System.Text.RegularExpressions;

namespace Syrna.Alpha.Icons.Generating.TelerikUI;

/// <summary>
/// Provides functionality to download and extract a zipped repository.
/// </summary>
public class RepoDownloader
{
    private static readonly HttpClient client = new();
    private static readonly char[] separator = new[] { '/' };

    /// <summary>
    /// Initializes a new instance of the <see cref="RepoDownloader"/> class.
    /// </summary>
    /// <param name="address">The URL of the .zip file containing the repository contents.</param>
    public RepoDownloader(Uri address)
    {
        Address = address;
        var parts = Address.AbsolutePath.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        AuthorName = parts[0];
        RepoName = parts[1];
        BranchName = Path.GetFileNameWithoutExtension(parts[parts.Length - 1]);
    }

    /// <summary>
    /// Gets the URL of the zip file that contains the repository contents.
    /// </summary>
    public Uri Address { get; }

    /// <summary>
    /// Gets or sets the target folder where the repository will be extracted.
    /// </summary>
    public string? RootFolder { get; set; }

    /// <summary>
    /// Gets the full path to the folder where the repository was extracted. This is a folder named 'files' inside the <see cref="RootFolder"/>.
    /// </summary>
    public string ExtractedFolder => Path.Combine(RootFolder, "files");

    /// <summary>
    /// Deletes the <see cref="ExtractedFolder"/>.
    /// </summary>
    public void CleanUp()
    {
        Directory.Delete(RootFolder, true);
    }

    /// <summary>
    /// Gets the name of the target repository author.
    /// </summary>
    public string AuthorName { get; }

    /// <summary>
    /// Gets the name of the target repository.
    /// </summary>
    public string RepoName { get; }

    /// <summary>
    /// Gets the name of the target repository branch.
    /// </summary>
    public string BranchName { get; }

    /// <summary>
    /// Downloads the repository contents
    /// </summary>
    /// <returns>A list of the file names that were extracted.</returns>
    public async Task<string> Download()
    {
        var fileName = Path.GetFileName(Address.AbsolutePath);

        var bytes = await client.GetByteArrayAsync(Address).ConfigureAwait(false);

        if (string.IsNullOrEmpty(RootFolder))
        {
            RootFolder = Path.Combine(Path.GetTempPath(), $"{RepoName}-{Guid.NewGuid():N}");
            Directory.CreateDirectory(RootFolder);
        }

        var zipFileName = Path.Combine(RootFolder, fileName);
        File.WriteAllBytes(zipFileName, bytes);
        return zipFileName;
    }

    public async Task CopyZip(string sourceFileName)
    {
        await Task.Run(() =>
        {
            if (string.IsNullOrEmpty(RootFolder))
            {
                RootFolder = Path.Combine(Path.GetTempPath(), $"{RepoName}-{Guid.NewGuid():N}");
                Directory.CreateDirectory(RootFolder);
            }

            //var fileName = Path.GetFileName(sourceFileName);
            var fileName = Path.GetFileName(Address.AbsolutePath);
            File.Copy(sourceFileName, Path.Combine(RootFolder, fileName));
        });
    }

    /// <summary>
    /// Extracts the repository contents
    /// </summary>
    /// <param name="zipFileName"></param>
    /// <param name="entriesFilter">An optional regular expression filter. Only files matching the pattern in the .zip archive will be extracted.</param>
    /// <returns>A list of the file names that were extracted.</returns>
    public async Task<IReadOnlyList<string>> Extract(string zipFileName, string entriesFilter = @"\.svg")
    {
        return await Task.Run(() =>
        {
            using var archive = ZipFile.OpenRead(zipFileName);
            var entries = archive.Entries.Where(x =>
                !string.IsNullOrEmpty(x.Name)
                && Regex.IsMatch(x.FullName.Substring(RepoName.Length + 1 + BranchName.Length + 1), entriesFilter));

            if (!Directory.Exists(ExtractedFolder))
                Directory.CreateDirectory(ExtractedFolder);

            var extractedFiles = new List<string>();

            foreach (var entry in entries)
            {
                var extractedName = Path.Combine(ExtractedFolder, entry.FullName.Replace('/', Path.DirectorySeparatorChar));
                if (extractedName.Contains(".."))
                {
                    throw new InvalidOperationException($"Invalid file name '{extractedName}'");
                }

                var dir = Path.GetDirectoryName(extractedName);

                Directory.CreateDirectory(dir);

                extractedFiles.Add(extractedName);

                entry.ExtractToFile(extractedName);
            }

            return extractedFiles;
        });
    }
}

