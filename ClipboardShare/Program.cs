using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClipboardShare
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            bool createdNew;
            string appName = "ClipboardShare";

            using (Mutex mutex = new Mutex(true, appName, out createdNew))
            {
                // 이미 실행 중인 인스턴스가 있는지 확인합니다.
                if (!createdNew)
                {
                    // 이미 실행 중인 인스턴스가 있다면 메시지를 표시하거나 종료합니다.
                    MessageBox.Show($"{appName} 앱이 이미 실행 중입니다.");
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new TrayClipboardImageShareApp());
            }
        }
    }
}
