using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClipboardShare
{
    public class TrayClipboardImageShareApp : ApplicationContext
    {
        private NotifyIcon notifyIcon;
        private string _ip = "127.0.0.1";

        public TrayClipboardImageShareApp()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            // NotifyIcon 생성
            notifyIcon = new NotifyIcon();
            // from https://icon-icons.com/ko/%EC%95%84%EC%9D%B4%EC%BD%98/%EC%9D%B4%EB%AF%B8%EC%A7%80/153794
            notifyIcon.Icon = Properties.Resources.image;
            notifyIcon.Text = "ClipboardImageShare";
            // 컨텍스트 메뉴 설정 (옵션)
            ContextMenu contextMenu = new ContextMenu();
            contextMenu.MenuItems.Add(new MenuItem("Clipboard Image to Server", ClipboardImagetoServer_Click));
            contextMenu.MenuItems.Add(new MenuItem("Exit", ExitMenuItem_Click));
            notifyIcon.ContextMenu = contextMenu;

            // 아이콘을 트레이에 표시
            notifyIcon.Visible = true;
            SetPublicIP();
        }

        private void NotifyMsg(string title, string message, ToolTipIcon icon, int time)
        {
            notifyIcon.BalloonTipTitle = title;
            notifyIcon.BalloonTipText = message;
            notifyIcon.BalloonTipIcon = icon;

            notifyIcon.ShowBalloonTip(time);
        }

        private void SetPublicIP()
        {
            try
            {
                // http://ipinfo.io/ip 일일사용량 어느정도는 무료
                _ip = new WebClient().DownloadString("http://ipinfo.io/ip").Trim();
            }
            catch(Exception ex)
            {
                MessageBox.Show("공인 ip를 얻어올 수 없습니다." + ex.Message);
            }
        }

        private void ClipboardImagetoServer_Click(object sender, EventArgs e)
        {
            // 클립보드에 이미지가 있는지 확인
            if (Clipboard.ContainsImage())
            {
                // 클립보드에서 이미지 가져오기
                Image imageFromClipboard = Clipboard.GetImage();

                // 이미지를 Base64로 인코딩
                string base64Image = ImageToBase64(imageFromClipboard);

                // Google Apps Script로 전송
                SendToGoogleAppsScript(base64Image);

                // NotifyMsg("전송 성공", "이미지를 성공적으로 Google Apps Script로 전송했습니다.", ToolTipIcon.Info, 5000);
            }
            else
            {
                NotifyMsg("클립보드 복사 실패", "클립보드에 이미지가 없습니다.", ToolTipIcon.Warning, 5000);
            }
        }

        private string ImageToBase64(Image image)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // 이미지를 스트림에 쓰기
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);

                // 스트림을 byte 배열로 변환
                byte[] imageBytes = ms.ToArray();

                // byte 배열을 Base64로 인코딩
                string base64String = Convert.ToBase64String(imageBytes);

                return base64String;
            }
        }

        private void SendToGoogleAppsScript(string base64Image)
        {
            // Google Apps Script 웹 앱 URL
            string scriptUrl = "https://script.google.com/macros/s/AKfycbyjITeBrk_4bsGRy9jiczwB-KPoYwOZZxnsrr0hYJpxqPLOWEMjDBo96VsMLHssBwlN/exec";

            // JSON 형태의 데이터 생성
            JObject json = new JObject();
            json.Add("metaData", "data:image/png;base64");
            json.Add("data", base64Image);
            json.Add("ip", _ip);

            String jsonData = json.ToString();

            // Google Apps Script로 POST 요청 보내기
            using (var client = new System.Net.WebClient())
            {
                client.Headers.Add("Content-Type", "application/json");
                string response = client.UploadString(scriptUrl, "POST", jsonData);

                // JSON 문자열을 JSON 객체로 변환
                JObject jsonObject = JObject.Parse(response);

                // 변환된 JSON 객체를 사용하여 필요한 작업 수행
                bool isSuccess = (bool)jsonObject["success"];
                string link = jsonObject["link"].ToString();

                if (isSuccess)
                {
                    // 텍스트를 클립보드에 복사
                    Clipboard.SetText(link);
                    NotifyMsg("복사 성공", "클립보드에 공유한 이미지 url을 복사하였습니다.", ToolTipIcon.Info, 5000);
                }
                else
                {
                    NotifyMsg("업로드 실패", "클립보드를 서버에 업로드하는 작업이 실패하였습니다.", ToolTipIcon.Warning, 5000);
                }
            }
        }

        private void ExitMenuItem_Click(object sender, EventArgs e)
        {
            // 애플리케이션 종료
            ExitThread();
        }
    }
}
