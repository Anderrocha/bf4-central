using System;
using System.Collections.Generic;
namespace BF4Bots {
public static class BotMaps {
 // Declared by the installed LAN BOT ALPHA v0.2 manual (2026-11-07).
 // This is a compatibility declaration, not a gameplay calibration.
 static readonly HashSet<string> Conquest=new HashSet<string>(StringComparer.Ordinal){"MP_Prison","MP_Resort","MP_Abandoned","XP5_Night_01","MP_Siege","XP3_Prpganda","XP0_Metro","XP3_MarketPl","MP_Tremors","XP1_003","MP_TheDish","MP_Naval"};
 public static bool Supported(string map,string mode){return mode=="AirSuperiority0"||(mode=="ConquestLarge0"&&Conquest.Contains(map));}
 public static string Warning(string map,string mode){return MapOption.NameOf(map)+" / "+MapOption.ModeName(mode)+" não tem suporte de bots confirmado nesta versão do mod. Os nomes podem aparecer no placar, mas a IA pode ficar parada. Use Operation Locker ou outro mapa da lista compatível. Carregar mesmo assim?";}
}
}
