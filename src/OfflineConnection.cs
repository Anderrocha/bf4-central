using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
namespace BF4Bots {public static partial class OneClick {
 public static bool HasBackend(int pid,int port){int size=0;GetExtendedTcpTable(IntPtr.Zero,ref size,false,2,5,0);var memory=Marshal.AllocHGlobal(size);try{if(GetExtendedTcpTable(memory,ref size,false,2,5,0)!=0)throw new IOException("Não foi possível verificar a conexão do jogo.");int count=Marshal.ReadInt32(memory);for(int i=0;i<count;i++){int row=4+i*24;if(Marshal.ReadInt32(memory,row+20)!=pid||Marshal.ReadInt32(memory,row)!=5)continue;uint raw=unchecked((uint)Marshal.ReadInt32(memory,row+16));int remotePort=(int)(((raw&255)<<8)|((raw>>8)&255));byte[] bytes=new byte[4];Marshal.Copy(IntPtr.Add(memory,row+12),bytes,0,4);if(remotePort==port&&new IPAddress(bytes).Equals(IPAddress.Loopback))return true;}return false;}finally{Marshal.FreeHGlobal(memory);}}
 public static void WaitBackend(int pid,int port,int seconds){var timer=Stopwatch.StartNew();int stable=0;while(timer.Elapsed.TotalSeconds<seconds){using(var game=Process.GetProcessById(pid)){if(game.HasExited)throw new IOException("O jogo encerrou antes de conectar ao backend local.");}if(HasBackend(pid,port)){if(++stable>=6)return;}else stable=0;Thread.Sleep(500);}throw new TimeoutException("O jogo abriu, mas não confirmou conexão com o backend offline. Confira a mensagem dentro do BF4; processos abertos não garantem sessão conectada.");}
}}
