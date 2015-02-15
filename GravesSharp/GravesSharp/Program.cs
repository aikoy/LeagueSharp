using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;
using Notifications;

namespace GravesSharp
{
	class Program
	{
		public static Orbwalking.Orbwalker m_oOrbwalker;
		public static Menu m_mMenu;
		private static Obj_AI_Hero myHero;
		private static int m_iTurnOffCastE = 0;
		private static ToastNotification m_nENotification;

		public static SpellEx Q;
		//public static SpellEx Q1;
		public static SpellEx W;
		public static SpellEx E;
		public static SpellEx R;
		public static SpellEx R1;

		static void Main(string[] args)
		{
			CustomEvents.Game.OnGameLoad += GameLoaded;
		}

		private static void GameLoaded(EventArgs args)
		{
			myHero = ObjectManager.Player;

			if (!myHero.BaseSkinName.Equals("Graves"))
				return;

			m_nENotification = new ToastNotification("", new ColorBGRA(255f, 255f, 255f, 255f), -1);
			Notification.AddNotification(m_nENotification);
			Q = new SpellEx(SpellSlot.Q, 840f);
			//Q1 = new SpellEx(SpellSlot.Q, 930f);
			W = new SpellEx(SpellSlot.W, 950f);
			E = new SpellEx(SpellSlot.E, 450f);
			R = new SpellEx(SpellSlot.R, 1000f);
			R1 = new SpellEx(SpellSlot.R, 1600f);

			Q.SetSkillshot(0.26f, 20f * (float)Math.PI / 180, 1800f, false, SkillshotType.SkillshotCone);
			//Q1.SetSkillshot(0.26f, 50f, 1950f, false, SkillshotType.SkillshotLine);
			W.SetSkillshot(0.35f, 250f, 1650f, false, SkillshotType.SkillshotCircle);
			R.SetSkillshot(0.25f, 120f, 2100f, false, SkillshotType.SkillshotLine);
			R1.SetSkillshot(0.26f, 20f * (float)Math.PI / 180, 2100f, false, SkillshotType.SkillshotCone);

			m_mMenu = new Menu("GravesSharp", "GravesSharp", true);

			Menu tsMenu = new Menu("Target Selector", "ts");
			TargetSelector.AddToMenu(tsMenu);
			m_mMenu.AddSubMenu(tsMenu);

			Menu orbwalkMenu = new Menu("Orbwalking", "orbwalk");
			m_mMenu.AddSubMenu(orbwalkMenu);
			m_oOrbwalker = new Orbwalking.Orbwalker(orbwalkMenu);

			Menu comboMenu = new Menu("Combo Options", "combo");
			m_mMenu.AddSubMenu(comboMenu);
				comboMenu.AddItem(new MenuItem("enabled", "Combo Enabled").SetValue(new KeyBind(' ', KeyBindType.Press)));
				comboMenu.AddItem(new MenuItem("useQ", "Use Q").SetValue(true));
				comboMenu.AddItem(new MenuItem("useW", "Use W").SetValue(true));
			Menu harassMenu = new Menu("Harass Options", "harass");
			m_mMenu.AddSubMenu(harassMenu);
				harassMenu.AddItem(new MenuItem("enabled", "Harass Enabled").SetValue(new KeyBind('C', KeyBindType.Press)));
				harassMenu.AddItem(new MenuItem("useQ", "Use Q").SetValue(true));
				harassMenu.AddItem(new MenuItem("useW", "Use W").SetValue(false));
				harassMenu.AddItem(new MenuItem("manaSlider", "Harass Mana Percent").SetValue(new Slider(15, 0, 100)));
			/*Menu farmMenu = new Menu("Farm Options", "farm");
			m_config.AddSubMenu(farmMenu);
				farmMenu.AddItem(new MenuItem("enabled", "Farm Enabled").SetValue(new KeyBind('V', KeyBindType.Press)));
			//farm for later
			 */

			MenuItem comboDmg = new MenuItem("comboDmg", "Draw HPBar Combo Damage").SetValue(true);
			Utility.HpBarDamageIndicator.DamageToUnit += delegate(Obj_AI_Hero hero)
			{
				float dmg = 0;
				if (Ready(Q))
				{
					dmg += (float)myHero.GetSpellDamage(hero, SpellSlot.Q);
				}
				if (Ready(W))
				{
					dmg += (float)myHero.GetSpellDamage(hero, SpellSlot.W);
				}
				if (Ready(R))
				{
					dmg += (float)myHero.GetSpellDamage(hero, SpellSlot.R);
				}
				dmg += (float)myHero.GetAutoAttackDamage(hero, true) * 3;
				return dmg;
			};
			Utility.HpBarDamageIndicator.Enabled = comboDmg.GetValue<bool>();
			comboDmg.ValueChanged += delegate(object sender, OnValueChangeEventArgs eventArgs)
			{
				Utility.HpBarDamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
			};

			Menu drawMenu = new Menu("Draw Options", "draw");
			m_mMenu.AddSubMenu(drawMenu);
				drawMenu.AddItem(new MenuItem("Q", "Draw Q Range").SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
				drawMenu.AddItem(new MenuItem("W", "Draw W Range").SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
				drawMenu.AddItem(new MenuItem("E", "Draw E Range").SetValue(new Circle(true, Color.FromArgb(100, 255, 0, 255))));
				drawMenu.AddItem(new MenuItem("R", "Draw R Range").SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
				drawMenu.AddItem(new MenuItem("panicE", "Draw E Panic Status").SetValue(true)).ValueChanged +=
					delegate(object sender, OnValueChangeEventArgs eventArgs)
					{
						if (!eventArgs.GetNewValue<bool>())
						{
							Notification.RemoveNotification(m_nENotification);
							return;
						}
						Notification.AddNotification(m_nENotification);
					};
				drawMenu.AddItem(comboDmg);
			Menu miscMenu = new Menu("Misc Options", "misc");
			m_mMenu.AddSubMenu(miscMenu);
				miscMenu.AddItem(new MenuItem("castTime", "When to Cast Spells").SetValue(new StringList(new[] { "After Auto Attack", "Any Time" }, 0))).ValueChanged += CastTimeChanged;
				miscMenu.AddItem(new MenuItem("antiGapCloseE", "Use E as Anti-Gapclose").SetValue(true));
				miscMenu.AddItem(new MenuItem("castE", "Enable E Panic Mode").SetValue(new KeyBind('A', KeyBindType.Toggle))).ValueChanged += CastEChanged;
				miscMenu.AddItem(new MenuItem("castEDisable", "Disable Cast E After x Seconds").SetValue(new Slider(0, 0, 25)));
				miscMenu.AddItem(new MenuItem("useQ", "Use Q in E Panic Mode").SetValue(true));
				miscMenu.AddItem(new MenuItem("waitTimeQ", "Wait Time if Q is Down").SetValue(new Slider(3, 0, 15)));
				miscMenu.AddItem(new MenuItem("turnOffAfterAA", "Turn off E Panic Mode After AA").SetValue(true));
				miscMenu.AddItem(new MenuItem("useW", "Cast W in E Panic Mode").SetValue(true));
				miscMenu.AddItem(new MenuItem("autoUlt", "Auto Aim Ult").SetValue(new KeyBind('R', KeyBindType.Press)));
			Menu ksMenu = new Menu("KS Options", "ks");
			m_mMenu.AddSubMenu(ksMenu);
				ksMenu.AddItem(new MenuItem("ksQ", "KS with Q").SetValue(true));
				ksMenu.AddItem(new MenuItem("ksW", "KS with W").SetValue(false));
				ksMenu.AddItem(new MenuItem("ksR", "KS with R").SetValue(true));

			m_mMenu.AddToMainMenu();

			Game.OnGameUpdate += delegate(EventArgs eventArgs)
			{
				if (m_nENotification.IsValid)
				{
					List<string> list = new List<string>();
					if (m_mMenu.SubMenu("misc").Item("castE").GetValue<KeyBind>().Active)
					{
						list.Add("Panic E Enabled");
						if (m_mMenu.SubMenu("misc").Item("castEDisable").GetValue<Slider>().Value > 0)
						{
							list.Add(String.Format("Tuning off in {0} seconds", (m_iTurnOffCastE - Environment.TickCount) / 1000));
						}
					}

					m_nENotification.Text = list.ToArray();
					if (m_nENotification.Text.Length == 0)
						m_nENotification.ToastColor = new ColorBGRA(0f, 0f, 0f, 0f);
					else
						m_nENotification.ToastColor = new ColorBGRA(0f, 0f, 0f, 255f);
				}
				else if (!m_nENotification.IsValid && m_mMenu.SubMenu("misc").Item("panicE").GetValue<bool>())
				{
					Notification.AddNotification(m_nENotification);
				}
			};

			Game.OnGameUpdate += delegate(EventArgs eventArgs)
			{
				if (m_mMenu.SubMenu("misc").Item("autoUlt").GetValue<KeyBind>().Active)
				{
					Obj_AI_Base target = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
					if (target.IsValid && target.IsTargetable)
					{
						if (R1.Cast(target) != Spell.CastStates.SuccessfullyCasted)
							R.Cast(target);
					}
				}
			};
			Game.OnGameUpdate += KillSteal;
			Drawing.OnDraw += Draw;
			AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
			Orbwalking.AfterAttack += PanicE;



			if (m_mMenu.SubMenu("misc").Item("castTime").GetValue<StringList>().SelectedValue.Equals("After Auto Attack"))
			{
				ReadyForSpellCast.OnReadyForSpellCast += Combo;
				ReadyForSpellCast.OnReadyForSpellCast += Harass;
			}
			else if (m_mMenu.SubMenu("misc").Item("castTime").GetValue<StringList>().SelectedValue.Equals("Any Time"))
			{
				Game.OnGameUpdate += Combo;
				Game.OnGameUpdate += Harass;
			}

			Game.OnGameUpdate += delegate(EventArgs eventArgs)
			{
				if (m_mMenu.SubMenu("misc").Item("castEDisable").GetValue<Slider>().Value > 0 && Environment.TickCount >= m_iTurnOffCastE)
				{
					KeyBind bind = m_mMenu.SubMenu("misc").Item("castE").GetValue<KeyBind>();
					bind.Active = false;
					m_mMenu.SubMenu("misc").Item("castE").SetValue(bind);
				}
			};

			Game.PrintChat("<font color=\"#00BFFF\">GravesSharp -</font> <font color=\"#FFFFFF\">Loaded</font>");
		}

		private static void CastTimeChanged(object sender, OnValueChangeEventArgs onValueChangeEventArgs)
		{
			String newVal = onValueChangeEventArgs.GetNewValue<StringList>().SelectedValue;
			if (newVal.Equals("After Auto Attack"))
			{
				Game.OnGameUpdate -= Combo;
				Game.OnGameUpdate -= Harass;

				ReadyForSpellCast.OnReadyForSpellCast += Combo;
				ReadyForSpellCast.OnReadyForSpellCast += Harass;
			}
			else if (newVal.Equals("Any Time"))
			{
				ReadyForSpellCast.OnReadyForSpellCast -= Combo;
				ReadyForSpellCast.OnReadyForSpellCast -= Harass;

				Game.OnGameUpdate += Combo;
				Game.OnGameUpdate += Harass;
			}
		}

		private static void Combo(EventArgs args)
		{
			Obj_AI_Base target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
			
			if (target.IsValid)
				_Combo(target);
		}

		private static void Combo(ReadyForSpellCastEventArgs args)
		{
			if (args.unit.IsMe)
			{
				Obj_AI_Base target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
				if (target.IsValid<Obj_AI_Hero>())
					_Combo(target);
			}
		}

		private static void _Combo(Obj_AI_Base target)
		{
			if (m_mMenu.SubMenu("combo").Item("enabled").GetValue<KeyBind>().Active)
			{
				bool useQ = m_mMenu.SubMenu("combo").Item("useQ").GetValue<bool>();
				bool useW = m_mMenu.SubMenu("combo").Item("useW").GetValue<bool>();

				if (m_mMenu.SubMenu("misc").Item("castE").GetValue<KeyBind>().Active && Ready(E))
				{
					useQ = (useQ && !(m_mMenu.SubMenu("misc").Item("useQ").GetValue<bool>()));
					useW = (useW && !(m_mMenu.SubMenu("misc").Item("useW").GetValue<bool>()));

					if (!useQ && (myHero.Spellbook.GetSpell(SpellSlot.E).CooldownExpires - Game.Time) > m_mMenu.SubMenu("misc").Item("waitTimeQ").GetValue<Slider>().Value)
					{
						useQ = true;
					}
				}

				if (useQ && Ready(Q))
				{
					Q.Cast(target);
				}
				if (useW && Ready(W))
				{
					W.Cast(target);
				}
			}
		}

		private static void Harass(EventArgs args)
		{
			Obj_AI_Base target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);

			if (target.IsValid)
				_Harass(target);
		}

		private static void Harass(ReadyForSpellCastEventArgs args)
		{
			if (args.unit.IsMe)
			{
				Obj_AI_Base target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
				if (target.IsValid<Obj_AI_Hero>())
					_Harass(target);
			}
		}

		private static void _Harass(Obj_AI_Base target)
		{
			if (m_mMenu.SubMenu("harass").Item("enabled").GetValue<KeyBind>().Active)
			{
				bool useQ = m_mMenu.SubMenu("harass").Item("useQ").GetValue<bool>();
				bool useW = m_mMenu.SubMenu("harass").Item("useW").GetValue<bool>();

				if (m_mMenu.SubMenu("misc").Item("castE").GetValue<KeyBind>().Active && Ready(E))
				{
					useQ = (useQ && !(m_mMenu.SubMenu("misc").Item("useQ").GetValue<bool>()));
					useW = (useW && !(m_mMenu.SubMenu("misc").Item("useW").GetValue<bool>()));

					if (!useQ && myHero.Spellbook.GetSpell(SpellSlot.Q).Cooldown > m_mMenu.SubMenu("misc").Item("waitTimeQ").GetValue<Slider>().Value)
					{
						useQ = true;
					}
				}

				if (useQ && Ready(Q))
				{
					Q.Cast(target);
				}
				if (useW && Ready(W))
				{
					W.Cast(target);
				}
			}
		}

		private static void PanicE(AttackableUnit unit, AttackableUnit target)
		{
			if (unit.IsMe && target.GetType() == myHero.GetType())
			{
				_PanicE((Obj_AI_Base)target, new Vector3(0f, 0f, 0f), true);
			}
		}

		private static void _PanicE(Obj_AI_Base target, Vector3 pos, bool mousePos = false)
		{
			if (target.IsValid && target.IsTargetable && m_mMenu.SubMenu("misc").Item("castE").GetValue<KeyBind>().Active && Ready(E))
			{
				if (m_mMenu.SubMenu("misc").Item("useQ").GetValue<bool>())
				{
					if (!Ready(Q))
					{
						if ((myHero.Spellbook.GetSpell(SpellSlot.Q).CooldownExpires - Game.Time) <=
						    m_mMenu.SubMenu("misc").Item("waitTimeQ").GetValue<Slider>().Value)
						{
							return;
						}
					}
					else
					{
						Q.Cast(target);
						GameObjectProcessSpellCast castSpell = null;
						castSpell = delegate(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
						{
							Utility.DelayAction.Add(1, delegate
							{
								if (sender.IsMe && args.SData.Name.Equals("GravesClusterShot"))
								{
									if (mousePos)
									{
										Vector3 mouseVec = Game.CursorPos;
										Vector3 myPos = myHero.Position;
										Vector3 delta = (myPos - mouseVec).Normalized();
										Vector3 castPos = myPos - (delta*E.Range);

										ForceCast(E, castPos);
										Utility.DelayAction.Add(1, () => Obj_AI_Base.OnProcessSpellCast -= castSpell);
										Utility.DelayAction.Add(1, delegate
										{
											if (m_mMenu.SubMenu("misc").Item("turnOffAfterAA").GetValue<bool>())
											{
												KeyBind bind = m_mMenu.SubMenu("misc").Item("castE").GetValue<KeyBind>();
												bind.Active = false;
												m_mMenu.SubMenu("misc").Item("castE").SetValue(bind);
											}
										});
										return;
									}
									ForceCast(E, pos);
									Utility.DelayAction.Add(1, () => Obj_AI_Base.OnProcessSpellCast -= castSpell);
									Utility.DelayAction.Add(1, delegate
									{
										if (m_mMenu.SubMenu("misc").Item("turnOffAfterAA").GetValue<bool>())
										{
											KeyBind bind = m_mMenu.SubMenu("misc").Item("castE").GetValue<KeyBind>();
											bind.Active = false;
											m_mMenu.SubMenu("misc").Item("castE").SetValue(bind);
										}
									});
								}
							});
						};
						Obj_AI_Base.OnProcessSpellCast += castSpell;
						Utility.DelayAction.Add(500 + Game.Ping, () => Obj_AI_Base.OnProcessSpellCast -= castSpell);
						return;
					}
				}
				else if (m_mMenu.SubMenu("misc").Item("useW").GetValue<bool>() && Ready(W))
				{
					W.Cast(target);
					GameObjectProcessSpellCast castSpell = null;
					castSpell = delegate(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
					{
						Utility.DelayAction.Add(1, delegate
						{
							if (sender.IsMe && args.SData.Name.Equals("GravesSmokeGrenade"))
							{
								if (mousePos)
								{
									Vector3 mouseVec = Game.CursorPos;
									Vector3 myPos = myHero.Position;
									Vector3 delta = (myPos - mouseVec).Normalized();
									Vector3 castPos = myPos - (delta * E.Range);

									ForceCast(E, castPos);
									Utility.DelayAction.Add(1, () => Obj_AI_Base.OnProcessSpellCast -= castSpell);
									Utility.DelayAction.Add(1, delegate
									{
										if (m_mMenu.SubMenu("misc").Item("turnOffAfterAA").GetValue<bool>())
										{
											KeyBind bind = m_mMenu.SubMenu("misc").Item("castE").GetValue<KeyBind>();
											bind.Active = false;
											m_mMenu.SubMenu("misc").Item("castE").SetValue(bind);
										}
									});
									return;
								}
								ForceCast(E, pos);
								Utility.DelayAction.Add(1, () => Obj_AI_Base.OnProcessSpellCast -= castSpell);
								Utility.DelayAction.Add(1, delegate
								{
									if (m_mMenu.SubMenu("misc").Item("turnOffAfterAA").GetValue<bool>())
									{
										KeyBind bind = m_mMenu.SubMenu("misc").Item("castE").GetValue<KeyBind>();
										bind.Active = false;
										m_mMenu.SubMenu("misc").Item("castE").SetValue(bind);
									}
								});
							}
						});
					};
					Obj_AI_Base.OnProcessSpellCast += castSpell;
					Utility.DelayAction.Add(50 + Game.Ping, () => Obj_AI_Base.OnProcessSpellCast -= castSpell);
					return;
				}
				if (mousePos)
				{
					Vector3 mouseVec = Game.CursorPos;
					Vector3 myPos = myHero.Position;
					Vector3 delta = (myPos - mouseVec).Normalized();
					Vector3 castPos = myPos - (delta * E.Range);

					E.Cast(castPos);
					return;
				}
				E.Cast(pos);
			}
		}

		public static void ForceCast(SpellEx s, Vector3 pos)
		{
			s.ForceCast(pos);
		}
		public static void ForceCast(SpellEx s, Obj_AI_Base enemy)
		{
			s.ForceCast(enemy);
		}

		public static bool Ready(Spell spell)
		{
			return (myHero.Spellbook.CanUseSpell(spell.Slot) == SpellState.Ready ||
			        myHero.Spellbook.CanUseSpell(spell.Slot) == SpellState.Surpressed);
		}

		private static void CastR()
		{
			Obj_AI_Hero newTarget = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
			R.Cast(newTarget);
		}

		private static void Draw(EventArgs args)
		{
			/*if (m_mMenu.SubMenu("draw").Item("panicE").GetValue<bool>() && m_mMenu.SubMenu("misc").Item("castE").GetValue<KeyBind>().Active)
			{
				double x = (Drawing.Width - (Drawing.Width * 0.58));
				double y = (Drawing.Height - (Drawing.Height * 0.78));

				String text = "PANIC MODE ON";
				if (m_mMenu.SubMenu("misc").Item("castEDisable").GetValue<Slider>().Value > 0)
				{
					text += String.Format(" - TURNING OFF IN {0} SECONDS", ((m_iTurnOffCastE - Environment.TickCount) / 1000));
				}
				Drawing.DrawText((float)x, (float)y, Color.FromArgb(255, 255, 0, 0), text);
			}*/

			foreach (MenuItem item in m_mMenu.SubMenu("draw").Items)
			{
				if (item.Name.Length == 1 && item.GetValue<Circle>().Active)
				{
					Spell spell = GetSpell(item.Name[0]);
					if (spell != null)
					{
						Render.Circle.DrawCircle(myHero.Position, spell.Range, item.GetValue<Circle>().Color);
					}
				}
			}
		}

		private static Spell GetSpell(char spell)
		{
			switch (spell)
			{
				case 'Q':
					return Q;
				case 'W':
					return W;
				case 'E':
					return E;
				case 'R':
					return R;
			}
			return null;
		}

		private static void KillSteal(EventArgs args)
		{
			foreach (Obj_AI_Hero enemy in ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsValidTarget()))
			{
				double damage = 0;
				if (m_mMenu.SubMenu("ks").Item("ksW").GetValue<bool>() && Ready(W) && myHero.Distance(enemy) <= (W.Range - 5))
				{
					damage = myHero.GetSpellDamage(enemy, SpellSlot.W) * 0.7;
					if (enemy.Health <= damage)
					{
						W.Cast(enemy);
						continue;
					}
				}
				if (m_mMenu.SubMenu("ks").Item("ksQ").GetValue<bool>() && Ready(Q) && myHero.Distance(enemy) <= (Q.Range - 5))
				{
					damage = myHero.GetSpellDamage(enemy, SpellSlot.Q) * 0.8;
					if (enemy.Health <= damage)
					{
						Q.Cast(enemy);
						continue;
					}
				}
				if (m_mMenu.SubMenu("ks").Item("ksW").GetValue<bool>() && m_mMenu.SubMenu("ks").Item("ksQ").GetValue<bool>() &&
				    Ready(W) && Ready(Q) && myHero.Distance(enemy) <= (Q.Range - 5))
				{
					damage = (myHero.GetSpellDamage(enemy, SpellSlot.W) * 0.7) + (myHero.GetSpellDamage(enemy, SpellSlot.Q) * 0.8);
					if (enemy.Health <= damage)
					{
						ForceCast(Q, enemy);
						ForceCast(W, enemy);
						continue;
					}
				}
				if (m_mMenu.SubMenu("ks").Item("ksR").GetValue<bool>() && Ready(R))
				{
					damage = myHero.GetSpellDamage(enemy, SpellSlot.R) * 0.9;
					if (enemy.Health <= damage)
					{
						if (myHero.Distance(enemy) <= R.Range)
							R.Cast(enemy);
						else if (myHero.Distance(enemy) <= R1.Range)
							R1.Cast(enemy);
					}
				}
				if (m_mMenu.SubMenu("ks").Item("ksQ").GetValue<bool>() && m_mMenu.SubMenu("ks").Item("ksR").GetValue<bool>() &&
					Ready(Q) && Ready(R) && myHero.Distance(enemy) <= (Q.Range - 5))
				{
					damage = (myHero.GetSpellDamage(enemy, SpellSlot.Q) * 0.9) + (myHero.GetSpellDamage(enemy, SpellSlot.R) * 0.8);
					if (enemy.Health <= damage)
					{
						ForceCast(Q, enemy);
						ForceCast(R, enemy);
						continue;
					}
				}
			}
		}

		private static void CastEChanged(object sender, OnValueChangeEventArgs onValueChangeEventArgs)
		{
			if (m_mMenu.SubMenu("misc").Item("castEDisable").GetValue<Slider>().Value > 0 && onValueChangeEventArgs.GetNewValue<KeyBind>().Active)
				m_iTurnOffCastE = Environment.TickCount + (m_mMenu.SubMenu("misc").Item("castEDisable").GetValue<Slider>().Value * 1000);
		}

		static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
		{
			if (m_mMenu.SubMenu("misc").Item("antiGapCloseE").GetValue<bool>() && Ready(E))
			{
				if (myHero.Distance(gapcloser.End) <= E.Range)
				{
					Vector3 endPos = gapcloser.End;
					Vector3 myPos = myHero.Position;
					Vector3 delta = (myPos - endPos).Normalized();
					Vector3 castPos = myPos + (delta * E.Range);

					_PanicE(gapcloser.Sender, castPos);
				}
			}
		}
	}
}