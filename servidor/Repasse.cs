using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
// Repasse do RCON: o servidor do BF4 só aceita o controle (porta 25210) de dentro do próprio PC.
// O painel escuta na porta 25212 da rede e repassa os bytes para 127.0.0.1:25210 (senha e comandos
// passam intactos). Assim a Central do PC de jogo troca mapa, vê jogadores etc.
namespace BF4Servidor {
public sealed class Repasse : IDisposable {
 public const int Port=25212;
 readonly Func<int> target;TcpListener listener;volatile bool closed;
 public Repasse(Func<int> target){this.target=target;}
 public void Start(){listener=new TcpListener(IPAddress.Any,Port);listener.Start();new Thread(Loop){IsBackground=true,Name="repasse-rcon"}.Start();}
 void Loop(){
  while(!closed){
   TcpClient client;try{client=listener.AcceptTcpClient();}catch{if(closed)return;Thread.Sleep(200);continue;}
   new Thread(()=>Pair(client)){IsBackground=true}.Start();
  }
 }
 void Pair(TcpClient client){
  TcpClient server=new TcpClient();
  try{server.Connect(IPAddress.Loopback,target());}catch{client.Close();server.Close();return;}
  client.NoDelay=true;server.NoDelay=true;
  var a=client.GetStream();var b=server.GetStream();
  Action close=()=>{try{client.Close();}catch{}try{server.Close();}catch{}};
  new Thread(()=>Pump(a,b,close)){IsBackground=true}.Start();
  Pump(b,a,close);
 }
 static void Pump(NetworkStream from,NetworkStream to,Action close){
  var buffer=new byte[16384];
  try{int n;while((n=from.Read(buffer,0,buffer.Length))>0)to.Write(buffer,0,n);}catch{}
  close();
 }
 public void Dispose(){closed=true;try{if(listener!=null)listener.Stop();}catch{}}
}
}
