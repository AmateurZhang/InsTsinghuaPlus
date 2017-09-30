using InsTsinghuaPlus.SecurityLevel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Web.Http;

namespace InsTsinghuaPlus
{
    class ClassLibrary
    {

        // 通用方法类，以后可能单独成为一个.cs
        private static Windows.Web.Http.Filters.HttpBaseProtocolFilter bpf = new Windows.Web.Http.Filters.HttpBaseProtocolFilter();
        private static HttpClient m_httpClient = new HttpClient(bpf);
        static public HttpCookieManager GetCookieManager()
        {
            return bpf.CookieManager;
        }

        private static HttpResponseMessage httpResponse = new HttpResponseMessage();
        public static async Task<string> GET(string url)
        {
            //getPage
            httpResponse = await m_httpClient.GetAsync(new Uri(url));
            httpResponse.EnsureSuccessStatusCode();
            return await httpResponse.Content.ReadAsStringAsync();
        }

        //注意此处需要改回private
        public static async Task<string> POST(string url, string form_string)
        {
            HttpStringContent stringContent = new HttpStringContent(
                form_string,
                Windows.Storage.Streams.UnicodeEncoding.Utf8,
                "application/x-www-form-urlencoded");

            httpResponse = await m_httpClient.PostAsync(new Uri(url), stringContent);
            httpResponse.EnsureSuccessStatusCode();
            return await httpResponse.Content.ReadAsStringAsync();
        }

        public static TimeSpan UnixTime()
        {
            return (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)));
        }

        public static async Task 十１ｓ(double seconds = 1)
        {
            await Task.Delay(TimeSpan.FromSeconds(seconds));
        }

        // wrapped JSON parser
        public class JSON
        {
            public static T parse<T>(string jsonString)
            {
                try
                {
                    using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonString)))
                    {
                        return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(ms);
                    }
                }
                catch (Exception)
                {
                    throw new ParsePageException("JSON " + typeof(T).ToString());
                }
            }

            public static string stringify(object jsonObject)
            {
                using (var ms = new MemoryStream())
                {
                    new DataContractJsonSerializer(jsonObject.GetType()).WriteObject(ms, jsonObject);

                    return Encoding.UTF8.GetString(ms.ToArray());
                }
            }



        }

        //Cache 读写
        public static async Task WriteCache(string filename, string value)
        {
            StorageFolder localCacheFolder = ApplicationData.Current.LocalCacheFolder;
            StorageFile file;
            try
            {
                file = await localCacheFolder.GetFileAsync(filename);
            }
            catch
            {
                file = await localCacheFolder.CreateFileAsync(filename);
            }
            await FileIO.WriteTextAsync(file, value);
        }

        public static async Task<string> ReadCache(string filename)
        {
            try
            {
                StorageFolder localCacheFolder = ApplicationData.Current.LocalCacheFolder;
                StorageFile file = await localCacheFolder.GetFileAsync(filename);
                String fileContent = await FileIO.ReadTextAsync(file);
                return fileContent;
            }
            catch
            {
                return "";
            }
        }

    }
}
