
using ZXing;
using ZXing.Common;
using ZXing.QrCode;
using SkiaSharp;
using ZXing.SkiaSharp;


public class QRCodeUtils
{

    public static string GetQRText(byte[] byteQR)
    {

        DecodingOptions readOptions = new() {
            PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE },
            TryHarder = true
        };

        string result = string.Empty;
        // Convert byte array to SKBitmap
        using (var ms = new MemoryStream(byteQR))
        using (var skStream = new SKManagedStream(ms))
        using (var skBitmap = SKBitmap.Decode(skStream))
        {
            // Use skBitmap with ZXing.SkiaSharp
            var reader = new BarcodeReader();
            var qrCodeResult = reader.Decode(skBitmap);
            result = qrCodeResult.Text;
        }
        

        return result;
    }
    public static byte[] GetQRCodes(string text)
    {
        
    
        QrCodeEncodingOptions options = new() {
            DisableECI = true,
            CharacterSet = "UTF-8",
            Width = 500,
            Height = 500
        };

        BarcodeWriter writer = new()
        {
            Format = BarcodeFormat.QR_CODE,
            Options = options
        };
        // Generate QR code as SKBitmap

        SKBitmap skBitmap = writer.Write(text);

        // Convert SKBitmap to byte array (JPEG)
        using var image = SKImage.FromBitmap(skBitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 100);
        byte[] bytes = data.ToArray();
        // Bitmap bm =  writer.Write(text);
        // byte[] bytes;
        // using (var ms = new MemoryStream()) 
        // {
        //        bm.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg); 
        //        bytes = ms.ToArray();
        // }

       // File.WriteAllBytes("C:\\temp\\qrcode.jpg", bytes); // Save to file for debugging
        return bytes;
    }
}