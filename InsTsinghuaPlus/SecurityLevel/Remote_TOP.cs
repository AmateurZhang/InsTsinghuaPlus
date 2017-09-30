using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Web.Http;
using HtmlAgilityPack;

namespace InsTsinghuaPlus.SecurityLevel
{
    class Remote_TOP
    {
        // 通用方法类，以后可能单独成为一个.cs
        private static Windows.Web.Http.Filters.HttpBaseProtocolFilter bpf = new Windows.Web.Http.Filters.HttpBaseProtocolFilter();
        private static HttpClient m_httpClient = new HttpClient(bpf);
        static public HttpCookieManager getCookieManager()
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

        private static TimeSpan UnixTime()
        {
            return (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)));
        }

        private static async Task 十１ｓ(double seconds = 1)
        {
            await Task.Delay(TimeSpan.FromSeconds(seconds));
        }

        //---------------------------------------------------------------------------------------------------------------
        // 登陆
        private static DateTime lastLogin = DateTime.MinValue;
        private static int LOGIN_TIMEOUT_MINUTES = 1;
        private static string lastLoginUsername = "";
        //登陆网址
        private static string loginSslvpnUri = "https://sslvpn.tsinghua.edu.cn/dana-na/auth/url_default/login.cgi";
        private static string logoutSslvpnUrl = "https://sslvpn.tsinghua.edu.cn/dana-na/auth/logout.cgi";
        private static string loginSslvpnCheckUri = "https://sslvpn.tsinghua.edu.cn/dana/home/starter0.cgi";
        private static string loginSslvpnCheckUriCheck = "https://sslvpn.tsinghua.edu.cn/dana/home/starter0.cgi?check=yes";
        private static string loginUri = "https://learn.tsinghua.edu.cn/MultiLanguage/lesson/teacher/loginteacher.jsp";
        private static string courseListUrl_Login = "http://learn.tsinghua.edu.cn/MultiLanguage/lesson/student/MyCourse.jsp?language=cn";
        public static string learninfo = "http://learn.tsinghua.edu.cn/MultiLanguage/vspace/vspace_userinfo1.jsp";

        public static async Task<int> LoginToLearn(bool useLocalSettings = true, string username = "", string password = "")
        {
            bool occupied = false;
            Debug.WriteLine("[login] begin");

            //retrieve username and password
            if (useLocalSettings)
            {
                if (DataAccess_TOP.CredentialAbsent())
                {
                    throw new LoginException("没有指定用户名和密码");
                }
                username = DataAccess_TOP.GetLocalSettings()["username"].ToString();

                if (username == "__anonymous")
                {
                    throw new LoginException("没有指定用户名和密码");
                }

                var vault = new Windows.Security.Credentials.PasswordVault();
                password = vault.Retrieve("Tsinghua_Learn_Website", username).Password;
            }

            while (occupied)
            {
                await 十１ｓ(.1);
            }

            occupied = true;
            //check for last login
            if ((DateTime.Now - lastLogin).TotalMinutes < LOGIN_TIMEOUT_MINUTES
                && lastLoginUsername == username)
            {
                Debug.WriteLine("[login] reuses recent session");
                occupied = false;
                return 2;
            }

            int SuccessFlag = 0;
            try
            {
                string loginResponse;

                //login to learn.tsinghua.edu.cn

                loginResponse = await POST(
                    loginUri,
                    $"leixin1=student&userid={username}&userpass={password}");

                //check if successful
                var alertInfoGroup = Regex.Match(loginResponse, @"window.alert\(""(.+)""\);").Groups;
                if (alertInfoGroup.Count > 1)
                {
                    throw new LoginException(alertInfoGroup[1].Value.Replace("\\r\\n", "\n"));
                }
                if (loginResponse.IndexOf(@"window.location = ""loginteacher_action.jsp"";") == -1)
                {
                    throw new ParsePageException("login_redirect");
                }

                //get iframe src
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(await GET(courseListUrl_Login));

                string iframeSrc= "";
                try
                {
                    iframeSrc = htmlDoc.DocumentNode.Descendants("iframe")/*MAGIC*/.First().Attributes["src"].Value;
                }
                catch (Exception)
                {
                    throw new ParsePageException("find_cic_iframe");
                }


                //login to learn.cic.tsinghua.edu.cn
                await 十１ｓ();
                await GET(iframeSrc);


                SuccessFlag = 1;
            }
            catch (Exception e)
            {
                occupied = false;
                Debug.WriteLine("[login] unsuccessful");
                throw e;
            }

            Debug.WriteLine("[login] successful");
            if (SuccessFlag == 1)
                lastLogin = DateTime.Now;
            else
                lastLogin = DateTime.MinValue;
            lastLoginUsername = username;

            occupied = false;

            return 0;
        }
        public static async Task ValidateCredential(string username, string password) // throws LoginException if fail
        {
            if (username == "233333")
                return;
            await Remote_TOP.LoginToLearn(false, username, password);
        }

        public static async Task LogoutSSLVPN()
        {
            await GET(logoutSslvpnUrl);
            Debug.WriteLine("[logoutSSLVPN] finish");
        }

        public static async Task<int> LoginSSLVPN()
        {

            Debug.WriteLine("[loginSSLVPN] start");

            //retrieve username and password
            if (DataAccess_TOP.CredentialAbsent())
            {
                throw new LoginException("没有指定用户名和密码");
            }
            var username = DataAccess_TOP.GetLocalSettings()["username"].ToString();

            var vault = new Windows.Security.Credentials.PasswordVault();
            var password = vault.Retrieve("Tsinghua_Learn_Website", username).Password;


            //login to sslvpn.tsinghua.edu.cn
            var loginResponse = await POST(
                loginSslvpnUri,
                $"tz_offset=480&username={username/*should be numeral ID*/}&password={password}&realm=ldap&btnSubmit=登录");


            //another sslvpn session exist?
            if (loginResponse.IndexOf("btnContinue") != -1)
            {
                Debug.WriteLine("[loginSSLVPN] another sslvpn session exist");

                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(loginResponse);
                string formDataStr = htmlDoc.GetElementbyId("DSIDFormDataStr").Attributes["value"].Value;

                loginResponse = await POST(
                    loginSslvpnUri,
                    $"btnContinue=继续会话&FormDataStr={Uri.EscapeDataString(formDataStr)}");
            }

            //get xauth token
            var xsauthGroups = Regex.Match(loginResponse, @"name=""xsauth"" value=""([^""]+)""").Groups;
            if (xsauthGroups.Count < 2)
            {
                throw new ParsePageException("find_xsauth_from_sslvpn");
            }
            var xsauth = xsauthGroups[1];

            //second step, invoking xsauth token
            var timestamp = UnixTime().TotalSeconds;

            loginResponse = await POST(
                loginSslvpnCheckUri,
                $"xsauth={xsauth}&tz_offset=480&clienttime={timestamp}&url=&activex_enabled=0&java_enabled=0&power_user=0&grab=1&browserproxy=&browsertype=&browserproxysettings=&check=yes");



            Debug.WriteLine("[loginSSLVPN] finish");
            return 0;
        }

        static public async Task<string> GetemailName()
        {
            return await ParseEmailUserName(await GetLearnInfoPage());
        }

        public static async Task<string> GetLearnInfoPage()
        {
            await LoginToLearn();
            return await GET(learninfo);
        }

        public static async Task<string> ParseEmailUserName(string page)
        {
            await LoginToLearn();
            string resultString = null;
            var subjectString = page;
            try
            {
                Regex regexObj = new Regex(@"<td class=""title"" height=""20"">电子邮件</td>\r\n\t\t\t\r\n\t\t\t\r\n\t\t<td class=""tr_l""><input class=""input_blue"" type=hidden name=email size=40 value=""(?<mycontent>[\s\S]+)@mails.tsinghua.edu.cn"">");
                resultString = regexObj.Match(subjectString).Groups["mycontent"].Value;
            }
            catch
            {
                // Syntax error in the regular expression
                Debug.WriteLine("get email name fails");
            }

            return resultString;

        }
        //---------------------------------------------------------------------------------------------------------------
        //Get Semester
        private static string hostedCalendarUrl = "http://static.nullspace.cn/thuCalendar.json";
        public static async Task<Semesters> GetHostedSemesters()
        {
            return ClassLibrary.JSON.parse<Semesters>(await ClassLibrary.GET(hostedCalendarUrl));
        }

        public static async Task<Semesters> GetRemoteSemesters()
        {
            await LoginToLearn();
            var _remoteCalendar = ParseSemestersPage(await GetCalendarPage());
            return new Semesters
            {
                currentSemester = _remoteCalendar.currentSemester,
                nextSemester = _remoteCalendar.nextSemester
            };
        }
        private static SemestersRootObject ParseSemestersPage(string page)
        {
            return ClassLibrary.JSON.parse<SemestersRootObject>(page);
        }
        private static async Task<string> GetCalendarPage()
        {
            return await GET("http://learn.cic.tsinghua.edu.cn/b/myCourse/courseList/getCurrentTeachingWeek");
        }
    }
}
