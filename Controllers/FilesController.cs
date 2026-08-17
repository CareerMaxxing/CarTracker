using CarCareTracker.Models;
using Microsoft.AspNetCore.Mvc;
using CarCareTracker.Helper;
using Microsoft.AspNetCore.Authorization;

namespace CarCareTracker.Controllers
{
    [Authorize]
    public class FilesController : Controller
    {
        private readonly ILogger<FilesController> _logger;
        private readonly IWebHostEnvironment _webEnv;
        private readonly IFileHelper _fileHelper;

        public FilesController(ILogger<FilesController> logger, IFileHelper fileHelper, IWebHostEnvironment webEnv)
        {
            _logger = logger;
            _webEnv = webEnv;
            _fileHelper = fileHelper;
        }

        [HttpPost]
        public IActionResult HandleFileUpload(IFormFile file)
        {
            var fileName = UploadFile(file);
            return Json(fileName);
        }
        [Authorize(Roles = nameof(UserData.IsRootUser))]
        [HttpPost]
        public IActionResult HandleTranslationFileUpload(IFormFile file)
        {
            var originalFileName = Path.GetFileNameWithoutExtension(file.FileName);
            if (originalFileName == "en_US")
            {
                return Json(OperationResponse.Failed("The translation file name en_US is reserved."));
            }
            var fileName = UploadFile(file);
            //move file from temp to translation folder.
            var uploadedFilePath = _fileHelper.MoveFileFromTemp(fileName, "translations/");
            //rename uploaded file so that it preserves original name.
            if (!string.IsNullOrWhiteSpace(uploadedFilePath))
            {
                var result = _fileHelper.RenameFile(uploadedFilePath, originalFileName);
                return Json(OperationResponse.Conditional(result));
            }
            return Json(OperationResponse.Failed());
        }
        [Authorize(Roles = nameof(UserData.IsRootUser))]
        [HttpPost]
        public IActionResult HandleThemeFileUpload(IFormFile file)
        {
            //check if extension is css
            if (Path.GetExtension(file.FileName) != ".css")
            {
                return Json(OperationResponse.Failed());
            }
            var originalFileName = Path.GetFileNameWithoutExtension(file.FileName);
            var fileName = UploadFile(file);
            //move file from temp to themes folder.
            var uploadedFilePath = _fileHelper.MoveFileFromTemp(fileName, "themes/");
            //rename uploaded file so that it preserves original name.
            if (!string.IsNullOrWhiteSpace(uploadedFilePath))
            {
                var result = _fileHelper.RenameFile(uploadedFilePath, originalFileName);
                return Json(OperationResponse.Conditional(result));
            }
            return Json(OperationResponse.Failed());
        }

        [HttpPost]
        public IActionResult HandleMultipleFileUpload(List<IFormFile> file)
        {
            List<UploadedFiles> uploadedFiles = new List<UploadedFiles>();
            foreach (IFormFile fileToUpload in file)
            {
                var fileName = UploadFile(fileToUpload);
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    continue; //blocked extension - see UploadFile.
                }
                uploadedFiles.Add(new UploadedFiles { Name = fileToUpload.FileName, Location = fileName, IsPending = true});
            }
            return Json(uploadedFiles);
        }
        [Authorize(Roles = nameof(UserData.IsRootUser))]
        [HttpPost]
        public IActionResult DeleteFiles(string fileLocation)
        {
            var result = _fileHelper.DeleteFile(fileLocation);
            return Json(result);
        }
        [Authorize(Roles = nameof(UserData.IsRootUser))]
        [HttpGet]
        public IActionResult MakeBackup()
        {
            var result = _fileHelper.MakeBackup();
            return Json(result);
        }
        [Authorize(Roles = nameof(UserData.IsRootUser))]
        [HttpPost]
        public IActionResult RestoreBackup(string fileName)
        {
            var result = _fileHelper.RestoreBackup(fileName);
            return Json(result);
        }
        //documents/images are served back same-origin with a content-type inferred purely from
        //extension (see Program.cs's /documents and /images static file roots) - anything that a
        //browser would execute or render as active content must never be accepted here, or an
        //uploaded "attachment" becomes stored XSS against whoever opens it.
        private static readonly HashSet<string> BlockedUploadExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".html", ".htm", ".xhtml", ".svg", ".js", ".mjs", ".exe", ".dll", ".bat", ".cmd", ".com",
            ".msi", ".ps1", ".psm1", ".sh", ".jar", ".php", ".php3", ".php4", ".php5", ".phtml",
            ".asp", ".aspx", ".jsp", ".hta", ".vbs", ".wsf", ".scr", ".cpl", ".jse", ".vbe"
        };
        private string UploadFile(IFormFile fileToUpload)
        {
            var extension = Path.GetExtension(fileToUpload.FileName);
            if (BlockedUploadExtensions.Contains(extension))
            {
                return string.Empty;
            }
            string uploadDirectory = "temp/";
            string uploadPath = Path.Combine(_webEnv.ContentRootPath, "data", uploadDirectory);
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);
            string fileName = Guid.NewGuid() + extension;
            string filePath = Path.Combine(uploadPath, fileName);
            using (var stream = System.IO.File.Create(filePath))
            {
                fileToUpload.CopyTo(stream);
            }
            return Path.Combine("/", uploadDirectory, fileName);
        }
        public IActionResult UploadCoordinates(List<string> coordinates)
        {
            string uploadDirectory = "temp/";
            string uploadPath = Path.Combine(_webEnv.ContentRootPath, "data", uploadDirectory);
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);
            string fileName = Guid.NewGuid() + ".csv";
            string filePath = Path.Combine(uploadPath, fileName);
            string fileData = string.Join("\r\n", coordinates);
            System.IO.File.WriteAllText(filePath, fileData);
            var uploadedFile = new UploadedFiles { Name = "coordinates.csv", Location = Path.Combine("/", uploadDirectory, fileName), IsPending = true };
            return Json(uploadedFile);
        }
        public IActionResult PreviewFile(string fileName, string fileLocation)
        {
            var viewModel = new UploadedFiles { Name = fileName, Location = fileLocation };
            return PartialView("_AttachmentPreview", viewModel);
        }
    }
}
