using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;


// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace InsTsinghuaPlus.SecurityLevel
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class LoginPage : Page
    {
        public LoginPage()
        {
            this.InitializeComponent();
            if (!DataAccess_TOP.CredentialAbsent())
            {
                btnLogin.Content = "注销登录";
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {

            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (!(localSettings.Values["FirstStart"] == null))
            {
                if (DataAccess_TOP.SupposedToWorkAnonymously())
                {
                    btnLogin.Content = "登录";

                    update_without_credential();
                }
                else if (!DataAccess_TOP.SupposedToWorkAnonymously()
                   && DataAccess_TOP.CredentialAbsent())
                {
                    update_without_credential();
                    await ChangeAccountAsync();
                }
                else if (!DataAccess_TOP.CredentialAbsent())
                {
                    update_with_credential();
                }
                localSettings.Values["FirstStart"] = false;
            }

        }

        private async void update_with_credential()
        {
           
            
        }

        public async void update_without_credential()
        {
            
        }

        private async Task ChangeAccountAsync()
        {
            btnLogin.Content = "登录";

            this.btnLogin.IsEnabled = false;
            
            if (await ChangeAccountHelper())
            {
                this.btnLogin.Content = "注销登录";

                update_with_credential();

                var value = DataAccess_TOP.localSettings.Values["usernumber"];

                var dialog1 = new ContentDialog()
                {
                    Title = "登录提示",
                    Content = $"Welcome,{value}!",
                    PrimaryButtonText = "确定",
                    FullSizeDesired = false,
                };
                dialog1.PrimaryButtonClick += (_s, _e) => { };
                await dialog1.ShowAsync();
            }
            else
            {
                this.btnLogin.Content = "登录";
                update_without_credential();
            }
            this.progressLogin.IsActive = false;
            this.btnLogin.IsEnabled = true;
        }

        private async Task<bool> ChangeAccountHelper() //false for anonymous
        {
            var dialog = new PasswordDialog();
            Password password;
            this.progressLogin.IsActive = true;
            try
            {
                password = await dialog.GetCredentialAsyc();
                this.progressLogin.IsActive = false;
            }
            catch (UserCancelException)
            {
                //user choose to stay anonymous
                DataAccess_TOP.SetLocalSettings("username", "__anonymous");
                return false;
            }

            //save credential
            //TODO: wrap as a function and move into DataAccess
            DataAccess_TOP.SetLocalSettings("toasted_assignments", "");
            DataAccess_TOP.SetLocalSettings("username", password.username);

           

            var vault = new Windows.Security.Credentials.PasswordVault();
            vault.Add(new Windows.Security.Credentials.PasswordCredential(
                "Tsinghua_Learn_Website", password.username, password.password));

            string usernumber = await DataAccess_TOP.GetemailName();
            DataAccess_TOP.SetLocalSettings("usernumber", usernumber);



            return true;
        }




        private async void GetDataFromLearn()
        {
           //need add
        }



        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            await ChangeAccountAsync();
            GetDataFromLearn();
        }
    }
}
