﻿using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MultipleSpouses
{
	public static class UIPatches
	{
		private static IMonitor Monitor;

		// call this method from your Entry class
		public static void Initialize(IMonitor monitor)
		{
			Monitor = monitor;
		}
		public static void SocialPage_drawNPCSlot(SocialPage __instance, int i, List<string> ___kidsNames, ref Dictionary<string, string> ___npcNames)
		{
			try
			{

				string name = __instance.names[i] as string;
				if (___kidsNames.Contains(name))
				{
					if (___npcNames[name].EndsWith(")"))
					{
						___npcNames[name] = string.Join(" ", ___npcNames[name].Split(' ').Reverse().Skip(1).Reverse());
					}
				}
			}
			catch (Exception ex)
			{
				Monitor.Log($"Failed in {nameof(SocialPage_drawNPCSlot)}:\n{ex}", LogLevel.Error);
			}
		}
		public static bool Event_answerDialogueQuestion_Prefix(Event __instance, NPC who, string answerKey)
		{
			try
			{

				if (answerKey == "danceAsk" && !who.HasPartnerForDance && Game1.player.friendshipData[who.Name].IsMarried())
				{
					string accept = "";
					int gender = who.Gender;
					if (gender != 0)
					{
						if (gender == 1)
						{
							accept = Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1634");
						}
					}
					else
					{
						accept = Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1633");
					}
					try
					{
						Game1.player.changeFriendship(250, Game1.getCharacterFromName(who.Name, true));
					}
					catch
					{
					}
					Game1.player.dancePartner.Value = who;
					who.setNewDialogue(accept, false, false);
					using (List<NPC>.Enumerator enumerator = __instance.actors.GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							NPC j = enumerator.Current;
							if (j.CurrentDialogue != null && j.CurrentDialogue.Count > 0 && j.CurrentDialogue.Peek().getCurrentDialogue().Equals("..."))
							{
								j.CurrentDialogue.Clear();
							}
						}
					}
					Game1.drawDialogue(who);
					who.immediateSpeak = true;
					who.facePlayer(Game1.player);
					who.Halt();
					return false;
				}
			}

			catch (Exception ex)
			{
				Monitor.Log($"Failed in {nameof(Event_answerDialogueQuestion_Prefix)}:\n{ex}", LogLevel.Error);
			}
			return true;
		}
		
		public static void DialogueBox_Prefix(ref List<string> dialogues)
		{
			try
			{
				if (dialogues[1] == Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1826"))
                {
					List<string> newDialogues = new List<string>()
					{
						dialogues[0]
					};



					List<NPC> spouses = ModEntry.GetAllSpouses().Values.OrderBy(o => Game1.player.friendshipData[o.Name].Points).Reverse().Take(4).ToList();

					List<int> which = new List<int>{ 0, 1, 2, 3 };

					ModEntry.ShuffleList(ref which);

					List<int> myWhich = new List<int>(which).Take(spouses.Count).ToList();

					for(int i = 0; i < spouses.Count; i++)
                    {
                        switch (which[i])
                        {
							case 0:
								newDialogues.Add(Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1827", spouses[i].displayName));
								break;
							case 1:
								newDialogues.Add(((spouses[i].Gender == 0) ? Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1832") : Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1834")) + " " + ((spouses[i].Gender == 0) ? Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1837", spouses[i].displayName[0]) : Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1838", spouses[i].displayName[0])));
								break;
							case 2:
								newDialogues.Add(Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1843", spouses[i].displayName));
								break;
							case 3:
								newDialogues.Add(((spouses[i].Gender == 0) ? Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1831") : Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1833")) + " " + ((spouses[i].Gender == 0) ? Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1837", spouses[i].displayName[0]) : Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1838", spouses[i].displayName[i])));
								break;
                        }
                    }
					dialogues = new List<string>(newDialogues);
				}
			}
			catch (Exception ex)
			{
				Monitor.Log($"Failed in {nameof(DialogueBox_Prefix)}:\n{ex}", LogLevel.Error);
			}
		}
	}
}