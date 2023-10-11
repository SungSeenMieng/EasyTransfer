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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace EasyTransfer.Core
{
    public struct MsgHead
    {
       
        public int Size;
        public static MsgHead Convert(byte[] arr)
        {
            Int32 size = Marshal.SizeOf(typeof(MsgHead));
            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(arr, 0, ptr, size);
                return Marshal.PtrToStructure<MsgHead>(ptr);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
        public static byte[] Convert(MsgHead header)
        {
            Int32 size = Marshal.SizeOf(header);
            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(header, ptr, false);
                byte[] arr = new byte[size];
                Marshal.Copy(ptr, arr, 0, size);
                return arr;
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
    }
}
