using ClipboardShare.GoogleDrive;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private bool isClipboardOperation;

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
        }

        private void NotifyMsg(string title, string message, ToolTipIcon icon, int time)
        {
            notifyIcon.BalloonTipTitle = title;
            notifyIcon.BalloonTipText = message;
            notifyIcon.BalloonTipIcon = icon;

            notifyIcon.ShowBalloonTip(time);
        }

        private async void ClipboardImagetoServer_Click(object sender, EventArgs e)
        {
            try
            {
                await RunService();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "FAIL");
            }
        }

        private async Task RunService()
        {
            try
            {
                if (isClipboardOperation)
                {
                    throw new Exception("클립보드 전송이 이미 실행중입니다.");
                }

                isClipboardOperation = true;

                String url = await GoogleDriveService.Instance.UploadImageFromClipboardToGoogleDrive();
                Clipboard.SetText(url);
                MessageBox.Show("Upload And URL Copy Success", "SUCCESS");
            }
            catch (AggregateException asyncEx)
            {
                MessageBox.Show(asyncEx.Message, "FAIL");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "FAIL");
            }
            finally
            {
                isClipboardOperation = false;
            }
        }

        private void ExitMenuItem_Click(object sender, EventArgs e)
        {
            // 애플리케이션 종료
            ExitThread();
        }
    }
}
