using Azure.Core;
using Azure.Identity;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AIWeeklyReport
{
    /// <summary>
    /// Uploads a file to a SharePoint document library via plain calls to the Microsoft Graph
    /// REST API (not the Microsoft.Graph SDK — its object model varies enough between package
    /// versions that a type-safe call here can silently stop compiling on an upgrade; raw
    /// HTTP + JSON against the documented REST endpoints doesn't have that problem).
    ///
    /// Authenticates app-only (client ID + secret, no user login) via Azure.Identity's
    /// ClientSecretCredential — suitable for an unattended/scheduled run.
    ///
    /// Azure AD app registration requirements:
    ///   - API permission: Sites.ReadWrite.All (Application) or Sites.Selected scoped to the
    ///     target site, admin-consented.
    ///   - A client secret (or certificate — swap ClientSecretCredential for
    ///     ClientCertificateCredential if your org requires certs over secrets).
    /// </summary>
    public class SharePointUploader
    {
        private const string GraphBaseUrl = "https://graph.microsoft.com/v1.0";
        private const long SmallFileLimitBytes = 4L * 1024 * 1024; // Graph's simple-upload ceiling

        private readonly ClientSecretCredential _credential;
        private readonly HttpClient _http;
        private readonly string _siteHostname;
        private readonly string _siteRelativePath;
        private readonly string? _libraryName;

        /// <param name="siteHostname">e.g. "reednet.sharepoint.com" (no scheme, no trailing path)</param>
        /// <param name="siteRelativePath">e.g. "/sites/AIReports" (must start with '/')</param>
        /// <param name="libraryName">
        /// The document library's display name, e.g. "Reports" (as shown in the site's library
        /// list / the URL segment right after the site path). Null or empty targets the site's
        /// default library ("Shared Documents").
        /// </param>
        public SharePointUploader(string tenantId, string clientId, string clientSecret,
            string siteHostname, string siteRelativePath, string? libraryName = null, HttpClient? httpClient = null)
        {
            _credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            _http = httpClient ?? new HttpClient();
            _siteHostname = siteHostname;
            _siteRelativePath = siteRelativePath.StartsWith('/') ? siteRelativePath : "/" + siteRelativePath;
            _libraryName = string.IsNullOrWhiteSpace(libraryName) ? null : libraryName;
        }

        /// <summary>
        /// Uploads localFilePath into the configured document library at remoteFolderPath
        /// (e.g. "2026/September" — missing folders in that path are created automatically, one
        /// level at a time), replacing any existing file of the same name. Returns the uploaded
        /// file's SharePoint web URL.
        /// </summary>
        public async Task<string> UploadFileAsync(string localFilePath, string remoteFolderPath,
            string? remoteFileName = null, CancellationToken ct = default)
        {
            if (!File.Exists(localFilePath))
                throw new FileNotFoundException("File to upload was not found.", localFilePath);

            var fileInfo = new FileInfo(localFilePath);
            if (fileInfo.Length > SmallFileLimitBytes)
                throw new NotSupportedException(
                    $"File is {fileInfo.Length:N0} bytes, over the 4 MB simple-upload limit this uploader " +
                    "supports (weekly HTML reports are always well under this). Chunked/resumable upload " +
                    "for larger files isn't implemented here.");

            remoteFileName ??= Path.GetFileName(localFilePath);

            var siteId = await GetSiteIdAsync(ct);
            var driveSegment = await GetDriveSegmentAsync(siteId, ct); // "/drive" or "/drives/{id}"

            if (!string.IsNullOrWhiteSpace(remoteFolderPath))
                await EnsureFolderPathExistsAsync(siteId, driveSegment, remoteFolderPath, ct);

            var itemPath = string.IsNullOrEmpty(remoteFolderPath)
                ? remoteFileName
                : $"{remoteFolderPath.Trim('/')}/{remoteFileName}";

            var uploadUrl = $"{GraphBaseUrl}/sites/{siteId}{driveSegment}/root:/{EncodePath(itemPath)}:/content";
            using var request = await AuthedRequestAsync(HttpMethod.Put, uploadUrl, ct);
            var bytes = await File.ReadAllBytesAsync(localFilePath, ct);
            request.Content = new ByteArrayContent(bytes);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");

            using var response = await _http.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Upload to '{itemPath}' failed ({(int)response.StatusCode}): {body}");

            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.TryGetProperty("webUrl", out var webUrlProp) ? webUrlProp.GetString() ?? "" : "";
        }

        private async Task<string> GetSiteIdAsync(CancellationToken ct)
        {
            var url = $"{GraphBaseUrl}/sites/{_siteHostname}:{_siteRelativePath}";
            using var request = await AuthedRequestAsync(HttpMethod.Get, url, ct);
            using var response = await _http.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    $"Could not resolve SharePoint site '{_siteHostname}{_siteRelativePath}' " +
                    $"({(int)response.StatusCode}): {body}");

            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.TryGetProperty("id", out var idProp) && idProp.GetString() is { } id
                ? id
                : throw new InvalidOperationException("Site lookup succeeded but the response had no 'id' field.");
        }

        /// <summary>
        /// Resolves which drive to use: the site's default drive ("/drive") if no library name
        /// was configured, otherwise looks up /sites/{siteId}/drives and matches by name to get
        /// that specific library's drive ID ("/drives/{id}"). A site's default library is always
        /// named "Documents" internally even though it displays as "Shared Documents", so both
        /// names are accepted when matching.
        /// </summary>
        private async Task<string> GetDriveSegmentAsync(string siteId, CancellationToken ct)
        {
            if (_libraryName == null)
                return "/drive";

            var url = $"{GraphBaseUrl}/sites/{siteId}/drives";
            using var request = await AuthedRequestAsync(HttpMethod.Get, url, ct);
            using var response = await _http.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Could not list document libraries for this site ({(int)response.StatusCode}): {body}");

            using var doc = JsonDocument.Parse(body);
            var drives = doc.RootElement.GetProperty("value").EnumerateArray().ToList();

            var match = drives.FirstOrDefault(d =>
                d.TryGetProperty("name", out var nameProp) &&
                string.Equals(nameProp.GetString(), _libraryName, StringComparison.OrdinalIgnoreCase));

            if (match.ValueKind == JsonValueKind.Undefined)
            {
                var available = string.Join(", ", drives
                    .Select(d => d.TryGetProperty("name", out var n) ? n.GetString() : null)
                    .Where(n => n != null));
                throw new InvalidOperationException(
                    $"No document library named '{_libraryName}' was found on this site. " +
                    $"Available libraries: {available}");
            }

            var driveId = match.GetProperty("id").GetString()
                ?? throw new InvalidOperationException($"Library '{_libraryName}' had no drive id.");
            return $"/drives/{driveId}";
        }

        private async Task<bool> PathExistsAsync(string siteId, string driveSegment, string path, CancellationToken ct)
        {
            var url = $"{GraphBaseUrl}/sites/{siteId}{driveSegment}/root:/{EncodePath(path)}";
            using var request = await AuthedRequestAsync(HttpMethod.Get, url, ct);
            using var response = await _http.SendAsync(request, ct);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Graph's path-based addressing does NOT auto-create missing intermediate folders — a
        /// request against a path with a missing segment just 404s. This walks the target path
        /// one segment at a time (e.g. "2026", then "September" under it), creating whichever
        /// segments don't already exist, within the given drive.
        /// </summary>
        private async Task EnsureFolderPathExistsAsync(string siteId, string driveSegment, string folderPath, CancellationToken ct)
        {
            var segments = folderPath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            var currentPath = "";

            foreach (var segment in segments)
            {
                var parentPath = currentPath;
                currentPath = string.IsNullOrEmpty(currentPath) ? segment : $"{currentPath}/{segment}";

                if (await PathExistsAsync(siteId, driveSegment, currentPath, ct))
                    continue;

                var createUrl = string.IsNullOrEmpty(parentPath)
                    ? $"{GraphBaseUrl}/sites/{siteId}{driveSegment}/root/children"
                    : $"{GraphBaseUrl}/sites/{siteId}{driveSegment}/root:/{EncodePath(parentPath)}:/children";

                var payload = new Dictionary<string, object>
                {
                    ["name"] = segment,
                    ["folder"] = new Dictionary<string, object>(),
                    // "fail" (not "replace"/"rename"): a 409 here just means another run created
                    // the same folder in the meantime, which is fine — see the check below.
                    ["@microsoft.graph.conflictBehavior"] = "fail"
                };

                using var request = await AuthedRequestAsync(HttpMethod.Post, createUrl, ct);
                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                using var response = await _http.SendAsync(request, ct);
                if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Conflict)
                    continue;

                var body = await response.Content.ReadAsStringAsync(ct);
                throw new InvalidOperationException($"Could not create folder '{currentPath}' ({(int)response.StatusCode}): {body}");
            }
        }

        private async Task<HttpRequestMessage> AuthedRequestAsync(HttpMethod method, string url, CancellationToken ct)
        {
            var request = new HttpRequestMessage(method, url);
            var tokenContext = new TokenRequestContext(new[] { "https://graph.microsoft.com/.default" });
            var accessToken = await _credential.GetTokenAsync(tokenContext, ct);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Token);
            return request;
        }

        /// <summary>Percent-encodes a path's segments individually while keeping the '/' separators literal.</summary>
        private static string EncodePath(string path) =>
            string.Join("/", path.Split('/').Select(Uri.EscapeDataString));
    }
}