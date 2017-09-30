using HtmlAgilityPack;
using InsTsinghuaPlus.SecurityLevel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;



namespace InsTsinghuaPlus.CoursePage
{
    class Remote_Course
    {
        public static async Task<Timetable> getRemoteTimetable()
        {
            //----------------------------

            //----------------------------
            Debug.WriteLine("[getRemoteTimetable] start");
            await Remote_TOP.LoginToLearn();


            var ticket = await ClassLibrary.POST(
                "http://learn.cic.tsinghua.edu.cn:80/gnt",
                "appId=ALL_ZHJW");

            //await 十１ｓ(); //cross-domain tickets needs some time to take effect

            bool outside_campus_network = false;
            try
            {
                var zhjw = await ClassLibrary.GET(
                    $"http://zhjw.cic.tsinghua.edu.cn/j_acegi_login.do?url=/&ticket={ticket}");
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                if (e.Message.IndexOf("403") == -1)
                    throw e;
                Debug.WriteLine("[getRemoteTimetable] outside campus network");
                //throw new NeedCampusNetworkException();

                outside_campus_network = true;

            }



            if (outside_campus_network)
            {
                //connect via sslvpn
                await Remote_TOP.LogoutSSLVPN();
                await Remote_TOP.LoginSSLVPN();

                await Remote_TOP.LoginToLearn();

                ticket = await ClassLibrary.POST(
                    "http://learn.cic.tsinghua.edu.cn:80/gnt",
                    "appId=ALL_ZHJW");

                await ClassLibrary.十１ｓ();

                var ticketPage = await ClassLibrary.GET(
                    $"https://sslvpn.tsinghua.edu.cn/,DanaInfo=zhjw.cic.tsinghua.edu.cn+j_acegi_login.do?url=/&ticket={ticket}");

                Timetable timetable = new Timetable();

                for (int i = -6; i <= 4; i += 2)
                {
                    string page;
                    try
                    {
                        page = await get_calendar_sslvpn_page(
                            DateTime.Now.AddMonths(i).AddDays(1).ToString("yyyyMMdd"),
                            DateTime.Now.AddMonths(i + 2).ToString("yyyyMMdd")
                            );
                    }
                    catch (Exception)
                    {
                        page = await get_calendar_sslvpn_page(
                            DateTime.Now.AddMonths(i).AddDays(1).ToString("yyyyMMdd"),
                            DateTime.Now.AddMonths(i + 2).ToString("yyyyMMdd")
                            );
                    }
                    var set_to_be_appended = Remote_Course.ParseTimetablePage(page);
                    foreach (var _____ in set_to_be_appended)
                    {
                        timetable.Add(_____);
                    }
                }

                await Remote_TOP.LogoutSSLVPN();

                Debug.WriteLine("[getRemoteTimetable] returning sslvpn");
                return timetable;
            }
            else
            { //TODO: duplicate code

                //connect directly

                Timetable timetable = new Timetable();
                for (int i = -6; i <= 4; i += 2)
                {
                    string page;
                    try
                    {
                        page = await get_calendar_page(
                            DateTime.Now.AddMonths(i).AddDays(1).ToString("yyyyMMdd"),
                            DateTime.Now.AddMonths(i + 2).ToString("yyyyMMdd")
                            );
                    }
                    catch (Exception)
                    {
                        page = await get_calendar_page(
                            DateTime.Now.AddMonths(i).AddDays(1).ToString("yyyyMMdd"),
                            DateTime.Now.AddMonths(i + 2).ToString("yyyyMMdd")
                            );
                    }
                    var set_to_be_appended = ParseTimetablePage(page);
                    foreach (var _____ in set_to_be_appended)
                    {
                        timetable.Add(_____);
                    }
                }

                Debug.WriteLine("[getRemoteTimetable] returning direct");
                return timetable;
            }
        }



        static async Task<string> get_calendar_page(string starting_date, string ending_date)
        {
            Debug.WriteLine($"[get_calendar_page] {starting_date}-{ending_date}");
            var stamp = (long)ClassLibrary.UnixTime().TotalMilliseconds;
            return await ClassLibrary.GET(
                $"http://zhjw.cic.tsinghua.edu.cn/jxmh.do?m=bks_jxrl_all&p_start_date={starting_date}&p_end_date={ending_date}&jsoncallback=_&_={stamp}");
        }
        static async Task<string> get_calendar_sslvpn_page(string starting_date, string ending_date)
        {
            var stamp = (long)ClassLibrary.UnixTime().TotalMilliseconds;
            return await ClassLibrary.GET(
                    $"https://sslvpn.tsinghua.edu.cn/,DanaInfo=zhjw.cic.tsinghua.edu.cn,CT=js+jxmh.do?m=bks_jxrl_all&p_start_date={starting_date}&p_end_date={ending_date}&jsoncallback=_&_={stamp}");
        }

