using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace EasyTransfer
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            AppDomain.CurrentDomain.AssemblyResolve += OnResolveAssembly;
        }
        public static EasyTransfer.Core.ETService Service;
        private static Assembly OnResolveAssembly(object sender, ResolveEventArgs args)
        {
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            var executingAssemblyName = executingAssembly.GetName();
            var resName = executingAssemblyName.Name + ".resources";

            AssemblyName assemblyName = new AssemblyName(args.Name); string path = "";
            if (resName == assemblyName.Name)
            {
                path = executingAssemblyName.Name + ".g.resources"; ;
            }
            else
            {
                path = assemblyName.Name + ".dll";
                if (assemblyName.CultureInfo.Equals(CultureInfo.InvariantCulture) == false)
                {
                    path = String.Format(@"{0}\{1}", assemblyName.CultureInfo, path);
                }
            }

            using (Stream stream = executingAssembly.GetManifestResourceStream(path))
            {
                if (stream == null)
                    return null;

                byte[] assemblyRawBytes = new byte[stream.Length];
                stream.Read(assemblyRawBytes, 0, assemblyRawBytes.Length);
                return Assembly.Load(assemblyRawBytes);
            }
        }
        protected override void OnStartup(StartupEventArgs e)
        {
      
            Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            int port = 0;
            if (e.Args.Length > 0)
            {
                int.TryParse(e.Args[0], out port);
            }
            Service = new EasyTransfer.Core.ETService(port);
            Service.OnConnectRequesting += Service_OnConnectRequesting;
            Service.Start();
            base.OnStartup(e);
        }

        private bool Service_OnConnectRequesting(string code)
        {
           if(MessageBox.Show($"连接请求,Code：\r\n{code}\r\n是否允许连接","",MessageBoxButton.YesNo)==MessageBoxResult.Yes)
            {
                return true;
            }
            return false;
        }

        protected override void OnExit(ExitEventArgs e)
        {
           Service.Stop();
            Environment.Exit(0);
            //base.OnExit(e);
        }
    }
}
