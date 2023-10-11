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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EasyTransfer.Core
{
    public static class ExtensionHelper
    {
        public static Socket ReceiveData(this Socket socket, int length, byte[] result)
        {
            int current = 0;
            int buffSize = 4096;
            if (buffSize > length)
            {
                buffSize = length;
            }
            byte[] buffer = new byte[buffSize];
            while (current < length)
            {
                if(length-current<buffSize)
                {
                    buffer = new  byte[length-current];
                }
                int recLength = socket.Receive(buffer);

                for (int i = 0; i < recLength; i++)
                {
                    if (current + i == length)
                    {
                        break;
                    }
                    result[current + i] = buffer[i];
                }
                current += recLength;
            }
            return socket;
        }
        public static string TransformToSize(this long size)
        {
            if (size <= 1024)
            {
                return string.Format(size.ToString() + 'B');
            }
            if (size > 1024 && size <= Math.Pow(1024, 2))
            {
                return string.Format((size / 1024.0).ToString("0.00") + "KB");
            }
            if (size > Math.Pow(1024, 2) && size <= Math.Pow(1024, 3))
            {
                return string.Format((size / 1024.0 / 1024.0).ToString("0.00") + "MB");
            }

            return string.Format((size / 1024.0 / 1024.0 / 1024.0).ToString("0.00") + "GB");
        }
    }
}
