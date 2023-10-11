///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//THIS FILE IS THE THE PROPERTY OF SHANGHAI ELECTRIC (GROUP) CORPORATION.ITS NOT ALLOWED
//TO USE,COPY,DISPLAY,PUBLISH,OR BROADCAST IT IN WHOLE OR IN PART IN ANY MANNER. OR
//TO ANY PERSON.NOR USE,COPY OR DEPLOY IT TO ANY EQUIPMENT WITHOUT THE EXPRESS
//WRITTEN PERMISSION OF SHANGHAI ELECTRIC(GROUP) CORPORATION
//
//Copyright(c) 2021-2022 
//By Shanghai Electric (Group) Corperation,China.All rights reserved.
//
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using Google.Protobuf;
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

namespace EasyTransfer.Core
{
    public class ETService
    {
        public int Port { get; private set; }
        public bool IsRunning { get; private set; }
        private Thread _mainThread;
        private Socket _mainSocket;
        public ETService(int port)
        {
            this.Port = port;
            _mainThread = new Thread(MainRoutin);
        }
        public void Start()
        {
            if (IsRunning)
            {
                return;
            }
            IsRunning = true;
            _mainThread.Start();
        }
        public void Stop()
        {
            if (!IsRunning)
            {
                return;
            }
            IsRunning = false;
            _mainSocket.Close();
        }
        public delegate bool ConnectRequesting(string code);
        public event ConnectRequesting OnConnectRequesting;
        private void MainRoutin()
        {
            _mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _mainSocket.Bind(new IPEndPoint(IPAddress.Parse("0.0.0.0"), Port));
            _mainSocket.Listen(0);
            Port = (_mainSocket.LocalEndPoint as IPEndPoint).Port;
            while (IsRunning)
            {
                ETResponse response = new ETResponse();
                response.Connect = new ConnectResponse();
                try
                {

                    Socket socket = _mainSocket.Accept();
                    int headSize = Marshal.SizeOf(typeof(MsgHead));
                    try
                    {
                        byte[] headBytes = new byte[headSize];
                        socket.ReceiveData(headSize, headBytes);
                        MsgHead head = MsgHead.Convert(headBytes);
                        byte[] data = new byte[head.Size];
                        socket.ReceiveData(head.Size, data);
                        ETRequest request = ETRequest.Parser.ParseFrom(data);


                        if (request.RequestCase == ETRequest.RequestOneofCase.Connect && !string.IsNullOrEmpty(request.Connect.Code) && request.Connect.Code.Length == 4)
                        {
                            bool result = false;
                            if (OnConnectRequesting != null)
                            {
                                result = OnConnectRequesting.Invoke(request.Connect.Code);
                            }
                            response.Connect.Accept = result;
                            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                            {
                                response.Connect.OS = OS.Windows;
                            }
                            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                            {
                                response.Connect.OS = OS.Linux;
                            }
                            byte[] buff = new byte[response.CalculateSize()];
                            head = new MsgHead()
                            {
                                Size = buff.Length
                            };
                            using (CodedOutputStream output = new CodedOutputStream(buff))
                            {
                                response.WriteTo(output);
                            }
                            socket.Send(MsgHead.Convert(head));
                            socket.Send(buff);
                            if (!result)
                            {
                                socket.Shutdown(SocketShutdown.Both);
                                socket.Close();
                                continue;
                            }
                            Console.WriteLine($"Clinet Connect Accepted . Code:{request.Connect.Code}");
                        }
                        else
                        {
                            byte[] buff = new byte[response.CalculateSize()];
                            head = new MsgHead()
                            {
                                Size = buff.Length
                            };
                            using (CodedOutputStream output = new CodedOutputStream(buff))
                            {
                                response.WriteTo(output);
                            }
                            socket.Send(MsgHead.Convert(head));
                            socket.Send(buff);
                            socket.Shutdown(SocketShutdown.Both);
                            socket.Close();
                            continue;
                        }
                    }
                    catch
                    {
                        byte[] buff = new byte[response.CalculateSize()];
                        MsgHead head = new MsgHead()
                        {
                            Size = buff.Length
                        };
                        using (CodedOutputStream output = new CodedOutputStream(buff))
                        {
                            response.WriteTo(output);
                        }
                        socket.Send(MsgHead.Convert(head));
                        socket.Send(buff);
                        socket.Shutdown(SocketShutdown.Both);
                        socket.Close();
                        continue;
                    }
                    sockets.Add(socket);
                    Thread thread = new Thread(new ParameterizedThreadStart(HandleConnection));
                    thread.Start(socket);

                }
                catch
                {
                    break;
                }
            }
        }
        private List<Socket> sockets = new List<Socket>();
        private void HandleConnection(object obj)
        {
            Socket socket = obj as Socket;
            if (socket != null)
            {
                while (true)
                {
                    try
                    {
                        int headLength = Marshal.SizeOf(typeof(MsgHead));
                        byte[] temp = new byte[headLength];
                        socket.ReceiveData(headLength, temp);

                        MsgHead head = MsgHead.Convert(temp);
                        byte[] buff = new byte[head.Size];

                        socket.ReceiveData(head.Size, buff);
                        ETRequest request = ETRequest.Parser.ParseFrom(buff);
                        HandleRequest(socket, request);
                    }
                    catch
                    {
                        socket.Close();
                        sockets.Remove(socket);
                        break;
                    }
                }
            }
        }
        private void HandleRequest(Socket socket, ETRequest request)
        {
            switch (request.RequestCase)
            {
                case ETRequest.RequestOneofCase.Connect:
                    break;
                case ETRequest.RequestOneofCase.Browse:
                    ETResponse response = new ETResponse();
                    response.Browse = new BrowseResponse();
                    if (string.IsNullOrEmpty(request.Browse.Path))
                    {
                        DriveInfo[] infos = DriveInfo.GetDrives();

                        foreach (DriveInfo info in infos)
                        {
                            PathInfo path = new PathInfo()
                            {
                                Type = PathType.Driver
                            };
                            switch (info.DriveType)
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
                                            path.DisplayName = info.Name;
                                            path.FullPath = info.Name;
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
                                    break;
                                default:
                                    break;
                            }
                            response.Browse.PathItems.Add(path);
                        }
                    }
                    else if (Directory.Exists(request.Browse.Path))
                    {
                        string fullPath = new DirectoryInfo(request.Browse.Path).Parent?.FullName;
                        if (fullPath == null)
                        {
                            fullPath = "";
                        }
                        response.Browse.PathItems.Add(new PathInfo()
                        {
                            DisplayName = "..",
                            Type = PathType.Folder,
                            FullPath = fullPath
                        });
                        foreach (string folder in Directory.GetDirectories(request.Browse.Path))
                        {
                            PathInfo path = new PathInfo();
                            path.Type = PathType.Folder;
                            DirectoryInfo info = new DirectoryInfo(folder);
                            path.DisplayName = info.Name;
                            path.FullPath = info.FullName;
                            path.CreateTime = info.CreationTime.ToString("yyyy/MM/dd HH:mm:ss");
                            path.ModifyTime = info.LastWriteTime.ToString("yyyy/MM/dd HH:mm:ss");
                            response.Browse.PathItems.Add(path);
                        }
                        foreach (string file in Directory.GetFiles(request.Browse.Path))
                        {
                            PathInfo path = new PathInfo();
                            path.Type = PathType.File;
                            FileInfo info = new FileInfo(file);
                            path.DisplayName = info.Name;
                            path.FullPath = info.FullName;
                            path.CreateTime = info.CreationTime.ToString("yyyy/MM/dd HH:mm:ss");
                            path.ModifyTime = info.LastWriteTime.ToString("yyyy/MM/dd HH:mm:ss");
                            path.Size = info.Length.TransformToSize();
                            response.Browse.PathItems.Add(path);
                        }
                    }

                    byte[] responseBytes = new byte[response.CalculateSize()];
                    MsgHead responseHead = new MsgHead()
                    {
                        Size = responseBytes.Length,
                    };
                    using (CodedOutputStream stream = new CodedOutputStream(responseBytes))
                    {
                        response.WriteTo(stream);
                    }
                    socket.Send(MsgHead.Convert(responseHead));
                    socket.Send(responseBytes);
                    break;
                case ETRequest.RequestOneofCase.Upload:
                    string filePath = Path.Combine(request.Upload.Path, request.Upload.FileName.TrimStart('\\'));
                    FileInfo fileInfo = new FileInfo(filePath);
                    if (!fileInfo.Directory.Exists)
                    {
                        Directory.CreateDirectory(fileInfo.Directory.FullName);
                    }
                    _fs = File.Create(filePath);
                    break;
                case ETRequest.RequestOneofCase.UploadData:
                    if (_fs != null)
                    {

                        byte[] data = request.UploadData.Data.ToByteArray();
                        _fs.Position = request.UploadData.WriteSize - data.Length;
                        _fs.Write(data, 0, request.UploadData.Data.Length);
                    }
                    break;
                case ETRequest.RequestOneofCase.UploadDone:
                    _fs.Flush();
                    _fs.Close();
                    _fs = null;
                    break;
                case ETRequest.RequestOneofCase.Download:
                    Task.Run(() => {
                        List<string> paths = new List<string>();
                        string dirPath = request.Download.Path;
                        if (Directory.Exists(request.Download.Path))
                        {
                            paths.AddRange(Directory.GetFiles(request.Download.Path, "*", SearchOption.AllDirectories));
                        }
                        if (File.Exists(request.Download.Path))
                        {
                            dirPath = new FileInfo(request.Download.Path).Directory.FullName;
                            paths.Add(request.Download.Path);
                        }
                        response = new ETResponse();
                        response.Missions = new DownloadMissionResponse();

                        foreach (string path in paths)
                        {

                            response.Missions.Files.Add(new DownloadMissionItem()
                            {
                                FileName = path.Replace(dirPath, ""),
                                FileSize = new FileInfo(path).Length,
                            });
                        }
                        byte[] buff = new byte[response.CalculateSize()];
                        using (CodedOutputStream output = new CodedOutputStream(buff))
                        {
                            response.WriteTo(output);
                        }
                        MsgHead head = new MsgHead()
                        {
                            Size = buff.Length
                        };
                        socket.Send(MsgHead.Convert(head));
                        socket.Send(buff);

                        foreach (string path in paths)
                        {
                            FileInfo info = new FileInfo(path);
                            response = new ETResponse();
                            response.Download = new DownloadResponse();
                            response.Download.FileName = path;
                            response.Download.FileSize = info.Length;
                            buff = new byte[response.CalculateSize()];
                            head = new MsgHead()
                            {
                                Size = buff.Length,
                            };
                            using (CodedOutputStream stream = new CodedOutputStream(buff))
                            {
                                response.WriteTo(stream);
                            }
                            socket.Send(MsgHead.Convert(head));
                            socket.Send(buff);
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
                                    response = new ETResponse();
                                    response.DownloadData = new DownloadDataResponse();
                                    response.DownloadData.FileName = info.FullName.Replace(dirPath, "");
                                    response.DownloadData.FileSize = info.Length;
                                    response.DownloadData.WriteSize = writeSize;

                                    if (buff.Length > readed)
                                    {
                                        byte[] temp = new byte[readed];
                                        for (int i = 0; i < readed; i++)
                                        {
                                            temp[i] = buff[i];
                                        }
                                        buff = new byte[readed];
                                        temp.CopyTo(buff, 0);
                                    }

                                    response.DownloadData.Data = ByteString.CopyFrom(buff);
                                    response.DownloadData.Index = index;
                                    buff = new byte[response.CalculateSize()];
                                    head = new MsgHead()
                                    {
                                        Size = buff.Length,
                                    };
                                    using (CodedOutputStream stream = new CodedOutputStream(buff))
                                    {
                                        response.WriteTo(stream);
                                    }
                                    byte[] headBytes = MsgHead.Convert(head);
                                    socket.Send(headBytes);
                                    socket.Send(buff);
                                    index++;
                                }
                            }
                            response = new ETResponse();
                            response.DownloadDone = new DownloadDoneResponse();
                            response.DownloadDone.FileName = info.FullName.Replace(dirPath, "");
                            response.DownloadDone.FileSize = info.Length;
                            buff = new byte[response.CalculateSize()];
                            head = new MsgHead()
                            {
                                Size = buff.Length,
                            };
                            using (CodedOutputStream stream = new CodedOutputStream(buff))
                            {
                                response.WriteTo(stream);
                            }
                            socket.Send(MsgHead.Convert(head));
                            socket.Send(buff);
                        }
                    });
                    break;
                case ETRequest.RequestOneofCase.Delete:
                    response = new ETResponse();
                    response.Delete = new DeleteResponse();
                    response.Delete.Success = true;
                    if (Directory.Exists(request.Delete.Path))
                    {
                        try
                        {
                            Directory.Delete(request.Delete.Path, true);
                        }
                        catch (Exception ex)
                        {
                            response.Delete.Success = false;
                            response.Delete.Message = ex.Message;
                        }
                    }
                    if (File.Exists(request.Delete.Path))
                    {
                        try
                        {
                            File.Delete(request.Delete.Path);
                        }
                        catch (Exception ex)
                        {
                            response.Delete.Success = false;
                            response.Delete.Message = ex.Message;
                        }
                    }
                    byte[] buffer = new byte[response.CalculateSize()];
                    MsgHead header = new MsgHead()
                    {
                        Size = buffer.Length,
                    };
                    using (CodedOutputStream stream = new CodedOutputStream(buffer))
                    {
                        response.WriteTo(stream);
                    }
                    socket.Send(MsgHead.Convert(header));
                    socket.Send(buffer);
                    break;
            }
        }
        private FileStream _fs;
    }
}
