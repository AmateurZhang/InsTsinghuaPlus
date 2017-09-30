using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace InsTsinghuaPlus.SecurityLevel
{
   
        //Password
        public class Password
        {
            public string username;
            public string password;
            public string usernumber;
        }

        //Sememster
        public class Semesters
        {
            public Semester currentSemester { get; set; }
            public Semester nextSemester { get; set; }
        }

        public class Semester
        {
            public string id { get; set; }
            public string semesterName { get; set; }
            public string startDate { get; set; }
            public string endDate { get; set; }
            public string semesterEname { get; set; }

            public string getWeekName()
            {
                var semesterStart = DateTime.Parse(startDate);
                var delta = DateTime.Now - semesterStart;
                var days = delta.TotalDays;

                /* 0 ~  6.9 -> 1
                   7 ~ 13.9 -> 2 */
                return (Math.Floor(days / 7) + 1).ToString();
            }
        }

        //Json 数据结构

        public class SemestersRootObject
    {
        public Currentteachingweek currentTeachingWeek { get; set; }
        public Semester currentSemester { get; set; }
        public string currentDate { get; set; }
        public Semester nextSemester { get; set; }
    }

        public class Currentteachingweek
    {
        public int teachingWeekId { get; set; }
        public string weekName { get; set; }
        public string beginDate { get; set; }
        public string endDate { get; set; }
        public string semesterId { get; set; }
    }


}
