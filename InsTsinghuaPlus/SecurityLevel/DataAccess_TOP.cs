using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace InsTsinghuaPlus.SecurityLevel
{
    class DataAccess_TOP
    {
        public static ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        private static Semesters semesters = null;
        static string SEMESTERS_FILENAME = "semesters.json";


        static public Windows.Foundation.Collections.IPropertySet GetLocalSettings()
        {
            return localSettings.Values;
        }

        static public void SetLocalSettings<Type>(string key, Type value)
        {
            localSettings.Values[key] = value;
        }

        //网络环境
        static public async Task<bool> OutOfCampus()
        {
            bool outside_campus_network = false;
            string aca = "https://academic.tsinghua.edu.cn";
            try
            {
                var result = await Remote_TOP.GET(aca);
            }
            catch
            {
                outside_campus_network = true;
            }
            return outside_campus_network;
        }


        //登陆状态
        static public bool CredentialAbsent()
        {
            var username = localSettings.Values["username"];
            return username == null
                || username.ToString() == "__anonymous";
        }

        static public bool SupposedToWorkAnonymously()
        {
            var username = localSettings.Values["username"];
            return username != null
                && username.ToString() == "__anonymous";
        }

        static public bool IsDemo()
        {
            return
                localSettings.Values["username"] != null &&
                localSettings.Values["username"].ToString() == "233333";
        }

        static public async Task<string> GetemailName()
        {
            if (IsDemo())
                return "TEST MODE";
            else
                return await Remote_TOP.GetemailName();
        }

        public static async Task<Semester> GetSemester(bool forceRemote = false, bool getNextSemester = false)
        {
            if (IsDemo())
            {
                var start = DateTime.Now.AddDays(-20);
                while (start.DayOfWeek != DayOfWeek.Monday)
                    start = start.AddDays(-1);

                if (!getNextSemester)
                    return new Semester
                    {
                        startDate = start.ToString("yyyy-MM-dd"),
                        endDate = start.AddDays(10 * 7 - 1).ToString("yyyy-MM-dd"),
                        semesterEname = "2333-2334-Spring",
                    };
                else
                    return new Semester
                    {
                        startDate = start.AddDays(10 * 7).ToString("yyyy-MM-dd"),
                        endDate = start.AddDays(20 * 7 - 1).ToString("yyyy-MM-dd"),
                        semesterEname = "2334-2335-Autumn",
                    };
            }
            if (!forceRemote)
            {
                Semesters __semesters = null;
                //try memory
                if (semesters != null)
                {
                    Debug.WriteLine("[getCalendar] memory");
                    __semesters = semesters;
                }
                else //try localSettings
                {
                    var localJSON = await ClassLibrary.ReadCache(SEMESTERS_FILENAME);
                    if (localJSON.Length > 0)
                    {
                        Debug.WriteLine("[getCalendar] local settings");
                        __semesters = ClassLibrary.JSON.parse<Semesters>(localJSON);
                    }
                }

                //cache hit
                if (__semesters != null)
                {
                    if (getNextSemester)
                        return __semesters.nextSemester;

                    if (DateTime.Parse(__semesters.currentSemester.endDate + " 23:59") < DateTime.Now)
                    {
                        //perform a remote update
                        Task task = GetSemester(forceRemote: true);
                        await task.ContinueWith((_) => 0);

                        Debug.WriteLine("[getCalendar] Returning cache next");
                        if (__semesters.nextSemester.endDate == null)
                        {
                            //automatically complete missing endDate
                            if (__semesters.nextSemester.semesterEname.IndexOf("Autumn") != -1
                                || __semesters.nextSemester.semesterEname.IndexOf("Spring") != -1)
                                __semesters.nextSemester.endDate = DateTime.Parse(__semesters.nextSemester.startDate).AddDays(18 * 7 - 1).ToString();
                        }
                        return __semesters.nextSemester;
                    }
                    Debug.WriteLine("[getCalendar] Returning cache");
                    return __semesters.currentSemester;
                }
            }

            //fetch from remote
            Semesters _remoteSemesters = null;

            if (CredentialAbsent() == false)
            {
                try
                {
                    _remoteSemesters = await Remote_TOP.GetRemoteSemesters();
                }
                catch (Exception) { }
            }
            if (_remoteSemesters == null)
            {
                Debug.WriteLine("[getCalendar] remote fail, falling back to hosted");
                _remoteSemesters = await Remote_TOP.GetHostedSemesters();
            }

            semesters = _remoteSemesters;
            await ClassLibrary.WriteCache(SEMESTERS_FILENAME, ClassLibrary.JSON.stringify(semesters));

            Debug.WriteLine("[getCalendar] Returning remote");
            return semesters.currentSemester;
        }

        //取得用户名
        static public string GetUserName()
        {
            return localSettings.Values["username"].ToString();
        }
        //取得用户学号
        static public string GetUserNumber()
        {
            return localSettings.Values["usernumber"].ToString();
        }


    }
}
