using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClipboardShare.GoogleDrive
{
    public class GoogleDriveService
    {
        private static readonly string[] Scopes = { DriveService.Scope.DriveFile };
        private static readonly string APPLICATION_NAME = "ClipboardImageShare";

        // Lazy<T>를 사용하여 싱글턴 인스턴스
        private static readonly Lazy<GoogleDriveService> _instance = new Lazy<GoogleDriveService>(() => new GoogleDriveService());
        private DriveService service;

        // private 생성자
        private GoogleDriveService()
        {
            // 초기화 코드
        }

        // 싱글턴 인스턴스에 대한 public static 프로퍼티
        public static GoogleDriveService Instance
        {
            get
            {
                return _instance.Value;
            }
        }

        private DriveService AuthenticateOauth()
        {
            UserCredential credential;

            if (this.service == null)
            {
                // 클라이언트 시크릿을 하드코딩합니다.
                var clientSecrets = new ClientSecrets
                {
                    ClientId = "GCP로 받은 클라이언트 ID",
                    ClientSecret = "GCP로 받은 클라이언트 시크릿키"
                };

                int oauthTimeoutMinutes = 3;
                CancellationTokenSource clt = new CancellationTokenSource(TimeSpan.FromMinutes(oauthTimeoutMinutes));

                // 사용자 인증 정보를 가져옵니다.
                string credPath = "token.json";
                try
                {
                    credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    clientSecrets,
                    Scopes,
                    "user",
                    clt.Token,
                    new FileDataStore(credPath, true)).Result;
                }
                catch (AggregateException e)
                {
                    if (e.InnerException is TaskCanceledException)
                    {
                        throw new TimeoutException($"OAuth 인증이 {oauthTimeoutMinutes}분 내에 완료되지 않았습니다.");
                    }

                    throw;
                }
                // Google Drive API 서비스 생성
                this.service = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = APPLICATION_NAME,
                });
            }

            return this.service;
        }

        private async Task<string> CreateFolderIfNotExistsAsync(string folderName)
        {
            // 폴더 이름으로 검색
            var listRequest = service.Files.List();
            listRequest.Q = $"mimeType='application/vnd.google-apps.folder' and name='{folderName}' and trashed=false";
            listRequest.Fields = "files(id, name)";
            var listResponse = await listRequest.ExecuteAsync();

            var folder = listResponse.Files.FirstOrDefault();
            if (folder != null)
            {
                // 폴더가 이미 존재하면 ID 반환
                return folder.Id;
            }

            // 폴더가 없으면 새로 생성
            var folderMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = folderName,
                MimeType = "application/vnd.google-apps.folder"
            };

            var createRequest = service.Files.Create(folderMetadata);
            createRequest.Fields = "id";
            var folderResponse = await createRequest.ExecuteAsync();

            return folderResponse.Id;
        }

        public async Task<string> UploadImageFromClipboardToGoogleDrive()
        {
            // 클립보드에서 이미지를 메모리 스트림으로 변환
            if (Clipboard.ContainsImage())
            {
                Image clipboardImage = Clipboard.GetImage();

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    // 이미지를 PNG 형식으로 메모리 스트림에 저장
                    clipboardImage.Save(memoryStream, ImageFormat.Png);

                    // Google Drive에 업로드할 준비
                    var driveService = AuthenticateOauth();

                    // 현재 시간을 기반으로 파일명 생성
                    string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmssfff");
                    string fileName = $"{timeStamp}_ClipboardImage.png";
                    string folderName = "ClipboardImageFolder";  // 수정된 폴더 이름

                    // 폴더 생성 또는 찾기
                    string folderId = await CreateFolderIfNotExistsAsync(folderName);

                    var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                    {
                        Name = fileName,
                        Parents = new List<string> { folderId }  // 파일을 폴더에 저장
                    };

                    // 스트림을 처음부터 읽기 위해 포지션을 0으로 설정
                    memoryStream.Position = 0;

                    FilesResource.CreateMediaUpload request = driveService.Files.Create(
                        fileMetadata, memoryStream, "image/png");
                    request.Fields = "id";

                    // 업로드 시작
                    IUploadProgress progress = await request.UploadAsync();
                    if (progress.Status != UploadStatus.Completed)
                    {
                        throw new Exception("Image upload failed.");
                    }

                    // 파일 ID 얻기
                    var file = request.ResponseBody;

                    // 파일을 누구나 읽을 수 있도록 공유 설정 변경
                    var permission = new Google.Apis.Drive.v3.Data.Permission()
                    {
                        Role = "reader",
                        Type = "anyone"
                    };
                    await driveService.Permissions.Create(permission, file.Id).ExecuteAsync();

                    // 파일의 공유 링크 가져오기
                    var fileRequest = driveService.Files.Get(file.Id);
                    fileRequest.Fields = "webViewLink";
                    var fileResponse = await fileRequest.ExecuteAsync();

                    return fileResponse.WebViewLink;  // 공유 링크 반환
                }
            }
            else
            {
                throw new Exception("No image found in clipboard.");
            }
        }
    }
}