        public static async Task<List<Deadline>> getRemoteHomeworkList(string courseId)
        {
            await Remote_TOP.LoginToLearn();
            return await parseHomeworkListPage(await getHomeworkListPage(courseId), courseId);
        }


        public static async Task<List<Deadline>> getRemoteHomeworkListNew(string courseId)
        {
            await Remote_TOP.LoginToLearn();
            return await parseHomeworkListPageNew(await getHomeworkListPageNew(courseId), courseId);
        }

        public static async Task<List<Announce>> getRemoteAncList(string courseId)
        {
            await Remote_TOP.LoginToLearn();
            return await parseAncListPage(await getAncListPage(courseId), courseId);
        }

        public static async Task<List<Announce>> getRemoteAncListNew(string courseId)
        {
            await Remote_TOP.LoginToLearn();
            return await parseAncPageNew(await getNewAncPage(courseId), courseId);
        }

        public static async Task<List<Course>> getRemoteCourseList()
        {
            await Remote_TOP.LoginToLearn();
            return parseCourseList(await getCourseListPage());
        }

        // remote object URLs and wrappers

        private static string courseListUrl = "http://learn.tsinghua.edu.cn/MultiLanguage/lesson/student/MyCourse.jsp?language=cn";
        public static string learninfo = "http://learn.tsinghua.edu.cn/MultiLanguage/vspace/vspace_userinfo1.jsp";

        public static async Task<string> getHomeworkListPage(string courseId)
        {
            return await ClassLibrary.GET($"http://learn.tsinghua.edu.cn/MultiLanguage/lesson/student/hom_wk_brw.jsp?course_id={courseId}");
        }
        public static async Task<string> getAncListPage(string courseId)
        {
            return await ClassLibrary.GET($"http://learn.tsinghua.edu.cn/MultiLanguage/public/bbs/getnoteid_student.jsp?course_id={courseId}");
        }

        public static async Task<string> getHomeworkListPageNew(string courseId)
        {
            var timestamp = ClassLibrary.UnixTime().TotalMilliseconds;
            string url = $"http://learn.cic.tsinghua.edu.cn/b/myCourse/homework/list4Student/{courseId}/0?_={timestamp}";
            return await ClassLibrary.GET(url);
        }

        public static async Task<string> getCourseListPage()
        {
            return await ClassLibrary.GET(courseListUrl);
        }

        private static async Task<string> getCalendarPage()
        {
            return await ClassLibrary.GET("http://learn.cic.tsinghua.edu.cn/b/myCourse/courseList/getCurrentTeachingWeek");
        }

        //Anc in cic
        public static async Task<string> getNewAncPage(string courseId)
        {
            string Uri = $"http://learn.cic.tsinghua.edu.cn/b/myCourse/notice/listForStudent/{courseId}/";
            return await ClassLibrary.GET(Uri);
        }

        public static async Task<string> GetLearnInfoPage()
        {
            await Remote_TOP.LoginToLearn();
            return await ClassLibrary.GET(learninfo);
        }





        // parse HTML or JSON, and return corresponding Object
        private static Timetable ParseTimetablePage(string page)
        {
            if (page.Length < "_([])".Length)
                throw new ParsePageException("timetable_javascript");
            string json = page.Substring(2, page.Length - 3);
            return ClassLibrary.JSON.parse<Timetable>(json);
        }

        public static async Task<List<Deadline>> parseHomeworkListPage(string page, string courseID)
        {
            try
            {
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(page);

                string _name, _due, _course;

                _course = htmlDoc.DocumentNode.Descendants("td")/*MAGIC*/.First().InnerText;
                _course = _course.Trim();
                _course = _course.Substring(6/*MAGIC*/);
                _course = WebUtility.HtmlDecode(_course);

                HtmlNode[] nodes = htmlDoc.DocumentNode.Descendants("tr")/*MAGIC*/.ToArray();


                List<Deadline> deadlines = new List<Deadline>();
                for (int i = 4/*MAGIC*/; i < nodes.Length - 1/*MAGIC*/; i++)
                {
                    HtmlNode node = nodes[i];

                    var tds = node.Descendants("td");

                    var _isFinished = (tds.ElementAt(3/*MAGIC*/).InnerText.Trim() == "已经提交");

                    _due = tds.ElementAt(2/*MAGIC*/).InnerText;

                    var link_to_detail = node.Descendants("a")/*MAGIC*/.First();
                    _name = link_to_detail.InnerText;
                    _name = WebUtility.HtmlDecode(_name);

                    var _href = link_to_detail.Attributes["href"].Value;
                    var _id = Regex.Match(_href, @"[^_]id=(\d+)").Groups[1].Value;

                    var _cplhref = "http://learn.tsinghua.edu.cn/MultiLanguage/lesson/student/" + _href;

                    var _detail = await parseHWListContent(_cplhref);
                    _detail = "<b>" + _name + "</b>" + "<br>" + _detail;

                    deadlines.Add(new Deadline
                    {
                        name = _name,
                        ddl = _due,
                        course = _course,
                        hasBeenFinished = _isFinished,
                        id = "@" + _id,
                        detail = _detail,
                        courseid = courseID
                    });
                }
                return deadlines;
            }
            catch (Exception)
            {
                throw new ParsePageException("AssignmentList");
            }

        }

