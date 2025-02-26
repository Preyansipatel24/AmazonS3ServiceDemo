using System.Web;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;

namespace AmazonS3Demo
{

    public class S3FileService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;
        private readonly TransferUtility _transferUtility;

        public S3FileService(IConfiguration configuration)
        {
            var awsOptions = configuration.GetSection("AWS");
            _bucketName = awsOptions["BucketName"];
            string regionName = awsOptions["Region"];

            if (string.IsNullOrEmpty(regionName) || string.IsNullOrEmpty(_bucketName))
            {
                throw new ArgumentException("AWS Region or Bucket Name is missing in configuration.");
            }

            var region = RegionEndpoint.GetBySystemName(regionName);

            _s3Client = new AmazonS3Client(
                awsOptions["AccessKey"],
                awsOptions["SecretKey"],
                new AmazonS3Config
                {
                    RegionEndpoint = region,
                    ForcePathStyle = false,  // ✅ Recommended for AWS S3
                    UseHttp = false          // ✅ Use HTTPS
                }
            );

            _transferUtility = new TransferUtility(_s3Client);
        }
        public async Task<string> UploadFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("Invalid file.");
            }

            using var stream = file.OpenReadStream();
            var fileKey = $"uploads/{DateTime.UtcNow:yyyy/MM/dd}/{file.FileName}"; // ✅ Correct S3 path

            var putRequest = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = fileKey,  // ✅ Correct S3 path
                InputStream = stream,  // ✅ Use stream instead of FilePath
                ContentType = file.ContentType // ✅ Preserve original file type
            };

            var response = await _s3Client.PutObjectAsync(putRequest);

            return $"File uploaded successfully! \n" +
                $"metadata:{response} \n" +
                $"filekey: {fileKey}\n" +
                   $"URL: https://{_bucketName}.s3.amazonaws.com/{fileKey} \n" +
                   $"ETag: {response.ETag} \n" +
                   $"Last Modified: {response.HttpStatusCode}";
        }
        public async Task<string> DownloadFileAsync(string fileKey)
        {
            string localDirectory = "D:\\S3FileService\\AmazonS3Demo\\AmazonS3Demo\\wwwRoot\\";
            try
            {
                 string fileKey1 = HttpUtility.UrlDecode(fileKey); // ✅ Decode first

                var request = new GetObjectRequest
                {
                    BucketName = _bucketName,
                    //EtagToMatch = fileKey
                    Key = fileKey1 // ✅ Use full file path
                };
                string fileName = Path.GetFileName(fileKey1);
                using var response = await _s3Client.GetObjectAsync(request);

                // Ensure directory exists
                if (!Directory.Exists(localDirectory))
                {
                    Directory.CreateDirectory(localDirectory);
                }

                // Save the file locally
                string localFilePath = Path.Combine(localDirectory, Path.GetFileName(fileName));
                await using var fileStream = File.Create(localFilePath);
                await response.ResponseStream.CopyToAsync(fileStream);

                return $"File downloaded successfully! \n" +
                       $"URL: https://{_bucketName}.s3.amazonaws.com/{fileKey} \n" +
                       $"ETag: {response.ETag} \n" +
                       $"Last Modified: {response.LastModified} \n" +
                       $"Local File Path: {localFilePath}";
            }

            catch (AmazonS3Exception ex)
            {
                return $"S3 Error: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
        public async Task<string> DeleteFileAsync(string fileKey)
        {
            string fileKey1 = HttpUtility.UrlDecode(fileKey); // ✅ Decode first

            //var request = new GetObjectRequest
            //{
            //    BucketName = _bucketName,
            //    //EtagToMatch = fileKey
            //    Key = fileKey1 // ✅ Use full file path
            //};
            var response = await _s3Client.DeleteObjectAsync(_bucketName , fileKey1);
            return $"File downloaded successfully! \n" +
                      $"URL: https://{_bucketName}.s3.amazonaws.com/{fileKey} \n" +
                      $"response: {response} \n";
        }
        public async Task<List<string>> ListFilesAsync()
        {
            var response = await _s3Client.ListObjectsV2Async(new Amazon.S3.Model.ListObjectsV2Request
            {
                BucketName = _bucketName
            });

            return response.S3Objects.Select(o => o.Key).ToList();
        }

        public async Task<string> UpdateFileAsync(IFormFile file, string existingFileKey)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("Invalid file.");
            }

            using var stream = file.OpenReadStream();
            string fileKey1 = HttpUtility.UrlDecode(existingFileKey);
            try
            {
                // Check if the file exists in S3 before updating
                var metadataRequest = new GetObjectMetadataRequest
                {
                    BucketName = _bucketName,
                    Key = fileKey1
                };

                try
                {
                    var metadataResponse = await _s3Client.GetObjectMetadataAsync(metadataRequest);
                }
                catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return $"Error: The file '{existingFileKey}' does not exist.";
                }

                // Upload (overwrite) the existing file
                var putRequest = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = existingFileKey,
                    InputStream = stream,
                    ContentType = file.ContentType
                };

                var response = await _s3Client.PutObjectAsync(putRequest);

                return $"File updated successfully!\n" +
                       $"File Key: {existingFileKey}\n" +
                       $"URL: https://{_bucketName}.s3.amazonaws.com/{existingFileKey}\n" +
                       $"ETag: {response.ETag}\n" +
                       $"Status Code: {response.HttpStatusCode}";
            }
            catch (AmazonS3Exception ex)
            {
                return $"AWS S3 Error: {ex.Message} (Code: {ex.ErrorCode})";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

    }

}
