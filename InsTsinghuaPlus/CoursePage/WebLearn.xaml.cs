﻿using InsTsinghuaPlus.SecurityLevel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace InsTsinghuaPlus.CoursePage
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class WebLearn : Page
    {
        public List<Course> listcourses;
        private ObservableCollection<Deadline> ddl1;
        private ObservableCollection<Course> courses1;
        private ObservableCollection<Announce> announces;
        private List<Announce> listanc;
        private List<Deadline> listddl;
        public WebLearn()
        {
            this.InitializeComponent();
            courses1 = new ObservableCollection<Course>();
            ddl1 = new ObservableCollection<Deadline>();
            announces = new ObservableCollection<Announce>();


        }

        private async void Oninitial(object sender, RoutedEventArgs e)
        {

            if (!DataAccess_TOP.CredentialAbsent())
            {
                try
                {

                    listcourses = await DataAccess_Course.GetCourses();//temp
                    courses1.Clear();
                    listcourses.ForEach(p => courses1.Add(p));
                }
                catch
                {
                    Debug.WriteLine("[Oninitial]获得本地数据失败。");
                }
                try
                {
                    await Remote_TOP.LoginToLearn();
                }
                catch
                {
                    Debug.WriteLine("[Oninitial]无法连接到学校服务器，检查网络。");
                }
                Coursebuttons.IsEnabled = true;
            }
            else
            {
                Coursebuttons.IsEnabled = false;
                MessageDialog msg = new MessageDialog("请您先登录，谢谢");
                await msg.ShowAsync();

            }

        }

        private async void Coursebuttons_Click(object sender, RoutedEventArgs e)
        {
            PR0.IsActive = true;
            try
            {

                listcourses = await DataAccess_Course.GetCourses(forceRemote: false);//temp
                Debug.WriteLine("[remote refresh]await DataAccess.getCourses(forceRemote: true)");
            }
            catch
            {

                MessageDialog a = new MessageDialog("检查网络连接");
                await a.ShowAsync();
                Debug.WriteLine("[remote refresh]Wrong data local try remote");

            }
            try
            {
                await DataAccess_Course.GetAllDeadlines(forceRemote: true);
                Debug.WriteLine("[remote refresh]await DataAccess.getAllDeadlines(forceRemote: true);");
            }
            catch
            {
                // MessageDialog a = new MessageDialog("获取作业失败，检查网络连接");
                // await a.ShowAsync();
            }
            try
            {
                await DataAccess_Course.GetAllAnnounce(forceRemote: true);
                Debug.WriteLine("[remote refresh] await DataAccess.getAllAnnounce(forceRemote: true);");
            }
            catch
            {
                // MessageDialog a = new MessageDialog("获取公告失败，检查网络连接");
                //  await a.ShowAsync();
            }


            try
            {
                courses1.Clear();
                listcourses.ForEach(p => courses1.Add(p));
            }
            catch
            {
                MessageDialog a = new MessageDialog("Wrong data add to list");
                await a.ShowAsync();
            }


            PR0.IsActive = false;
            try
            {
                GetStart();
                Debug.WriteLine("[磁贴更新] ok");
            }
            catch
            {
                Debug.WriteLine("[磁贴更新] failed");
            }
        }
        private async void CourseGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int tp;
            String coid;
            tp = CourseGrid.SelectedIndex;

            try
            {
                coid = listcourses[tp].id;
                listddl = await DataAccess_Course.GetAllDeadlines();
                listddl.ForEach(p => ddl1.Add(p));
                ddl1.Clear();
                var filteredDDLItems = listddl.Where(p => p.courseid == coid).ToList();
                filteredDDLItems.ForEach(p => ddl1.Add(p));
                if (ddl1.Count == 0)
                    NoData.Visibility = Visibility.Visible;
                else
                    NoData.Visibility = Visibility.Collapsed;

                coid = listcourses[tp].id;
                listanc = await DataAccess_Course.GetAllAnnounce();
                listanc.ForEach(q => announces.Add(q));
                announces.Clear();
                var filteredDDLItems1 = listanc.Where(p => p.courseId == coid).ToList();
                filteredDDLItems1.ForEach(p => announces.Add(p));

                if (announces.Count == 0)
                    NoData1.Visibility = Visibility.Visible;
                else
                    NoData1.Visibility = Visibility.Collapsed;
            }
            catch
            {
                // INFOTB.Visibility = Visibility.Visible;
            }
        }

        private void CoursePivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private async void HWGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int tp;
            tp = HWGrid.SelectedIndex;
            try
            {
                var tp1 = CourseGrid.SelectedIndex;

                string uri = ddl1[tp].detail;

                var dialog1 = new ContentDialogAnc(uri, true);

                await dialog1.ShowAsync();

                HWGrid.SelectedItem = null;

            }
            catch
            {
            }
        }

        private async void AncGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int tp;
            tp = AncGrid.SelectedIndex;
            try
            {
                var tp1 = CourseGrid.SelectedIndex;

                string uri = announces[tp].detail;

                var dialog1 = new ContentDialogAnc(uri, true);

                await dialog1.ShowAsync();

                AncGrid.SelectedItem = null;

            }
            catch
            {
            }
        }

        //磁贴更新
        private async void GetStart()
        {

            if (DataAccess_TOP.SupposedToWorkAnonymously())
            {

                try
                {
                   // await Notification.update(calendarOnly: true);
                    // await Appointment.updateCalendar();
                }
                catch
                {
                }
            }
            else if (!DataAccess_TOP.SupposedToWorkAnonymously()
               && DataAccess_TOP.CredentialAbsent())
            {
                try
                {
                   // await Notification.update(calendarOnly: true);
                    // await Appointment.updateCalendar();
                }
                catch
                {
                }

            }
            else if (!DataAccess_TOP.CredentialAbsent())
            {
                try
                {
                   // await Notification.update(true);
                    // await Appointment.updateDeadlines();
                }
                catch (Exception e)
                {
                    // this.errorUpdate.Visibility = Visibility.Visible;
                    try
                    {
                       // await Notification.update();
                    }
                    catch (Exception) { }
                }
            }


        }
    }
}