        public static async Task<string> parseHWListContent(string uri)
        {
            string page = await ClassLibrary.GET(uri);
            string resultString = null;
            var subjectString = page;
            try
            {
                Regex regexObj = new Regex(@"<td height=""100"" class=""title""> 作业说明</td>\r\n\t\t\t<td class=""tr_2""><textarea cols=55  rows=""7"" style=""width:650px""  wrap=VIRTUAL>(?<mycontent>[\s\S].*?)</textarea></td>");
                resultString = regexObj.Match(subjectString).Groups["mycontent"].Value;

                if (resultString.Length == 0)
                {
                    try
                    {
                        Regex regexObj1 = new Regex(@"<textarea cols=55  rows=""7"" style=""width:650px""  wrap=VIRTUAL>(?<mycontent>[\s\S]+)\n</textarea>");
                        resultString = regexObj1.Match(subjectString).Groups["mycontent"].Value;
                        if (resultString.Length == 0)
                            resultString = "无内容或如题";
                    }
                    catch
                    {

                    }
                }


            }
            catch 
            {
                // Syntax error in the regular expression
            }

            return resultString;
        }
        public static async Task<List<Deadline>> parseHomeworkListPageNew(string page, string courseID)
        {

            List<Deadline> deadlines = new List<Deadline>();

            string _course = "";
            var root = ClassLibrary.JSON.parse<CourseAssignmentsRootobject>(page);
            foreach (var item in root.resultList)
            {
                var _isFinished = (item.courseHomeworkRecord.status != "0" /*MAGIC*/);

                var _dueTimeStamp = item.courseHomeworkInfo.endDate;
                var _dueDate = (new DateTime(1970, 1, 1, 0, 0, 0, 0)).ToLocalTime().AddMilliseconds(_dueTimeStamp).Date;
                string _due = $"{_dueDate.Year}-{_dueDate.Month}-{_dueDate.Day}";

                string _name = item.courseHomeworkInfo.title;
                string _courseId = item.courseHomeworkInfo.courseId;
                string _detail = item.courseHomeworkInfo.detail;
                if (_detail == null)
                    _detail = "无内容";

                _detail = "<b>" + _name + "</b><br>" + _detail;
                if (_course == "")
                    _course = _courseId;
                if (_course == _courseId)
                {
                    foreach (var course in await DataAccess_Course.GetCourses())
                    {
                        if (course.id == _courseId)
                            _course = course.name;
                    }
                }

                _course = WebUtility.HtmlDecode(_course);
                _name = WebUtility.HtmlDecode(_name);

                deadlines.Add(new Deadline
                {
                    name = _name,
                    ddl = _due,
                    course = _course,
                    hasBeenFinished = _isFinished,
                    id = "_" + item.courseHomeworkInfo.homewkId,
                    detail = _detail,
                    courseid = courseID
                });
            }
            return deadlines;
        }



        public static async Task<List<Announce>> parseAncPageNew(string page, string courseid)
        {

            List<Announce> announces = new List<Announce>();
            var root = ClassLibrary.JSON.parse<AncRootObject>(page);
            foreach (var item in root.paginationList.recordList)
            {

                var _isFinished = (item.status != "0");

                string _due = item.courseNotice.regDate;
                string _name = item.courseNotice.title;
                string _courseId = item.courseNotice.courseId;
                string _owner = item.courseNotice.owner;
                string _detail = item.courseNotice.detail;
                string _id = item.courseNotice.id;
                string _coursename = "";
                try
                {
                    var totalcss = await DataAccess_Course.GetCourses();
                    var _courseitem = totalcss.Where(p => p.id == courseid).ToList();
                    _coursename = _courseitem[0].name;
                }
                catch
                {
                    Debug.WriteLine("[parseAncPage] id not match name");
                }

                announces.Add(new Announce
                {
                    title = _name,
                    regDate = _due,
                    owner = _owner,
                    id = _id,
                    detail = _detail,
                    courseId = courseid,
                    Isfinished = _isFinished,
                    course = _coursename

                });
            }

            return announces;
        }

