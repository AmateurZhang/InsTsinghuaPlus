using InsTsinghuaPlus.SecurityLevel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace InsTsinghuaPlus.CoursePage
{
   class DataAccess_Course
    {
        private static ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        private static List<Course> courses = null;
        private static List<Deadline> deadlines = null;
        private static List<Announce> announces = null;

        static string DEADLINES_FILENAME = "deadlines.json";
        static string COURSES_FILENAME = "courses.json";
        static string ANNOUNCE_FILENAME = "announce.json";

        static async public Task<int> UpdateAllFromRemote()
        {
            await GetCourses(true);
            await GetTimetable(true);
            await GetAllDeadlines(true);
            await GetAllAnnounce(true);
            return 0;
        }

        static public List<Deadline> SortDeadlines(List<Deadline> assignments, int limit = -1)
        {

            var future = (from assignment in assignments
                          where !assignment.isPast()
                          orderby assignment.daysFromNow() ascending
                          select assignment);

            int futureCount = future.Count();

            if (futureCount > limit && limit >= 0)
            {
                return future.Take(limit).ToList();
            }


            var past = (from assignment in assignments
                        where assignment.isPast()
                        orderby assignment.daysFromNow() descending
                        select assignment);


            if (limit < 0)
            {
                return future.Concat(past).ToList();
            }

            return future.Concat(past.Take(limit - futureCount)).ToList();
        }
        static public List<Announce> SortAnc(List<Announce> assignments, int limit = -1)
        {

            var future = (from assignment in assignments
                          where !assignment.Isfinished
                          orderby assignment.daysFromNow() ascending
                          select assignment);

            int futureCount = future.Count();

            if (futureCount > limit && limit >= 0)
            {
                return future.Take(limit).ToList();
            }


            var past = (from assignment in assignments
                        where assignment.Isfinished
                        orderby assignment.daysFromNow() descending
                        select assignment);


            if (limit < 0)
            {
                return future.Concat(past).ToList();
            }

            return future.Concat(past.Take(limit - futureCount)).ToList();
        }

        public static async Task<List<Course>> GetCourses(bool forceRemote = false)
        {
            if (DataAccess_TOP.IsDemo())
            {
                var list = new List<Course>();
                list.Add(new Course
                {
                    name = "数据结构",
                    id = "demo_course_0",
                });

                list.Add(new Course
                {
                    name = "操作系统",
                    id = "demo_course_1",
                });

                return list;
            }

            if (!forceRemote)
            {
                //try memory
                if (courses != null)
                {
                    Debug.WriteLine("[getCourses] Returning memory");
                    return courses;
                }

                //try localSettings
                var localCourses = await ClassLibrary.ReadCache(COURSES_FILENAME);
                if (localCourses.Length > 0)
                {
                    Debug.WriteLine("[getCourses] Returning local settings");
                    courses = ClassLibrary.JSON.parse<List<Course>>(localCourses);
                    return courses;
                }
            }


            //fetch from remote
            var _courses = await Remote_Course.getRemoteCourseList();
            courses = _courses;
            await ClassLibrary.WriteCache(COURSES_FILENAME, ClassLibrary.JSON.stringify(_courses));

            Debug.WriteLine("[getCourses] Returning remote");
            return courses;
        }

        public static async Task<Timetable> GetTimetable(bool forceRemote = false)
        {
            if (DataAccess_TOP.IsDemo())
            {
                var table = new Timetable();

                var start = DateTime.Now.AddDays(-20);
                while (start.DayOfWeek != DayOfWeek.Monday)
                    start = start.AddDays(-1);

                for (var i = 0; i < 10; i++)
                {
                    table.Add(new Event
                    {
                        nr = "形式语言与自动机",
                        dd = "六教 6A301",
                        nq = start.AddDays(i * 7 + 2).ToString("yyyy-MM-dd"),
                        kssj = "08:00",
                        jssj = "09:35"

                    });

                    table.Add(new Event
                    {
                        nr = "高级数据结构",
                        dd = "六教 6A301",
                        nq = start.AddDays(i * 7 + 2).ToString("yyyy-MM-dd"),
                        kssj = "09:50",
                        jssj = "11:25"
                    });

                    table.Add(new Event
                    {
                        nr = "操作系统",
                        dd = "六教 6A303",
                        nq = start.AddDays(i * 7 + 3).ToString("yyyy-MM-dd"),
                        kssj = "09:50",
                        jssj = "11:25"
                    });

                    table.Add(new Event
                    {
                        nr = "概率论与数理统计",
                        dd = "六教 6C102",
                        nq = start.AddDays(i * 7 + 4).ToString("yyyy-MM-dd"),
                        kssj = "15:20",
                        jssj = "16:55"
                    });

                    table.Add(new Event
                    {
                        nr = "概率论与数理统计",
                        dd = "一教 104",
                        nq = start.AddDays(i * 7 + 1).ToString("yyyy-MM-dd"),
                        kssj = "13:30",
                        jssj = "15:05"
                    });
                }
                return table;
            }

            //fetch from remote
            var _remoteTimetable = await Remote_Course.getRemoteTimetable();
            Debug.WriteLine("[getTimetable] Returning remote");
            return _remoteTimetable;
        }

        static public async Task<List<Deadline>> GetAllDeadlines(bool forceRemote = false)
        {
            if (DataAccess_TOP.IsDemo())
            {
                var list = new List<Deadline>();
                var start = DateTime.Now.AddDays(-20);
                while (start.DayOfWeek != DayOfWeek.Monday)
                    start = start.AddDays(-1);

                for (var i = 0; i <= 3; i++)
                {
                    list.Add(new Deadline
                    {
                        course = "操作系统",
                        ddl = start.AddDays(i * 7 + 4 + 7).ToString("yyyy-MM-dd"),
                        name = $"代码阅读报告{i + 1}",
                        hasBeenFinished = (i < 3),
                        id = "operating_systems_" + i.ToString(),
                        courseid = "demo_course_1",
                        detail = $"{i}个有趣的报告"
                    });
                }

                for (var i = 0; i <= 3; i++)
                {
                    list.Add(new Deadline
                    {
                        course = "数据结构",
                        ddl = start.AddDays(i * 7 + 3 + 7).ToString("yyyy-MM-dd"),
                        name = $"数据结构习题{i + 1}",
                        hasBeenFinished = (i < 3),
                        id = "data_structure_" + i.ToString(),
                        courseid = "demo_course_0",
                        detail = $"DSA{i + 10}"

                    });
                }

                return list;
            }
            if (!forceRemote)
            {
                //try session memory
                if (deadlines != null)
                {
                    Debug.WriteLine("[getAllDeadlines] Returning memory");
                    return deadlines;
                }


                //try local
                var cache = await ClassLibrary.ReadCache(DEADLINES_FILENAME);
                if (cache.Length > 0)
                {
                    Debug.WriteLine("[getAllDeadlines] Returning local");
                    return ClassLibrary.JSON.parse<List<Deadline>>(cache);
                }
            }

            //fetch from remote

            List<Deadline> _deadlines = new List<Deadline>();

            foreach (var course in await GetCourses(forceRemote))
            {
                Debug.WriteLine("[getAllDeadlines] Remote " + course.name);
                var id = course.id;
                List<Deadline> __deadlines;
                if (course.isNew)
                    __deadlines = await Remote_Course.getRemoteHomeworkListNew(id);
                else
                    __deadlines = await Remote_Course.getRemoteHomeworkList(id);
                _deadlines = _deadlines.Concat(__deadlines).ToList();
            }


            deadlines = _deadlines;
            await ClassLibrary.WriteCache(DEADLINES_FILENAME, ClassLibrary.JSON.stringify(deadlines));

            Debug.WriteLine("[getAllDeadlines] Returning remote");

            return _deadlines;
        }
        static public async Task<List<Announce>> GetAllAnnounce(bool forceRemote = false)
        {
            if (DataAccess_TOP.IsDemo())
            {
                var list = new List<Announce>();
                var start = DateTime.Now.AddDays(-20);
                while (start.DayOfWeek != DayOfWeek.Monday)
                    start = start.AddDays(-1);

                for (var i = 0; i <= 3; i++)
                {
                    list.Add(new Announce
                    {
                        owner = "Trump",
                        regDate = start.AddDays(i * 7 + 4 + 7).ToString("yyyy-MM-dd"),
                        title = $"代码阅读公告{i + 1}",
                        id = "operating_systems_" + i.ToString(),
                        courseId = "demo_course_1",
                        detail = $"一致通过{20 * i + 1}",
                        Isfinished = false,
                        course = "操作系统"


                    });
                }

                for (var i = 0; i <= 3; i++)
                {
                    list.Add(new Announce
                    {
                        owner = "邓俊辉",
                        regDate = start.AddDays(i * 7 + 3 + 7).ToString("yyyy-MM-dd"),
                        title = $"数据结构公告{i + 1}",
                        id = "data_structure_" + i.ToString(),
                        courseId = "demo_course_0",
                        detail = $"决定了{20 * i + 1}",
                        Isfinished = false,
                        course = "数据结构"
                    });
                }

                return list;
            }
            if (!forceRemote)
            {
                //try session memory
                if (announces != null)
                {
                    Debug.WriteLine("[getAllAnnounce] Returning memory");
                    return announces;
                }


                //try local
                var cache = await ClassLibrary.ReadCache(ANNOUNCE_FILENAME);
                if (cache.Length > 0)
                {
                    Debug.WriteLine("[getAllAnnounce] Returning local");
                    return ClassLibrary.JSON.parse<List<Announce>>(cache);
                }
            }

            //fetch from remote

            List<Announce> _announces = new List<Announce>();

            foreach (var course in await GetCourses(forceRemote))
            {
                Debug.WriteLine("[getAllAnnounces] Remote " + course.name);
                var id = course.id;
                List<Announce> __announces;
                if (course.isNew)
                    __announces = await Remote_Course.getRemoteAncListNew(id);
                else
                    __announces = await Remote_Course.getRemoteAncList(id);
                _announces = _announces.Concat(__announces).ToList();
            }


            announces = _announces;
            await ClassLibrary.WriteCache(ANNOUNCE_FILENAME, ClassLibrary.JSON.stringify(announces));

            Debug.WriteLine("[getAllAnnounces] Returning remote");

            return _announces;
        }

    }
}
