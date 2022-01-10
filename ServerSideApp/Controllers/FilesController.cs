using Microsoft.AspNetCore.Http;

namespace ServerSideApp.Controllers;

/// <summary>
/// Files controller
/// </summary>
[ApiController]
[Route("api/[Controller]")]
public class FilesController : ControllerBase
{
    /// <summary>
    /// The logger
    /// </summary>
    private readonly ILogger<FilesController> _logger;
    /// <summary>
    /// The HTTP context accessor
    /// </summary>
    private readonly IHttpContextAccessor _httpContextAccessor;
    /// <summary>
    /// The file size limit
    /// </summary>
    private readonly long _fileSizeLimit;
    /// <summary>
    /// The permitted extensions
    /// </summary>
    private readonly string[] _permittedExtensions = { ".gif", ".png", ".jpg" };
    /// <summary>
    /// The target folder path
    /// </summary>
    private readonly string _targetFolderPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="FilesController"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="config">The configuration.</param>
    public FilesController(ILogger<FilesController> logger, IConfiguration config, IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;

        _fileSizeLimit = config.GetValue<long>("FileSizeLimit");
        _targetFolderPath = config.GetValue<string>("StoredFilesPath");

        Directory.CreateDirectory(_targetFolderPath);
    }

    /// <summary>
    /// Uploads a file.
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    [DisableFormValueModelBinding]
    public async Task<IActionResult> Upload()
    {
        if (!Request.ContentType.IsMultipartContentType())
        {
            ModelState.AddModelError("File", "The request couldn't be processed (Error 1).");
            _logger.LogWarning($"The request content type [{Request.ContentType}] is invalid.");
            return BadRequest(ModelState);
        }

        var formModel = new CustomFormModel();

        var boundary = MediaTypeHeaderValue.Parse(Request.ContentType).GetBoundary(new FormOptions().MultipartBoundaryLengthLimit);
        var reader = new MultipartReader(boundary, HttpContext.Request.Body);
        var section = await reader.ReadNextSectionAsync();

        string trustedFileNameForFileStorage = String.Empty;

        while (section != null)
        {
            var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, 
                out var contentDisposition);

            if (hasContentDispositionHeader)
            {
                if (contentDisposition.IsFileDisposition())
                {
                    // Don't trust the file name sent by the client. To display the file name, HTML-encode the value.
                    var trustedFileNameForDisplay = WebUtility.HtmlEncode(contentDisposition.FileName.Value);
                    trustedFileNameForFileStorage = Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) +
                            Path.GetExtension(trustedFileNameForDisplay);

                    var streamedFileContent = await FileHelpers.ProcessStreamedFile(section, contentDisposition, 
                        ModelState, _permittedExtensions, _fileSizeLimit);

                    if (!ModelState.IsValid)
                        return BadRequest(ModelState);

                    var trustedFilePath = Path.Combine(_targetFolderPath, trustedFileNameForFileStorage);
                    using (var targetStream = System.IO.File.Create(trustedFilePath))
                    {
                        await targetStream.WriteAsync(streamedFileContent);
                        formModel.TrustedFilePath = trustedFilePath;
                        formModel.TrustedFileName = trustedFileNameForDisplay;
                        _logger.LogInformation($"Uploaded file '{trustedFileNameForDisplay}' saved to '{_targetFolderPath}' " + 
                                               $"as {trustedFileNameForFileStorage}");
                    }
                }
                else if (contentDisposition.IsFormDisposition())
                {
                    var content = new StreamReader(section.Body).ReadToEnd();
                    if (contentDisposition.Name == "userId" && int.TryParse(content, out var useId))
                    {
                        formModel.UserId = useId;
                    }

                    if (contentDisposition.Name == "comment")
                    {
                        formModel.Comment = content;
                    }

                    if (contentDisposition.Name == "isPrimary" && bool.TryParse(content, out var isPrimary))
                    {
                        formModel.IsPrimary = isPrimary;
                    }
                }
            }

            // Drain any remaining section body that hasn't been consumed and read the headers for the next section.
            section = await reader.ReadNextSectionAsync();
        }

        if (!string.IsNullOrEmpty(trustedFileNameForFileStorage))
        {
            string host = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host.Value}";
            int index = FileHelpers.GetExtensionId(Path.GetExtension(trustedFileNameForFileStorage));
            return Ok($"{host}/api/files/{index}/{Path.GetFileNameWithoutExtension(trustedFileNameForFileStorage)}");
        }
        else
            return BadRequest("It wasn't possible to upload the file");
    }

    /// <summary>
    /// Downloads the specified unique identifier.
    /// </summary>
    /// <param name="guid">The unique identifier.</param>
    /// <returns></returns>
    [HttpGet]
    [Route("{fileType}/{fileName}")]
    public async Task<IActionResult> Download(int fileType, string fileName)
    {
        string ext = FileHelpers.GetExtensionById(fileType);
        if (ext == null)
        {
            _logger.LogInformation($"Extension not Found.");
            return BadRequest($"Extension not Found.");
        }

        var trustedFilePath = Path.Combine(_targetFolderPath, fileName + ext);
        if(!System.IO.File.Exists(trustedFilePath))
        {
            _logger.LogInformation($"File {trustedFilePath} not exists");
            return NotFound($"File {trustedFilePath} not exists");
        }

        _logger.LogInformation($"Downloading file [{trustedFilePath}].");
        var bytes = await System.IO.File.ReadAllBytesAsync(trustedFilePath);
        return File(bytes, ext.GetMimeTypes(), trustedFilePath);
    }
}