using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Drawing;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;
using ZXing.Windows.Compatibility;
public class QRCodeUtils
{

    public static string GetQRText(byte[] byteQR)
    {

        DecodingOptions readOptions = new() {
            PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE },
            TryHarder = true
        };

        string result = string.Empty;
        using (MemoryStream ms = new MemoryStream(byteQR)){
           Bitmap readQRCodeBitmap = new(ms);
            BarcodeReader reader = new() {
            Options = readOptions
        };
            Result qrCodeResult = reader.Decode(readQRCodeBitmap);
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
        Bitmap bm =  writer.Write(text);
        byte[] bytes;
        using (var ms = new MemoryStream()) 
        {
               bm.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg); 
               bytes = ms.ToArray();
        }
        return bytes;
    }
}