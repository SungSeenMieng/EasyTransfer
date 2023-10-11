using EasyTransfer.Core;
using Google.Protobuf;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EasyTransfer.Component
{
    public enum PathItemType
    {
        File,
        Folder,
        Driver,
    }
    public class PathItem : BindableBase
    {
        public string FullPath
        {
            get; private set;
        }
        public PathItemType Type
        {
            get; private set;
        }
        public string DisplayName
        {
            get; private set;
        }
        public string CreateTime
        {
            get; private set;
        }
        public string ModifyTime
        {
            get; private set;
        }
        public string Size
        {
            get; private set;
        }
        public PathItem(PathItemType type, string displayName, string fullPath, string createTime, string modifyTime, string size)
        {
            this.Type = type;
            this.DisplayName = displayName;
            this.FullPath = fullPath;
            this.CreateTime = createTime;
            this.ModifyTime = modifyTime;
            this.Size = size;
            RaisePropertyChanged(nameof(Type));
            RaisePropertyChanged(nameof(DisplayName));
            RaisePropertyChanged(nameof(CreateTime));
            RaisePropertyChanged(nameof(ModifyTime));
            RaisePropertyChanged(nameof(Size));
        }
        public PathItem(DriveInfo driver)
        {
            this.Type = PathItemType.Driver;
            RaisePropertyChanged(nameof(Type));
            Task.Run(() =>
            {
                switch (driver.DriveType)
                {
                    case DriveType.Fixed:
                    case DriveType.Removable:
                    case DriveType.CDRom:
                    case DriveType.Network:
                        bool isSuccess = false;
                        int i = 0;
                        while (!isSuccess)
                        {
                            try
                            {
                                this.DisplayName = driver.Name;
                                this.FullPath = driver.Name;
                                isSuccess = true;
                            }
                            catch
                            {
                                i++;
                                Thread.Sleep(200);
                            }
                            if (i > 100)
                            {
                                break;
                            }
                        }
                        RaisePropertyChanged(nameof(DisplayName));
                        break;
                    default:
                        break;
                }
            });
        }
        public PathItem(string path)
        {
            if (File.Exists(path))
            {
                this.Type = PathItemType.File;
                FileInfo info = new FileInfo(path);
                this.DisplayName = info.Name;
                this.FullPath = info.FullName;
                this.CreateTime = info.CreationTime.ToString("yyyy/MM/dd HH:mm:ss");
                this.ModifyTime = info.LastWriteTime.ToString("yyyy/MM/dd HH:mm:ss");
                this.Size = info.Length.TransformToSize();
            }
            else if (Directory.Exists(path))
            {
                this.Type = PathItemType.Folder;
                DirectoryInfo info = new DirectoryInfo(path);
                this.DisplayName = info.Name;
                this.FullPath = info.FullName;
                this.CreateTime = info.CreationTime.ToString("yyyy/MM/dd HH:mm:ss");
                this.ModifyTime = info.LastWriteTime.ToString("yyyy/MM/dd HH:mm:ss");

            }
            RaisePropertyChanged(nameof(Type));
            RaisePropertyChanged(nameof(DisplayName));
            RaisePropertyChanged(nameof(CreateTime));
            RaisePropertyChanged(nameof(ModifyTime));
            RaisePropertyChanged(nameof(Size));
        }
        public PathItem(DirectoryInfo info)
        {
            this.DisplayName = "..";
            this.Type = PathItemType.Folder;
            if (info.Parent != null)
            {
                this.FullPath = info.Parent.FullName;
            }
            RaisePropertyChanged(nameof(DisplayName));
        }

    }
    /// <summary>
    /// PathBrowser.xaml 的交互逻辑
    /// </summary>
    public partial class PathBrowser : UserControl
    {
        public PathBrowser()
        {
            InitializeComponent();
            CurrentPath = String.Empty;
        }

        public bool IsConnected
        {
            get { return (bool)GetValue(IsConnectedProperty); }
            set { SetValue(IsConnectedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsConnected.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsConnectedProperty =
            DependencyProperty.Register("IsConnected", typeof(bool), typeof(PathBrowser), new PropertyMetadata(false));

        //public bool IsRemote
        //{
        //    get { return (bool)GetValue(IsRemoteProperty); }
        //    set { SetValue(IsRemoteProperty, value);
        //        Refresh();
        //    }
        //}

        //// Using a DependencyProperty as the backing store for IsRemote.  This enables animation, styling, binding, etc...
        //public static readonly DependencyProperty IsRemoteProperty =
        //    DependencyProperty.Register("IsRemote", typeof(bool), typeof(PathBrowser), new PropertyMetadata(false));
        private bool _isRemote;
        public bool IsRemote
        {
            get
            {
                return _isRemote;
            }
            set
            {
                _isRemote = value;
                Refresh();
            }
        }
        public string RemoteAddress
        {
            get { return (string)GetValue(RemoteAddressProperty); }
            set { SetValue(RemoteAddressProperty, value); }
        }

        // Using a DependencyProperty as the backing store for RemoteAddress.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RemoteAddressProperty =
            DependencyProperty.Register("RemoteAddress", typeof(string), typeof(PathBrowser), new PropertyMetadata(string.Empty));




        public DelegateCommand<PathItem> Transfer
        {
            get { return (DelegateCommand<PathItem>)GetValue(TransferProperty); }
            set { SetValue(TransferProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Transfer.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TransferProperty =
            DependencyProperty.Register("Transfer", typeof(DelegateCommand<PathItem>), typeof(PathBrowser), new PropertyMetadata(null));


        public DelegateCommand<Socket> Connected
        {
            get { return (DelegateCommand<Socket>)GetValue(ConnectedProperty); }
            set { SetValue(ConnectedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Connected.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ConnectedProperty =
            DependencyProperty.Register("Connected", typeof(DelegateCommand<Socket>), typeof(PathBrowser), new PropertyMetadata(null));


        public DelegateCommand<string> PathChanged
        {
            get { return (DelegateCommand<string>)GetValue(PathChangedProperty); }
            set { SetValue(PathChangedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PathChanged.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PathChangedProperty =
            DependencyProperty.Register("PathChanged", typeof(DelegateCommand<string>), typeof(PathBrowser), new PropertyMetadata(null));



        public DelegateCommand<Action> RefreshAction
        {
            get { return (DelegateCommand<Action>)GetValue(RefreshActionProperty); }
            set { SetValue(RefreshActionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for RefreshAction.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RefreshActionProperty =
            DependencyProperty.Register("RefreshAction", typeof(DelegateCommand<Action>), typeof(PathBrowser), new PropertyMetadata(null, new PropertyChangedCallback(SetAction)));

        private static void SetAction(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PathBrowser control = (PathBrowser)d;
            if (control != null)
            {
                control.RefreshAction?.Execute(control.Refresh);
            }
        }
        private string GenerateCode()
        {
            Guid guid = Guid.NewGuid();
            return guid.ToString().Substring(9, 4);
        }
        private void Connect()
        {
            try
            {
                RemoteAddress = tb_Address.Text;
                string remoteIP = RemoteAddress.Split(':')[0];
                int remotePort = int.Parse(RemoteAddress.Split(':')[1]);
                //string path = RemoteAddress.Split('@')[1];
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(IPAddress.Parse(remoteIP), remotePort);

                string code = GenerateCode();
                RequestConnectCommand?.Execute(code);
                ETRequest request = new ETRequest();
                request.Connect = new ConnectRequest();
                request.Connect.Code = code;
                byte[] buff = new byte[request.CalculateSize()];
                using(CodedOutputStream output = new CodedOutputStream(buff))
                {
                    request.WriteTo(output);
                }
                MsgHead head = new MsgHead { 
                Size = buff.Length,
                };
                socket.Send(MsgHead.Convert(head));
                socket.Send(buff);
                Task.Run(() =>
                {
                    int headSize = Marshal.SizeOf(head);
                    buff = new byte[headSize];
                    socket.ReceiveData(headSize, buff);
                    head = MsgHead.Convert(buff);
                    buff = new byte[head.Size];
                    socket.ReceiveData(head.Size, buff);
                    ETResponse response = ETResponse.Parser.ParseFrom(buff);
                    if (response.ResponseCase == ETResponse.ResponseOneofCase.Connect)
                    {
                        if (response.Connect.Accept)
                        {
                            if (response.Connect.OS == OS.Linux)
                            {
                                this.Dispatcher.Invoke(() =>
                                {
                                    tb_Path.Text = "/";
                                    CurrentPath = tb_Path.Text;
                                });
                            }

                        }
                        this.Dispatcher.Invoke(() =>
                        {
                            this.ConnectCommand?.Execute(response.Connect.Accept);
                        });
                        if (!response.Connect.Accept)
                        {
                            throw new Exception();
                        }
                    }
                    else
                    {
                        throw new Exception();
                    }
                    Thread thread = new Thread(new ParameterizedThreadStart(ConnectionHandler));
                    thread.Start(socket);
                
                    this.Dispatcher.Invoke(() =>
                    {
                        IsConnected = true;
                        btn_Go.IsEnabled = true;
                        tb_Path.IsEnabled = true;
                        Refresh();
                        Connected?.Execute(socket);
                    });
                });
            }
            catch
            {

            }
        }


        private void Refresh()
        {
            CurrentPath = tb_Path.Text;
            if (IsRemote)
            {
                if (IsConnected)
                {
                    PathChanged?.Execute(CurrentPath);
                    ETRequest request = new ETRequest();
                    request.Browse = new BrowseRequest();
                    request.Browse.Path = CurrentPath;
                    byte[] buff = new byte[request.CalculateSize()];
                    using (CodedOutputStream stream = new CodedOutputStream(buff))
                    {
                        request.WriteTo(stream);
                    }
                    MsgHead head = new MsgHead()
                    {
                        Size = buff.Length,
                    };
                    socket.Send(MsgHead.Convert(head));
                    socket.Send(buff);
                    return;
                }
                else
                {
                    btn_Go.IsEnabled = false;
                    tb_Path.IsEnabled = false;
                }
                return;
            }
            List<PathItem> list = new List<PathItem>();
            PathChanged?.Execute(CurrentPath);
            if (string.IsNullOrEmpty(CurrentPath))
            {
                DriveInfo[] infos = DriveInfo.GetDrives();
                foreach (DriveInfo info in infos)
                {
                    list.Add(new PathItem(info));
                }
            }
            else if (Directory.Exists(CurrentPath))
            {
                list.Add(new PathItem(new DirectoryInfo(CurrentPath)));
                foreach (string folder in Directory.GetDirectories(CurrentPath))
                {
                    list.Add(new PathItem(folder));
                }
                foreach (string file in Directory.GetFiles(CurrentPath))
                {
                    list.Add(new PathItem(file));
                }
            }
            lv_Items.ItemsSource = list;
        }
        private Socket socket;
        public string CurrentPath
        {
            get { return (string)GetValue(CurrentPathProperty); }
            set { SetValue(CurrentPathProperty, value); }
        }


        public DelegateCommand<ETResponse> Response
        {
            get { return (DelegateCommand<ETResponse>)GetValue(ResponseProperty); }
            set { SetValue(ResponseProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Response.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ResponseProperty =
            DependencyProperty.Register("Response", typeof(DelegateCommand<ETResponse>), typeof(PathBrowser), new PropertyMetadata(null));



        public DelegateCommand<string> RequestConnectCommand
        {
            get { return (DelegateCommand<string>)GetValue(RequestConnectCommandProperty); }
            set { SetValue(RequestConnectCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for RequestConnectCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RequestConnectCommandProperty =
            DependencyProperty.Register("RequestConnectCommand", typeof(DelegateCommand<string>), typeof(PathBrowser), new PropertyMetadata(null));



        public DelegateCommand<bool?> ConnectCommand
        {
            get { return (DelegateCommand<bool?>)GetValue(ConnectCommandProperty); }
            set { SetValue(ConnectCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ConnectCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ConnectCommandProperty =
            DependencyProperty.Register("ConnectCommand", typeof(DelegateCommand<bool?>), typeof(PathBrowser), new PropertyMetadata(null));



        private void ConnectionHandler(object obj)
        {
            Socket socket = obj as Socket;
            int headSize = Marshal.SizeOf(typeof(MsgHead));
            if (socket != null)
            {
                while (true)
                {
                    try
                    {
                        byte[] buff = new byte[headSize];
                        socket.ReceiveData(headSize, buff);
                        MsgHead head = MsgHead.Convert(buff);
                        int dataLength = head.Size;
                        buff = new byte[dataLength];
                        socket.ReceiveData(dataLength, buff);

                        ETResponse response = ETResponse.Parser.ParseFrom(buff);
                        switch (response.ResponseCase)
                        {
                            case ETResponse.ResponseOneofCase.Browse:
                                List<PathItem> list = new List<PathItem>();
                                foreach (var pathItem in response.Browse.PathItems)
                                {
                                    PathItem item = new PathItem((PathItemType)(int)(pathItem.Type), pathItem.DisplayName, pathItem.FullPath, pathItem.CreateTime, pathItem.ModifyTime, pathItem.Size);
                                    list.Add(item);
                                }
                                this.Dispatcher.Invoke(() =>
                                {
                                    lv_Items.ItemsSource = list;
                                });
                                break;
                            case ETResponse.ResponseOneofCase.Delete:
                                this.Dispatcher.Invoke(() => { 
                                    if(!response.Delete.Success)
                                    {
                                        MessageBox.Show(response.Delete.Message);
                                    }
                                    Refresh();
                                });
                                break;
                            default:
                                this.Dispatcher.Invoke(() =>
                                {
                                    Response?.Execute(response);
                                });
                                break;
                        }
                    }
                    catch
                    {
                        try
                        {
                            socket.Shutdown(SocketShutdown.Both);
                            socket.Close();
                            socket = null;
                        }
                        catch
                        {

                        }
                        this.Dispatcher.Invoke(() =>
                        {
                            IsConnected = false;
                            lv_Items.ItemsSource = null;
                            tb_Path.Text = String.Empty;
                            CurrentPath = String.Empty;
                            btn_Go.IsEnabled = false;
                            tb_Path.IsEnabled = false;
                        });
                        break;
                    }
                }
            }
        }

        // Using a DependencyProperty as the backing store for CurrentPath.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CurrentPathProperty =
            DependencyProperty.Register("CurrentPath", typeof(string), typeof(PathBrowser), new PropertyMetadata(string.Empty, new PropertyChangedCallback(CurrentPathChanged)));

        private static void CurrentPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

        }

        public string DisplayPath
        {
            get { return (string)GetValue(DisplayPathProperty); }
            set { SetValue(DisplayPathProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DisplayPath.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DisplayPathProperty =
            DependencyProperty.Register("DisplayPath", typeof(string), typeof(PathBrowser), new PropertyMetadata(string.Empty));



        private void btn_Go_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }
        private void lvItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListViewItem lvItem)
            {
                PathItem item = lvItem.DataContext as PathItem;
                if (item != null)
                {
                    switch (item.Type)
                    {
                        case PathItemType.Driver:
                            tb_Path.Text = item.FullPath;
                            Refresh();
                            break;
                        case PathItemType.Folder:
                            tb_Path.Text = item.FullPath;
                            Refresh();
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private void btn_Connect_Click(object sender, RoutedEventArgs e)
        {
            if (!IsConnected)
            {
                Connect();
            }
            else if (socket != null)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                socket = null;
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var item = lv_Items.SelectedItem;
            if (item != null)
            {
                Transfer?.Execute(item as PathItem);
            }
        }

        private void Delete_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var item = lv_Items.SelectedItem;
            if (item != null && item is PathItem pathItem)
            {
                if (IsRemote)
                {
                    if (IsConnected)
                    {
                        if (MessageBox.Show($"确定删除远程路径{pathItem.FullPath}吗?", "远程删除", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {

                            ETRequest request = new ETRequest();
                            request.Delete = new DeleteRequest();
                            request.Delete.Path = pathItem.FullPath;
                            byte[] buff = new byte[request.CalculateSize()];
                            MsgHead head = new MsgHead()
                            {
                                Size = buff.Length
                            };
                            using(CodedOutputStream output = new CodedOutputStream(buff))
                            {
                                request.WriteTo(output);
                            }
                            socket.Send(MsgHead.Convert(head));
                            socket.Send(buff);
                        }
                    }
                }
                else
                {
                    if (MessageBox.Show($"确定删除本地路径{pathItem.FullPath}吗?", "本地删除", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        if (Directory.Exists(pathItem.FullPath))
                        {
                            try
                            {
                                Directory.Delete(pathItem.FullPath, true);
                            }
                            catch(Exception ex)
                            {
                                MessageBox.Show(ex.Message);
                            }
                            finally
                            {
                                Refresh();
                            }
                        }
                        if (File.Exists(pathItem.FullPath))
                        {
                            try
                            {
                                File.Delete(pathItem.FullPath);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message);
                            }
                            finally
                            {
                                Refresh();
                            }
                        }
                    }
                }
            }
        }
    }
}
