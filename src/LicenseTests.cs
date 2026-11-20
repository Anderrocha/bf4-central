using System;
namespace BF4Bots {
// Liberação via Sócio Premiado: assinatura real do site (amostra feita com a chave do servidor).
public static class LicenseTests {
 const string Sample="QkY0QzF8VEVTVEUtTUFRVUlOQS0xMjN8dTB8VGVzdGV8MjAyNi0xMS0yMA==.BMkcB8R96DNCx1qLWxQDUt3cq6GX1rrf0HMaY0JukFiUszqaEzX5tW2iyx78rIbClgRSsxplxQBsUV/5B9vn3TUBKve7yBqdmTbk9tqnwRwqSMK1XfYkHIniId4+XpUWSCoPPPNJixRSRUPO5/cQ70gH1v2ucOamBqQTDdsprygIjKUzkgEsIFKmHgbSkrpXGTa1pacoJRWYmaDMS66wHEQyh6ubrVmUdbIJjx9u21Ug1t/NwNuLvf0/9qka3nZNGIjLxc0X0YtOLYWtU6MvuJpZ/RA/Lp/O0br9vDJWv9tU4BQDiH8JDNZkJxd3Z7kNeo4jlrZiuBFj/f8ugKy++g==";
 public static string Run(){
  string name;
  if(!License.Check(Sample,"TESTE-MAQUINA-123",out name)||name!="Teste")throw new Exception("Licença: amostra assinada pelo site não foi aceita");
  if(License.Check(Sample,"OUTRO-PC-000",out name))throw new Exception("Licença: aceitou licença de outro PC");
  string forged=Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("BF4C1|OUTRO-PC-000|u0|Teste|2026-11-20"))+Sample.Substring(Sample.IndexOf('.'));
  if(License.Check(forged,"OUTRO-PC-000",out name))throw new Exception("Licença: aceitou carga alterada");
  if(License.Check("",License.MachineId(),out name)||License.Check("lixo.lixo",License.MachineId(),out name))throw new Exception("Licença: aceitou texto inválido");
  if(String.IsNullOrEmpty(License.MachineId()))throw new Exception("Licença: sem identificador do PC");
  var r=License.Accept("{\"ok\":false,\"motivo\":\"semcompra\",\"nome\":\"Ana\",\"msg\":\"x\"}");
  if(r.Ok||r.Message.IndexOf("Pix")<0||r.Message.IndexOf("Ana")<0)throw new Exception("Licença: mensagem de conta sem compra errada");
  r=License.Accept("{\"ok\":true,\"licenca\":\""+Sample+"\"}");
  if(r.Ok)throw new Exception("Licença: gravou licença que não é deste PC");
  r=License.Accept("{\"ok\":false,\"motivo\":\"login\",\"msg\":\"Telefone ou senha incorretos.\"}");
  if(r.Ok||r.Message!="Telefone ou senha incorretos.")throw new Exception("Licença: mensagem de login errada");
  return "Licença: assinatura do site, PC, adulteração e respostas OK";
 }
}
}