        public static List<Course> parseCourseList(string page)
        {
            try
            {
                List<Course> courses = new List<Course>();

                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(page);
                var links = htmlDoc.DocumentNode.Descendants("table")/*MAGIC*/.Last()/*MAGIC*/.Descendants("a")/*MAGIC*/.ToArray();

                foreach (var link in links)
                {
                    string _name = link.InnerText.Trim();
                    string _url = link.Attributes["href"].Value;
                    var match = Regex.Match(_name, "(.+?)\\((\\d+)\\)\\((.+?)\\)");
                    string _semester = match.Groups[3].Value;
                    _name = match.Groups[1].Value;
                    bool _isNew = false;
                    string _id = "";

                    if (_url.StartsWith("http://learn.cic.tsinghua.edu.cn/"))
                    {
                        _isNew = true;
                        _id = Regex.Match(_url, "/([-\\d]+)").Groups[1].Value;
                    }
                    else
                    {
                        _isNew = false;
                        _id = Regex.Match(_url, "course_id=(\\d+)").Groups[1].Value;
                    }
                    courses.Add(new Course
                    {
                        name = _name,
                        isNew = _isNew,
                        id = _id,
                        semester = _semester
                    });
                }
                return courses;
            }
            catch (Exception)
            {
                throw new ParsePageException("CourseList");
            }
        }

        public static async Task<List<Announce>> parseAncListPage(string page, string courseid)
        {
            try
            {
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(page);

                string _name, _due, _course, _detail, _owner;

                _course = htmlDoc.DocumentNode.Descendants("td")/*MAGIC*/.First().InnerText;
                _course = _course.Trim();
                _course = _course.Substring(6/*MAGIC*/);
                _course = WebUtility.HtmlDecode(_course);

                HtmlNode[] nodes = htmlDoc.DocumentNode.Descendants("tr")/*MAGIC*/.ToArray();


                List<Announce> announces = new List<Announce>();
                for (int i = 3/*MAGIC*/; i < nodes.Length - 1/*MAGIC*/; i++)
                {
                    HtmlNode node = nodes[i];

                    var tds = node.Descendants("td");

                    var _isFinished = (tds.ElementAt(4/*MAGIC*/).InnerText.Trim() == "已读");

                    _owner = tds.ElementAt(2).InnerText;

                    _due = tds.ElementAt(3/*MAGIC*/).InnerText;

                    var link_to_detail = node.Descendants("a")/*MAGIC*/.First();
                    _name = link_to_detail.InnerText;
                    _name = WebUtility.HtmlDecode(_name);
                    //
                    var _href = link_to_detail.Attributes["href"].Value;
                    var _id = Regex.Match(_href, @"[^_]id=(\d+)").Groups[1].Value;

                    var _cplhref = "http://learn.tsinghua.edu.cn/MultiLanguage/public/bbs/" + _href;
                    _detail = await parseAncListContent(_cplhref);
                    _detail = "<b>" + _name + "</b>" + "<br>" + _detail;
                    //_detail = await parseAncListContent(_cplhref);
                    string _coursename = "";
                    try
                    {
                        var totalcss = await DataAccess_Course.GetCourses();
                        var _courseitem = totalcss.Where(p => p.id == courseid).ToList();
                        _coursename = _courseitem[0].name;
                    }
                    catch
                    {
                        Debug.WriteLine("[parseAncPage] id not match name");
                    }

                    announces.Add(new Announce
                    {
                        title = _name,
                        regDate = _due,
                        id = _id,
                        detail = _detail,
                        courseId = courseid,
                        owner = _owner,
                        Isfinished = _isFinished,
                        course = _coursename
                    });
                }
                return announces;

            }
            catch (Exception)
            {
                throw new ParsePageException("AssignmentList");
            }

        }


        public static async Task<string> parseAncListContent(string uri)
        {
            string page = await ClassLibrary.GET(uri);

            var subjectString = page;

            string resultString = null;
            try
            {
                Regex regexObj = new Regex(@"<td width=\""87%\"" class=\""tr_l2\"" colspan=\""3\"" style=\""table-layout:fixed; word-break: break-all; overflow:hidden;\"">(?<mycontent>[\s\S]*)\t\t</td>");
                resultString = regexObj.Match(subjectString).Groups["mycontent"].Value;
                if (resultString.Length == 0)
                    resultString = "无内容或如题";
            }
            catch 
            {
                // Syntax error in the regular expression
            }

            return resultString;
        }




    }
}
