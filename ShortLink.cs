using System;
using Microsoft.AspNetCore.WebUtilities;

namespace UrlShortener
{

    public class ShortLink{
        public int Id{ get; set; }
        public string Url { get; set; }
        
        public string GetUrlChunk(){
            return WebEncoders.Base64UrlEncode(BitConverter.GetBytes(Id));
        }
        public static int GetId(string urlChunk){
            return BitConverter.ToInt32(WebEncoders.Base64UrlDecode(urlChunk));
        }
    }
}