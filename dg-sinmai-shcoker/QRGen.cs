using MelonLoader;
using MelonLoader.Utils;
using QRCoder;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Web;
namespace dg_sinmai_shcoker
{
    public static class QRGenerater
    {
        public static void QRGen(string ws_url, string clientId)
        {
            try
            {
                string outputPath = Path.Combine(MelonEnvironment.UserDataDirectory, "qrcode.png");
                if (!string.IsNullOrEmpty(clientId))
                {
                    string qrContent = $"https://www.dungeon-lab.com/app-download.php#DGLAB-SOCKET#{ws_url}#{clientId}";
                    MelonLogger.Msg("生成二维码内容：" + qrContent);
                    MelonLogger.Msg(""); // 打印一行空行
                    GenerateQRCodeToFile(qrContent, outputPath);
                    OpenQRCodeImage(outputPath);
                    /*
                    QRCodeGenerator qrGenerator = new QRCodeGenerator();
                    QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.Q);
                    using Bitmap qrBitmap = new QRCode(qrCodeData).GetGraphic(1);
                    
                    for (int y = 0; y < qrBitmap.Height; y++)
                    {
                        string line = "";
                        for (int x = 0; x < qrBitmap.Width; x++)
                        {
                            Color pixel = qrBitmap.GetPixel(x, y);
                            // 使用双宽字符模拟二维码方块
                            line += pixel.R < 128 ? "██" : "  ";
                        }
                        MelonLogger.Msg(line);
                        MelonLogger.Msg(""); // 结束空行;
                    }*/
                }
                else
                {
                    MelonLogger.Warning("JSON 中找不到 clientId 字段");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error("解析 JSON 或生成二维码失败：" + ex.ToString());
            }

        }
        private static void GenerateQRCodeToFile(string content, string path)
        {
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                QRCodeData qrData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
                using (QRCode qrCode = new QRCode(qrData))
                {
                    using (Bitmap qrImage = qrCode.GetGraphic(20))
                    {
                        qrImage.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                        MelonLogger.Msg($"二维码已保存到: {path}");
                    }
                }
            }
        }
        private static void OpenQRCodeImage(string path)
        {
            var psi = System.Diagnostics.Process.Start(path);
        }
    }
}


