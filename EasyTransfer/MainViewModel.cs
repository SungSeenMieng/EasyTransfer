using EasyTransfer.Common;
using EasyTransfer.Component;
using EasyTransfer.Core;
using Google.Protobuf;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace EasyTransfer
{
    public class MainViewModel:BindableBase
    {
        public DelegateCommand<PathItem> UploadCommand { get;private set; } 
        public DelegateCommand<PathItem> DownloadCommand { get;private set; }
        public DelegateCommand<Socket> ConnectedCommand { get;private set; }
        public DelegateCommand<string> LocalPathCommand { get;private set; }
        public DelegateCommand<string> RemotePathCommand { get;private set; }   
        public DelegateCommand<ETResponse> ResponseCommand { get;private set; }
        public DelegateCommand<Action> LocalRefreshCommand { get;private set; }
        public DelegateCommand<Action> RemoteRefreshCommand { get;private set; }    
        public DelegateCommand<string> RequestConnectCommand { get;private set; }   
        public DelegateCommand<bool?> ConnectCommand {  get;private set; }
        public MainViewModel()
        {
            UploadCommand = new DelegateCommand<PathItem>(Upload);
            DownloadCommand = new DelegateCommand<PathItem>(Download);
            ConnectedCommand = new DelegateCommand<Socket>(Connected);
            LocalPathCommand = new DelegateCommand<string>(LocalPath);
            RemotePathCommand = new DelegateCommand<string>(RemotePath);
            ResponseCommand = new DelegateCommand<ETResponse>(Response);
            LocalRefreshCommand = new DelegateCommand<Action>(LocalRefresh);
            RemoteRefreshCommand = new DelegateCommand<Action>(RemoteRefresh);

            RequestConnectCommand = new DelegateCommand<string>(RequestConnect);
            ConnectCommand = new DelegateCommand<bool?>(ConnectResult);
        }
        private bool _isConnecting;
        public bool IsConnecting
        {
            get { return _isConnecting; } set
            {
                _isConnecting = value;
                RaisePropertyChanged(nameof(IsConnecting));
            }
        }
        private string _code;
        public string Code
        {
            get
            {
                return _code;
            }
            set
            {
                _code = value;
                RaisePropertyChanged(nameof(Code));
            }
        }
        private void ConnectResult(bool? obj)
        {
            IsConnecting = false;
            Code = string.Empty;
            if (!obj.HasValue||!obj.Value)
            {
                MessageBox.Show("连接被拒绝");
            }
        }

        private void RequestConnect(string obj)
        {
            Code = obj;
            IsConnecting = true;
        }

        private Action _localRefresh;
        private Action _remoteRefresh;
        private void LocalRefresh(Action obj)
        {
            _localRefresh = obj;
        }

        private void RemoteRefresh(Action obj)
        {
          _remoteRefresh = obj;
        }

        private Dictionary<string,MissionItem> missions = new Dictionary<string,MissionItem>();
        private FileStream _fs;
        private void Response(ETResponse obj)
        {
        switch(obj.ResponseCase)
            {

                case ETResponse.ResponseOneofCase.Missions:
                    Missions.Clear();
                    missions.Clear();
                    foreach(var path in obj.Missions.Files)
                    {
                        var item = new MissionItem(path.FileName, path.FileSize);
                        missions.Add(path.FileName, item);
                        Missions.Add(item);
                    }
                    break;
                case ETResponse.ResponseOneofCase.Download:
                    string filePath = Path.Combine(_localPath, obj.Download.FileName.Replace(_remotePath,"").TrimStart('\\'));
                    FileInfo fileInfo = new FileInfo(filePath);
                    if (!fileInfo.Directory.Exists)
                    {
                        Directory.CreateDirectory(fileInfo.Directory.FullName);
                    }
                    _fs = File.Create(filePath);
                    break;
                case ETResponse.ResponseOneofCase.DownloadData:
                    if (_fs != null)
                    {
                        byte[] data = obj.DownloadData.Data.ToByteArray();
                        _fs.Position = obj.DownloadData.WriteSize - data.Length;
                        _fs.Write(data, 0, obj.DownloadData.Data.Length);
                        if(missions.ContainsKey(obj.DownloadData.FileName))
                        {
                            missions[obj.DownloadData.FileName].Value = obj.DownloadData.WriteSize;
                        }
                    }
                    break;
                case ETResponse.ResponseOneofCase.DownloadDone:
                    _fs.Flush();
                    _fs.Close();
                    _fs = null;
                    if (missions.ContainsKey(obj.DownloadDone.FileName))
                    {
                       missions.Remove(obj.DownloadDone.FileName);
                    }
                    Application.Current.Dispatcher.Invoke(() => { 
                    _localRefresh?.Invoke();
                    });
                    break;
            }
        }

        private string _remotePath;
        public string _localPath;
        private void RemotePath(string obj)
        {
            _remotePath = obj;
        }

        private void LocalPath(string obj)
        {
            _localPath = obj;
        }

        private Socket _socket;
        private void Connected(Socket obj)
        {
            _socket = obj;
        }
        public ObservableCollection<MissionItem> Missions { get; private set; } = new ObservableCollection<MissionItem>();
        private void Upload(PathItem obj)
        {
            if(_socket==null)
            {
                return;
            }
            Missions.Clear();
            Task.Run(() => {
                List<string> paths = new List<string>();
                Dictionary<string,MissionItem> missions = new Dictionary<string,MissionItem>(); 
                switch (obj.Type)
                {
                    case PathItemType.File:
                        paths.Add(obj.FullPath);
                        break;
                    case PathItemType.Folder:
                        string[] files = Directory.GetFiles(obj.FullPath, "*", SearchOption.AllDirectories);
                        paths.AddRange(files);
                        break;
                }
                foreach (string path in paths)
                {
                    FileInfo info = new FileInfo(path);
                    MissionItem mission = new MissionItem(path, info.Length);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Missions.Add(mission);
                    });
                    missions.Add(path,mission);
                }
                foreach (var item in missions)
                {
                    string path = item.Key;
                    MissionItem mission = item.Value;
                    FileInfo info = new FileInfo(path);
                    ETRequest request = new ETRequest();
                    request.Upload = new UploadRequest();
                    request.Upload.Path = _remotePath;
                    request.Upload.FileName = info.FullName.Replace(_localPath, "");
                    request.Upload.FileSize = info.Length;
                    byte[] buff = new byte[request.CalculateSize()];
                    MsgHead head = new MsgHead() { 
                        Size = buff.Length,
                    };
                    using (CodedOutputStream stream = new CodedOutputStream(buff))
                    {
                        request.WriteTo(stream);
                    }
                    _socket.Send(MsgHead.Convert(head));
                    _socket.Send(buff);
                    int buffSize = 4096;
                    long writeSize = 0;
                    int index = 0;
                    using (FileStream fs = new FileStream(path, FileMode.Open))
                    {
                        while (writeSize < info.Length)
                        {
                            buff = new byte[buffSize];
                            fs.Position = writeSize;
                            int readed = fs.Read(buff, 0, buffSize);
                            writeSize += readed;
                            request = new ETRequest();
                            request.UploadData = new UploadDataRequest();
                            request.UploadData.Path = _remotePath;
                            request.UploadData.FileName = info.FullName.Replace(_localPath, "");
                            request.UploadData.FileSize = info.Length;
                            request.UploadData.WriteSize = writeSize;

                            if(buff.Length>readed)
                            {
                                byte[] temp = new byte[readed];
                                for(int i=0; i<readed; i++)
                                {
                                    temp[i] = buff[i];
                                }
                                buff = new byte[readed];
                                temp.CopyTo(buff, 0);
                            }

                            request.UploadData.Data = ByteString.CopyFrom(buff);
                            request.UploadData.Index = index;
                            buff = new byte[request.CalculateSize()];
                            head = new MsgHead()
                            {
                                Size = buff.Length,
                            };
                            using (CodedOutputStream stream = new CodedOutputStream(buff))
                            {
                                request.WriteTo(stream);
                            }
                            byte[] headBytes =MsgHead.Convert(head);
                            _socket.Send(headBytes);
                            _socket.Send(buff);
                            index++;
                            mission.Value = writeSize;
                        }
                    }
                    request = new ETRequest();
                    request.UploadDone = new UploadDoneRequest();
                    request.UploadDone.Path = _remotePath;
                    request.UploadDone.FileName = info.FullName.Replace(_localPath, "");
                    request.UploadDone.FileSize = info.Length;
                    buff = new byte[request.CalculateSize()];
                    head = new MsgHead()
                    {
                        Size = buff.Length,
                    };
                    using (CodedOutputStream stream = new CodedOutputStream(buff))
                    {
                        request.WriteTo(stream);
                    }
                    _socket.Send(MsgHead.Convert(head));
                    _socket.Send(buff);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _remoteRefresh?.Invoke();
                    });
                }
            });
        }
        public string ServiceInfo
        {
            get
            {
                return $"Service Running on Port: {App.Service.Port}";
            }
        }
        private void Download(PathItem obj)
        {
            if (_socket == null)
            {
                return;
            }
            ETRequest request = new ETRequest();
            request.Download = new DownloadRequest();
            request.Download.Path = obj.FullPath;
            byte [] buff = new byte[request.CalculateSize()];
            using(CodedOutputStream stream = new CodedOutputStream(buff))
            {
                request.WriteTo(stream);
            }
            MsgHead head = new MsgHead() { 
                Size = buff.Length,
            };
            _socket.Send(MsgHead.Convert(head));
            _socket.Send(buff);
        }
    }
}
