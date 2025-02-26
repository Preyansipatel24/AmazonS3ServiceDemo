using Amazon;
using Amazon.Runtime.Internal.Endpoints.StandardLibrary;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmazonS3Demo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AmazonS3Controller : ControllerBase
    {
        private readonly S3FileService _s3FileService;

        public AmazonS3Controller(S3FileService s3FileService)
        {
            _s3FileService = s3FileService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is empty");

            var url = await _s3FileService.UploadFileAsync(file);
            return Ok(new { Message = "File uploaded successfully", FileUrl = url });
        }

        [HttpGet("download/{filekey}")]
        public async Task<IActionResult> DownloadFile(string filekey)
        {
            var stream = await _s3FileService.DownloadFileAsync(filekey);
            //return File(stream, "application/octet-stream", fileName);
            return Ok(stream);
        }

        [HttpDelete("delete/{fileName}")]
        public async Task<IActionResult> DeleteFile(string fileName)
        {
            var result = await _s3FileService.DeleteFileAsync(fileName);
            return Ok(new { Message = "File deleted successfully", result});
        }

        [HttpGet("list")]
        public async Task<IActionResult> ListFiles()
        {
            var files = await _s3FileService.ListFilesAsync();
            return Ok(files);
        }

        [HttpPost("UpdateFile")]
        public async Task<IActionResult> UpdateFileAsync(IFormFile file, string existingFileKey)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is empty");

            var url = await _s3FileService.UpdateFileAsync(file,existingFileKey);
            return Ok(new { Message = "File Updated successfully", FileUrl = url });
        }
    }
}
