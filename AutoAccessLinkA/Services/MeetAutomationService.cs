// Cực kỳ quan trọng: Chỉ import Selenium nếu đang chạy trên Windows
#if WINDOWS
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium.Interactions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
#endif
using AutoAccessLinkA.Models;

namespace AutoAccessLinkA.Services
{
    public class MeetAutomationService
    {
        // Hàm này có thể gọi từ giao diện MAUI ở bất kỳ nền tảng nào
        public async Task StartMeetAsync(string link, BrowserType browser)
        {
#if WINDOWS
            // Nếu là Windows, thực thi logic mở trình duyệt ngầm
            await Task.Run(() => ExecuteAutomation(link, browser));
#else
        // Nếu là Android/iOS, không làm gì cả (hoặc hiển thị thông báo)
        Console.WriteLine("Tính năng tự động hóa trình duyệt chỉ hoạt động trên Windows.");
        await Task.CompletedTask;
#endif
        }

#if WINDOWS
        private void ExecuteAutomation(string link, BrowserType browser)
        {
            IWebDriver driver = null;
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            try
            {
                // --- FACTORY PATTERN: Khởi tạo Driver dựa trên lựa chọn ---
                switch (browser)
                {
                    case BrowserType.Brave:
                        var braveOptions = new ChromeOptions();
                        braveOptions.BinaryLocation = @"C:\Program Files\BraveSoftware\Brave-Browser\Application\brave.exe";
                        ApplyAntiBotOptions(braveOptions);

                        // Profile của Brave
                        string braveDataDir = System.IO.Path.Combine(localAppData, @"BraveSoftware\Brave-Browser\User Data");
                        braveOptions.AddArgument($@"--user-data-dir={braveDataDir}");
                        braveOptions.AddArgument(@"--profile-directory=Default");

                        driver = new ChromeDriver(braveOptions);
                        break;

                    case BrowserType.Chrome:
                        var chromeOptions = new ChromeOptions();
                        ApplyAntiBotOptions(chromeOptions);

                        // Profile của Chrome
                        string chromeDataDir = System.IO.Path.Combine(localAppData, @"Google\Chrome\User Data");
                        chromeOptions.AddArgument($@"--user-data-dir={chromeDataDir}");
                        chromeOptions.AddArgument(@"--profile-directory=Default");

                        driver = new ChromeDriver(chromeOptions);
                        break;

                    case BrowserType.Edge:
                        var edgeOptions = new EdgeOptions();
                        edgeOptions.AddExcludedArgument("enable-automation");
                        edgeOptions.AddAdditionalOption("useAutomationExtension", false);
                        edgeOptions.AddArgument("--disable-blink-features=AutomationControlled");
                        edgeOptions.AddArgument("--use-fake-ui-for-media-stream");

                        // Profile của Edge
                        string edgeDataDir = System.IO.Path.Combine(localAppData, @"Microsoft\Edge\User Data");
                        edgeOptions.AddArgument($@"--user-data-dir={edgeDataDir}");
                        edgeOptions.AddArgument(@"--profile-directory=Default");

                        driver = new EdgeDriver(edgeOptions);
                        break;
                }

                // --- KỊCH BẢN TỰ ĐỘNG HÓA CHUNG ---
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
                driver.Manage().Window.Maximize();
                driver.Navigate().GoToUrl(link);

                // Chờ trang tải xong (readyState = complete)
                wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));

                // Chờ thêm một chút để các thành phần UI của Google Meet render xong (tránh lỗi element not interactable)
                Thread.Sleep(3000);

                // Dùng Actions gửi phím tắt tắt Mic (Ctrl + D) và tắt Cam (Ctrl + E)
                Actions actions = new Actions(driver);
                actions.KeyDown(Keys.Control).SendKeys("d").KeyUp(Keys.Control).Perform();
                Thread.Sleep(1000);
                actions.KeyDown(Keys.Control).SendKeys("e").KeyUp(Keys.Control).Perform();
                Thread.Sleep(2000);

                // Tìm nút tham gia bằng XPath (hỗ trợ tiếng Việt và tiếng Anh)
                var joinButton = wait.Until(d =>
                {
                    var elements = d.FindElements(By.XPath("//button[contains(., 'Tham gia ngay') or contains(., 'Join now') or contains(., 'Tham gia') or contains(., 'Ask to join') or contains(., 'Yêu cầu tham gia')]"));
                    foreach (var element in elements)
                    {
                        if (element.Displayed && element.Enabled)
                            return element;
                    }
                    return null;
                });

                joinButton?.Click();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Lỗi Windows] {ex.Message}");
            }
        }

        // Hàm phụ trợ để tái sử dụng code cấu hình chống Bot cho nhánh Chromium
        private void ApplyAntiBotOptions(ChromeOptions options)
        {
            options.AddExcludedArgument("enable-automation");
            options.AddAdditionalOption("useAutomationExtension", false);
            options.AddArgument("--disable-blink-features=AutomationControlled");
            options.AddArgument("--disable-infobars");
            options.AddArgument("--use-fake-ui-for-media-stream");
        }
#endif
    }
}
