using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SkToolbox.Utility;
using UnityEngine;
using SkScope = SkToolbox.SkMenuController.SkScope;

namespace SkToolbox.SkModules
{
    internal class ModPlayer : SkBaseModule, IModule
    {
        //private Rect EnemyWindow;
        bool bTeleport = false;

        //GameObject gParent;
        //GameObject gObject;
        //bool gPickPut = false;

        Rect rectClock = new Rect(05, 005, 125, 20);
        Rect rectCoords = new Rect(05, 027, 125, 20);
        Rect rectEnemy = new Rect(05, 280, 425, 50);

        List<Character> nearbyCharacters = new List<Character>();

        public ModPlayer() : base()
        {
            base.ModuleName = "Player";
            base.Loading();
        }

        public void Start()
        {
            BeginMenu();
            base.Ready(); // Must be called when the module has completed initialization. // End of Start
        }

        /// <summary>This tab renders an icon grid rather than a list, so the normal item list stays empty.</summary>
        public void BeginMenu()
        {
            MenuOptions = new SkMenu();
        }

        /// <summary>Entry point for the tab.</summary>
        internal void ShowGrid()
        {
            // Valheim 1.0: resync from the live Player. Per-instance flags reset on respawn or relog, and the
            // vanilla hotkeys flip them behind our back.
            Player lp = Player.m_localPlayer;
            if (lp != null)
            {
                SkCommandProcessor.godEnabled = lp.InGodMode();      // Player.cs:4470
                SkCommandProcessor.flyEnabled = lp.IsDebugFlying();  // Player.cs:4507 (owner path -> m_debugFly)
                SkCommandProcessor.noCostEnabled = lp.NoCostCheat(); // Player.cs:6177
            }
            SkMC.RequestGridMenu(BuildGrid(), (List<SkMenuController.SkMenuSlider>)null, "Player", showFilter: false);
        }

        /// <summary>
        /// The Player tab as icon cells. Names live in the footer tooltip; toggles carry a live predicate so the
        /// controller can outline them while they are on, without rebuilding the grid after every click.
        /// </summary>
        private List<SkMenuController.SkGridItem> BuildGrid()
        {
            List<SkMenuController.SkGridItem> grid = new List<SkMenuController.SkGridItem>();

            AddFoodCell(grid, "Melhor Comida de Vida", FoodKind.Health, null);
            AddFoodCell(grid, "Melhor Comida de Stamina", FoodKind.Stamina, null);
            AddFoodCell(grid, "Melhor Comida de Eitr", FoodKind.Eitr, null);
            AddFoodCell(grid, "Comida Equilibrada", FoodKind.Balanced, SkIcons.First("Tankard", "BarleyWine"));
            Action(grid, "Personagem", "Curar a Si Mesmo", "Restaura vida e stamina", SkIcons.First("MeadHealthMedium", "MeadHealthMinor", "Honey"), Heal);
            Action(grid, "Personagem", "Reparar Tudo", "Repara todos os itens que você carrega", SkIcons.First("Hammer"), RepairAll);
            Action(grid, "Personagem", "Habilidades", "Define ou redefine o nível de uma habilidade", SkIcons.First("Wishbone", "Coins"), ShowSkillForm);
            Action(grid, "Personagem", "Vomitar", "Esvazia o estômago, limpando os três espaços de comida", SkIcons.First("Entrails", "RawMeat"),
                   () => SkRun.CmdNotify("puke", "Estômago esvaziado."));
            Action(grid, "Personagem", "Tamanho do Inventário", "Altera quantas linhas o seu inventário possui", SkIcons.First("ArmorLeatherChest", "ArmorRagsChest"),
                   ShowInventoryForm);
            Action(grid, "Personagem", "Estatísticas", "Mostra o que o jogo registrou para este personagem", SkIcons.First("Ruby", "Coins"),
                   () => SkRun.Show("Estatísticas do Jogador", "stats", "Nenhuma estatística para mostrar ainda."));
            Action(grid, "Personagem", "Ajustes", "Limite de peso, raio de coleta, pulo, velocidades e raio de revelação do mapa",
                   SkIcons.First("Bronze", "Copper"), ShowTuningForm);
            Action(grid, "Personagem", "Limpar Inventário", "Descarta tudo o que você está carregando",
                   SkIcons.First("Flint", "Stone"), ShowClearInventoryForm);

            Toggle(grid, "Trapaças", "Modo Deus", "Não recebe nenhum dano", SkIcons.First("HelmetOdin", "CapeOdin"),
                   ToggleGodmode, () => Player.m_localPlayer != null && Player.m_localPlayer.InGodMode());
            Toggle(grid, "Trapaças", "Voo", "Voo livre", SkIcons.First("Feathers"),
                   ToggleFlying, () => Player.m_localPlayer != null && Player.m_localPlayer.IsDebugFlying());
            Toggle(grid, "Trapaças", "Stamina Infinita", "Nada consome stamina: correr, nadar, furtividade, pescar, atacar, pular, esquivar", SkIcons.First("MeadStaminaMedium", "MeadStaminaMinor"),
                   ToggleInfStam, () => Player.m_localPlayer != null && SkCommandProcessor.infStamina);
            Toggle(grid, "Trapaças", "Eitr Infinito", "Magias e cajados não custam nada e o eitr recarrega instantaneamente. Requer comida de eitr", SkIcons.First("MeadEitrMinor", "Eitr", "Sap"),
                   ToggleInfEitr, () => Player.m_localPlayer != null && SkCommandProcessor.infEitr);
            Toggle(grid, "Trapaças", "Sem Limite de Carga", "Carregue o quanto quiser, sem lentidão e sem aviso de sobrecarga",
                   SkIcons.First("ArmorBronzeChest", "ArmorLeatherChest"),
                   ToggleNoCarryLimit, () => Player.m_localPlayer != null && SkCommandProcessor.noCarryLimit);
            Toggle(grid, "Trapaças", "Construção Sem Custo", "Desbloqueia todas as peças e constrói de graça", SkIcons.First("Wood", "Stone"),
                   ToggleNoCost, () => Player.m_localPlayer != null && Player.m_localPlayer.NoCostCheat());
            Toggle(grid, "Trapaças", "Construir em Qualquer Lugar", "Remove restrições de construção", SkIcons.First("Cultivator", "Hoe"),
                   ToggleAnywhere, () => SkCommandPatcher.bBuildAnywhere);
            Toggle(grid, "Trapaças", "Sem Necessidade de Suporte", "Estruturas flutuam sem suporte. Desativar faz construções sem suporte desmoronarem",
                   SkIcons.First("FineWood", "RoundLog", "Wood"), ToggleNoSupport, () => SkCommandPatcher.BFreeSupport);
            Toggle(grid, "Trapaças", "Fantasma", "Criaturas não conseguem te ver", SkIcons.First("CapeLinen", "CapeDeerHide", "Feathers"),
                   ToggleGhost, () => Player.m_localPlayer != null && Player.m_localPlayer.InGhostMode());
            Action(grid, "Trapaças", "Alcance", "Interaja e coloque coisas de longe",
                   SkIcons.First("Chain", "LeatherScraps"), ShowReachForm);
            Action(grid, "Trapaças", "Domesticar", "Domestica todas as criaturas próximas", SkIcons.First("Carrot", "Raspberry"), Tame, SkScope.Server);
            Action(grid, "Trapaças", "Imunidades", "Escolha o que pode te causar dano, em vez do tudo ou nada do Modo Deus",
                   SkIcons.First("ShieldIronTower", "ShieldWood", "HelmetOdin"), ShowImmunityForm);
            Action(grid, "Trapaças", "Poder do Guardião", "Escolha o poder de um chefe pelo troféu, use-o ou limpe o tempo de recarga",
                   SkIcons.First("TrophyEikthyr", "Wishbone"), ShowPowerGrid);
            Action(grid, "Trapaças", "Efeitos de Status", "Aplica um efeito de status ou limpa os existentes",
                   SkIcons.First("MeadFrostResist", "MeadPoisonResist", "MeadHealthMinor"), ShowStatusForm);

            Toggle(grid, "Informações", "Teleporte para o Mouse", "Pressione til (~) para se teleportar para onde está olhando", SkIcons.First("SurtlingCore", "Thunderstone"),
                   ToggleTeleport, () => bTeleport);
            Action(grid, "Informações", "Portais", "Lista todas as tags de portais", SkIcons.First("SurtlingCore"),
                   () => SkCommandProcessor.ProcessCommand("/portals", SkCommandProcessor.LogTo.Chat));
            Toggle(grid, "Informações", "Exibir Coordenadas", "Mostra as coordenadas no canto superior esquerdo", SkIcons.First("FishingRodFloat", "Thunderstone", "Ruby"),
                   ToggleCoords, () => SkCommandProcessor.bCoords);
            Toggle(grid, "Informações", "Detectar Inimigos Próximos", "Alcance: 20m", SkIcons.First("Wishbone"),
                   ToggleESPEnemies, () => SkCommandProcessor.bDetectEnemies);
            Action(grid, "Informações", "Posição", "Exibe suas coordenadas, zona e distância do centro", SkIcons.First("Amber", "Coins"),
                   () => SkRun.Show("Posição", "pos", "Sem posição ainda."));
            Action(grid, "Informações", "Encontrar Lápide", "Marca suas lápides próximas no mapa", SkIcons.First("AmberPearl", "Ruby"),
                   () => SkCommandProcessor.ProcessCommand("/findtomb", SkCommandProcessor.LogTo.Chat));
            Action(grid, "Informações", "Marcadores", "Salva onde você está e permite teleportar de volta mais tarde",
                   SkIcons.First("SurtlingCore", "Thunderstone"), ShowBookmarkForm);
            Action(grid, "Informações", "Conjuntos de Equipamentos", "Salva um modelo de todo o seu inventário e restaura mais tarde",
                   SkIcons.First("ArmorBronzeChest", "ArmorLeatherChest", "ArmorRagsChest"), ShowLoadoutForm);

            return grid;
        }

        private static void Action(List<SkMenuController.SkGridItem> grid, string section, string label, string tip, Sprite icon,
                                   System.Action run, SkScope scope = SkScope.Client)
        {
            grid.Add(new SkMenuController.SkGridItem
            {
                Name = label,
                Display = label,
                Tip = label + "  -  " + tip,
                Icon = icon,
                Section = section,
                Scope = scope,
                OnClick = (string ignored) => run(),
            });
        }

        private static void Toggle(List<SkMenuController.SkGridItem> grid, string section, string label, string tip, Sprite icon,
                                   System.Action run, Func<bool> isOn, SkScope scope = SkScope.Client)
        {
            grid.Add(new SkMenuController.SkGridItem
            {
                Name = label,
                Display = label,
                Tip = label + "  -  " + tip,
                Icon = icon,
                Section = section,
                Scope = scope,
                OnClick = (string ignored) => run(),
                IsOn = isOn,
            });
        }

        // ------------------------------------------------------------------ skills

        /// <summary>
        /// Skill picker. Levels are shown next to each name so the current state is visible before changing it.
        /// The game's CheatRaiseSkill adds to a skill and clamps to 0-100, so setting an exact level means raising
        /// by the difference, which also accepts a negative value.
        /// </summary>
        private void ShowSkillForm()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                SkCommandProcessor.Notify("Jogador ainda não carregado.");
                return;
            }

            SkMenuController.SkFormField picker = new SkMenuController.SkFormField
            {
                Id = "skill",
                Label = "Habilidade",
                Kind = SkMenuController.SkFieldKind.Choice,
            };
            picker.Options.Add("All");
            picker.OptionLabels.Add("Todas as habilidades");

            Skills skills = player.GetSkills();
            foreach (Skills.SkillType type in Enum.GetValues(typeof(Skills.SkillType)))
            {
                if (type == Skills.SkillType.None || type == Skills.SkillType.All) continue;
                float level = 0f;
                try { level = skills != null ? skills.GetSkillLevel(type) : 0f; } catch (Exception) { }
                picker.Options.Add(type.ToString());
                picker.OptionLabels.Add(type.ToString() + "     <color=#9FB6CC>nível " + Mathf.RoundToInt(level) + "</color>");
            }
            picker.Selected = 1; // first real skill rather than "All"

            SkMenuController.SkFormField amount = new SkMenuController.SkFormField
            {
                Id = "level",
                Label = "Nível a definir",
                Kind = SkMenuController.SkFieldKind.IntSlider,
                Min = 0,
                Max = 100,
                IntValue = 50,
            };

            SkMenuController.SkForm form = new SkMenuController.SkForm
            {
                Title = "Habilidades",
                Note = "Escolha uma habilidade, selecione um nível e clique em Definir. Redefinir volta para zero.",
            };
            form.Fields.Add(picker);
            form.Fields.Add(amount);

            form.Actions.Add(new SkMenuController.SkFormAction
            {
                Label = "Definir nível",
                Run = (SkMenuController.SkForm f) => SetSkill(f.Field("skill").SelectedOption, f.Field("level").IntValue),
            });
            form.Actions.Add(new SkMenuController.SkFormAction
            {
                Label = "Redefinir para zero",
                Run = (SkMenuController.SkForm f) => ResetSkill(f.Field("skill").SelectedOption),
            });

            SkMC.ShowForm(form);
        }

        private static void SetSkill(string name, int target)
        {
            Player player = Player.m_localPlayer;
            if (player == null || string.IsNullOrEmpty(name)) return;
            Skills skills = player.GetSkills();
            if (skills == null) return;

            if (name == "All")
            {
                foreach (Skills.SkillType type in Enum.GetValues(typeof(Skills.SkillType)))
                {
                    if (type == Skills.SkillType.None || type == Skills.SkillType.All) continue;
                    skills.CheatRaiseSkill(type.ToString(), target - skills.GetSkillLevel(type), false);
                }
                SkCommandProcessor.Notify("Todas as habilidades definidas para " + target);
                return;
            }

            Skills.SkillType chosen;
            try { chosen = (Skills.SkillType)Enum.Parse(typeof(Skills.SkillType), name, true); }
            catch (Exception) { SkCommandProcessor.Notify("Habilidade desconhecida: " + name); return; }

            skills.CheatRaiseSkill(name, target - skills.GetSkillLevel(chosen), false);
            SkCommandProcessor.Notify(name + " definida para " + target);
        }

        private static void ResetSkill(string name)
        {
            Player player = Player.m_localPlayer;
            if (player == null || string.IsNullOrEmpty(name)) return;
            Skills skills = player.GetSkills();
            if (skills == null) return;
            skills.CheatResetSkill(name == "All" ? "all" : name);
            SkCommandProcessor.Notify((name == "All" ? "Todas as habilidades" : name) + " redefinidas para zero");
        }

        // ------------------------------------------------------------------ immunities

        /// <summary>
        /// Per-source damage immunity. God mode is all or nothing; this is the dial, for the times you want the
        /// cliff and the campfire to stop killing you while a greydwarf still can.
        ///
        /// The list comes from the HitType enum rather than being hand-written, so a new source in a future patch
        /// appears on its own. Fighting damage is left out on purpose: switching those off is just god mode with
        /// extra clicks, and god mode is one cell away.
        /// </summary>
        private void ShowImmunityForm()
        {
            SkMenuController.SkFormField list = new SkMenuController.SkFormField
            {
                Id = "types",
                Label = "Não pode me ferir",
                Kind = SkMenuController.SkFieldKind.Checklist,
                Height = 210,
            };

            foreach (HitData.HitType type in Enum.GetValues(typeof(HitData.HitType)))
            {
                if (type == HitData.HitType.Undefined) continue;
                if (type == HitData.HitType.EnemyHit || type == HitData.HitType.PlayerHit) continue;
                list.Options.Add(((int)type).ToString());
                list.OptionLabels.Add(Spaced(type.ToString()));
                list.Checked.Add(SkCommandProcessor.immuneTo.Contains((int)type));
            }

            SkMenuController.SkForm form = new SkMenuController.SkForm
            {
                Title = "Imunidades",
                Note = "Qualquer item ativado aqui não te causará dano. Ataques de criaturas e jogadores não estão listados de propósito - para isso use o Modo Deus.",
            };
            form.Fields.Add(list);

            form.Actions.Add(new SkMenuController.SkFormAction
            {
                Label = "Aplicar",
                Run = (SkMenuController.SkForm f) =>
                {
                    SkMenuController.SkFormField picked = f.Field("types");
                    SkCommandProcessor.immuneTo.Clear();
                    int on = 0;
                    for (int i = 0; i < picked.Options.Count && i < picked.Checked.Count; i++)
                    {
                        if (!picked.Checked[i]) continue;
                        int value;
                        if (!int.TryParse(picked.Options[i], out value)) continue;
                        SkCommandProcessor.immuneTo.Add(value);
                        on++;
                    }
                    SkCommandProcessor.Notify(on > 0 ? "Imune a " + on + " tipos de dano." : "Nenhuma imunidade definida.");
                },
            });
            form.Actions.Add(new SkMenuController.SkFormAction
            {
                Label = "Ativar todas",
                Run = (SkMenuController.SkForm f) =>
                {
                    SkMenuController.SkFormField picked = f.Field("types");
                    SkCommandProcessor.immuneTo.Clear();
                    for (int i = 0; i < picked.Options.Count; i++)
                    {
                        int value;
                        if (int.TryParse(picked.Options[i], out value)) SkCommandProcessor.immuneTo.Add(value);
                    }
                    SkCommandProcessor.Notify("Nada além de combate pode te ferir agora.");
                },
            });
            form.Actions.Add(new SkMenuController.SkFormAction
            {
                Label = "Desativar todas",
                Run = (SkMenuController.SkForm f) =>
                {
                    SkCommandProcessor.immuneTo.Clear();
                    SkCommandProcessor.Notify("Imunidades limpas.");
                },
            });
            SkMC.ShowForm(form);
        }

        /// <summary>AshlandsLava reads better as Ashlands Lava.</summary>
        private static string Spaced(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            System.Text.StringBuilder built = new System.Text.StringBuilder(name.Length + 4);
            for (int i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1])) built.Append(' ');
                built.Append(name[i]);
            }
            return built.ToString();
        }

        // ------------------------------------------------------------------ guardian power

        /// <summary>
        /// GP_ name to the trophy whose icon stands for it. Three of the seven do not follow the power's own name -
        /// Moder's trophy is the dragon queen's, Yagluth's is the goblin king's, the Queen's is the seeker queen's -
        /// so this is written out rather than derived. A wrong name here is a silent blank cell, not an error.
        /// </summary>
        private static readonly Dictionary<string, string> PowerTrophies = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "GP_Eikthyr",  "TrophyEikthyr" },
            { "GP_TheElder", "TrophyTheElder" },
            { "GP_Bonemass", "TrophyBonemass" },
            { "GP_Moder",    "TrophyDragonQueen" },
            { "GP_Yagluth",  "TrophyGoblinKing" },
            { "GP_Queen",    "TrophySeekerQueen" },
            { "GP_Fader",    "TrophyFader" },
        };

        /// <summary>Progression order, which is not the order ObjectDB happens to hold them in.</summary>
        private static readonly List<string> PowerOrder = new List<string>
        {
            "GP_Eikthyr", "GP_TheElder", "GP_Bonemass", "GP_Moder", "GP_Yagluth", "GP_Queen", "GP_Fader",
        };

        /// <summary>
        /// Boss powers as a grid of trophies, with the ring on the one you have. Clicking a trophy sets that power:
        /// in a grid the ring is the state, so a click that did not move it would read as having done nothing.
        ///
        /// A pushed grid rather than a form, so it gets the same cells, ring, tooltips and Back button as every
        /// other level of the menu. The cost is the live "ready in Ns" readout the form used to show, which has
        /// nowhere to sit here - the two action cells report where things stand when you click them instead.
        ///
        /// Calls the player directly rather than the setpower command: that one is admin-gated, so it is refused
        /// for a client on someone else's server, and a guardian power is entirely local to you.
        /// </summary>
        private void ShowPowerGrid()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                SkCommandProcessor.Notify("Jogador ainda não carregado.");
                return;
            }

            List<StatusEffect> powers = new List<StatusEffect>();
            ObjectDB db = ObjectDB.instance;
            if (db != null && db.m_StatusEffects != null)
            {
                foreach (StatusEffect effect in db.m_StatusEffects)
                {
                    // Guardian powers are the GP_ prefixed status effects; everything else in here is a buff.
                    if (effect == null || string.IsNullOrEmpty(effect.name)) continue;
                    if (!effect.name.StartsWith("GP_", StringComparison.OrdinalIgnoreCase)) continue;
                    powers.Add(effect);
                }
            }
            if (powers.Count == 0)
            {
                SkCommandProcessor.Notify("A lista de poderes não está disponível ainda. Entre em um mundo primeiro.");
                return;
            }

            // Read from ObjectDB rather than from the table, so another mod's power still gets a cell. The table
            // only supplies the icon and the order; anything unrecognised sorts to the end and borrows the wishbone.
            powers.Sort(delegate (StatusEffect a, StatusEffect b)
            {
                int ia = PowerOrder.IndexOf(a.name); if (ia < 0) ia = int.MaxValue;
                int ib = PowerOrder.IndexOf(b.name); if (ib < 0) ib = int.MaxValue;
                return ia != ib ? ia.CompareTo(ib) : string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase);
            });

            List<SkMenuController.SkGridItem> grid = new List<SkMenuController.SkGridItem>();
            foreach (StatusEffect effect in powers)
            {
                string gp = effect.name;
                string label = PowerLabel(effect);
                string trophy;
                Sprite icon = PowerTrophies.TryGetValue(gp, out trophy)
                    ? SkIcons.First(trophy, "Wishbone")
                    : SkIcons.First("Wishbone");

                grid.Add(new SkMenuController.SkGridItem
                {
                    Name = gp,
                    Display = label,
                    Tip = label + "  -  define este como seu poder. O anel marca o poder atual",
                    Icon = icon,
                    Section = "Powers",
                    Scope = SkScope.Client,
                    // Read live from the player, so the ring follows a power set at a boss stone as well.
                    IsOn = () =>
                    {
                        Player p = Player.m_localPlayer;
                        return p != null && p.GetGuardianPowerName() == gp;
                    },
                    OnClick = (string ignored) => SetPower(gp, label),
                });
            }

            Action(grid, "Actions", "Usar agora", "Ativa o poder definido. Como rezar na pedra, também concede o bônus a jogadores a até 10m",
                   SkIcons.First("Wishbone", "TrophyEikthyr"), UsePower, SkScope.Server);
            Action(grid, "Actions", "Limpar tempo de recarga", "Deixa o poder pronto novamente sem esperar",
                   SkIcons.First("MeadStaminaMedium", "Coins"), ClearPowerCooldown);

            SkMC.RequestGridMenu(grid, (List<SkMenuController.SkMenuSlider>)null, "Poder do Guardião", showFilter: false);
        }

        /// <summary>The power's display name, falling back to the prefab name before localization is up.</summary>
        private static string PowerLabel(StatusEffect effect)
        {
            try
            {
                if (Localization.instance != null && !string.IsNullOrEmpty(effect.m_name))
                {
                    string localized = Localization.instance.Localize(effect.m_name);
                    if (!string.IsNullOrEmpty(localized) && !localized.StartsWith("[")) return localized;
                }
            }
            catch (Exception)
            {
                // Localization is not ready; the prefab name is a fine label.
            }
            return effect.name;
        }

        private static void SetPower(string gp, string label)
        {
            Player lp = Player.m_localPlayer;
            if (lp == null)
            {
                SkCommandProcessor.Notify("Jogador ainda não carregado.");
                return;
            }
            lp.SetGuardianPower(gp);
            lp.m_guardianPowerCooldown = 0f; // SetGuardianPower leaves it alone, so a swap would inherit the old wait
            SkCommandProcessor.Notify("Poder definido: " + label);
        }

        /// <summary>
        /// StartGuardianPower only fires the animation. The buff comes from the animation event calling
        /// ActivateGuardianPower, which is why an interrupted animation used to leave you with nothing. Calling it
        /// ourselves is what makes the button reliable, and it still applies exactly once: it sets the cooldown, so
        /// the animation event's own call finds one running and does nothing.
        ///
        /// Neither return value is worth reading. ActivateGuardianPower returns false on every path it has,
        /// including the one that worked.
        /// </summary>
        private static void UsePower()
        {
            Player lp = Player.m_localPlayer;
            if (lp == null)
            {
                SkCommandProcessor.Notify("Jogador ainda não carregado.");
                return;
            }
            if (string.IsNullOrEmpty(lp.GetGuardianPowerName()))
            {
                SkCommandProcessor.Notify("Nenhum poder definido - escolha um troféu primeiro.");
                return;
            }

            lp.m_guardianPowerCooldown = 0f; // both calls below refuse outright while one is running
            lp.StartGuardianPower();         // the animation and the sound
            lp.ActivateGuardianPower();      // the buff itself
            SkCommandProcessor.Notify("Poder utilizado.");
        }

        private static void ClearPowerCooldown()
        {
            Player lp = Player.m_localPlayer;
            if (lp == null)
            {
                SkCommandProcessor.Notify("Jogador ainda não carregado.");
                return;
            }
            if (lp.m_guardianPowerCooldown <= 0f)
            {
                SkCommandProcessor.Notify(string.IsNullOrEmpty(lp.GetGuardianPowerName())
                    ? "Nenhum poder definido ainda."
                    : "O poder já está pronto.");
                return;
            }
            lp.m_guardianPowerCooldown = 0f;
            SkCommandProcessor.Notify("O poder está pronto novamente.");
        }

        // ------------------------------------------------------------------ bookmarks and loadouts

        /// <summary>Named places in this world. Coordinates from another seed are meaningless, so they are hidden.</summary>
        private void ShowBookmarkForm()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                SkCommandProcessor.Notify("Jogador ainda não carregado.");
                return;
            }

            List<SkBookmarks.Bookmark> saved = SkBookmarks.ForThisWorld();
            List<string> names = new List<string>();
            List<string> labels = new List<string>();
            foreach (SkBookmarks.Bookmark mark in saved)
            {
                names.Add(mark.Name);
                labels.Add(mark.Name + "     <color=#9FB6CC>" + Mathf.RoundToInt(mark.Pos.x) + ", " + Mathf.RoundToInt(mark.Pos.z) + "</color>");
            }

            SkMenuController.SkForm form = new SkMenuController.SkForm
            {
                Title = "Marcadores",
                Note = saved.Count > 0
                    ? "Locais salvos neste mundo."
                    : "Nenhum local salvo neste mundo ainda. Clique em Salvar Aqui para nomear o local atual.",
            };
            if (names.Count > 0)
            {
                form.Fields.Add(new SkMenuController.SkFormField
                {
                    Id = "mark",
                    Label = "Locais salvos",
                    Kind = SkMenuController.SkFieldKind.Choice,
                    Height = 180,
                });
                form.Field("mark").Options.AddRange(names);
                form.Field("mark").OptionLabels.AddRange(labels);
            }
            form.Actions.Add(new SkMenuController.SkFormAction
            {
                Label = "Salvar aqui",
                // Asks for the name in a prompt over this form, rather than a text box sitting in it that has
                // nothing to do with the list above it.
                Run = (SkMenuController.SkForm f) => SkMC.ShowPrompt(new SkMenuController.SkPrompt
                {
                    Title = "Salvar marcador",
                    Label = "Um nome para onde você está pisando.",
                    Accept = "Salvar",
                    OnAccept = (string name) =>
                    {
                        Player lp = Player.m_localPlayer;
                        if (lp == null) { SkCommandProcessor.Notify("Jogador ainda não carregado."); return; }
                        if (name.Length == 0) { SkCommandProcessor.Notify("Dê um nome ao marcador primeiro."); return; }
                        SkCommandProcessor.Notify(SkBookmarks.Save(name, lp.transform.position)
                            ? "Marcador salvo: " + name
                            : "Entre em um mundo primeiro.");
                        ShowBookmarkForm(); // rebuild so the new one is in the list
                    },
                }),
            });
            if (names.Count > 0)
            {
                form.Actions.Add(new SkMenuController.SkFormAction
                {
                    Label = "Teleportar",
                    Run = (SkMenuController.SkForm f) =>
                    {
                        Player lp = Player.m_localPlayer;
                        string pick = f.Field("mark").SelectedOption;
                        if (lp == null || string.IsNullOrEmpty(pick)) return;
                        foreach (SkBookmarks.Bookmark mark in SkBookmarks.ForThisWorld())
                        {
                            if (!string.Equals(mark.Name, pick, StringComparison.OrdinalIgnoreCase)) continue;
                            lp.TeleportTo(mark.Pos, lp.transform.rotation, true);
                            SkCommandProcessor.Notify("Teleportando para " + mark.Name);
                            return;
                        }
                    },
                });
                form.Actions.Add(new SkMenuController.SkFormAction
                {
                    Label = "Excluir",
                    Run = (SkMenuController.SkForm f) =>
                    {
                        string pick = f.Field("mark").SelectedOption;
                        if (string.IsNullOrEmpty(pick)) return;
                        SkBookmarks.Delete(pick);
                        SkCommandProcessor.Notify("Marcador excluído: " + pick);
                    },
                });
            }
            SkMC.ShowForm(form);
        }

        /// <summary>Equipment sets. Restoring only re-equips what you are actually carrying.</summary>
        private void ShowLoadoutForm()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                SkCommandProcessor.Notify("Jogador ainda não carregado.");
                return;
            }

            List<string> names = SkLoadouts.Names();
            SkMenuController.SkForm form = new SkMenuController.SkForm
            {
                Title = "Conjuntos de Equipamentos",
                Note = "Salva todo o seu inventário: cada pilha, tamanho, qualidade, durabilidade e criador, além das peças equipadas. Carregar um conjunto restaura tudo.",
            };
            if (names.Count > 0)
            {
                form.Warning = "Carregar um conjunto SUBSTITUI tudo o que você está carregando.";
                SkMenuController.SkFormField picker = new SkMenuController.SkFormField
                {
                    Id = "set",
                    Label = "Conjuntos salvos",
                    Kind = SkMenuController.SkFieldKind.Choice,
                    Height = 150,
                };
                foreach (string entry in names)
                {
                    picker.Options.Add(entry);
                    int count = SkLoadouts.CountOf(entry);
                    picker.OptionLabels.Add(count >= 0
                        ? entry + "     <color=#9FB6CC>" + count + " pilhas</color>"
                        : entry);
                }
                form.Fields.Add(picker);
                form.Fields.Add(new SkMenuController.SkFormField
                {
                    Id = "confirm",
                    Label = "Substituir o que estou carregando",
                    Kind = SkMenuController.SkFieldKind.Toggle,
                });
            }
            form.Actions.Add(new SkMenuController.SkFormAction
            {
                Label = "Salvar atual",
                Run = (SkMenuController.SkForm f) => SkMC.ShowPrompt(new SkMenuController.SkPrompt
                {
                    Title = "Salvar conjunto",
                    Label = "Um nome para o seu inventário atual.",
                    Accept = "Salvar",
                    OnAccept = (string name) =>
                    {
                        Player lp = Player.m_localPlayer;
                        if (lp == null) { SkCommandProcessor.Notify("Jogador ainda não carregado."); return; }
                        if (name.Length == 0) { SkCommandProcessor.Notify("Dê um nome ao conjunto primeiro."); return; }
                        int count = SkLoadouts.Save(name, lp);
                        SkCommandProcessor.Notify(count > 0
                            ? "Salvo " + count + " pilhas como: " + name
                            : "Seu inventário está vazio, nada a salvar.");
                        ShowLoadoutForm();
                    },
                }),
            });
            if (names.Count > 0)
            {
                // Only the load is gated: saving and deleting cannot lose you anything you are holding.
                form.Validate = (SkMenuController.SkForm f) =>
                {
                    SkMenuController.SkFormField box = f.Field("confirm");
                    return (box != null && !box.BoolValue) ? "Ative a confirmação para carregar um conjunto." : null;
                };
                form.Actions.Add(new SkMenuController.SkFormAction
                {
                    Label = "Carregar conjunto",
                    Run = (SkMenuController.SkForm f) =>
                    {
                        Player lp = Player.m_localPlayer;
                        string pick = f.Field("set").SelectedOption;
                        if (lp == null || string.IsNullOrEmpty(pick)) return;
                        int stacks, equipped;
                        if (!SkLoadouts.Restore(pick, lp, out stacks, out equipped))
                        {
                            SkCommandProcessor.Notify("Não foi possível ler este conjunto.");
                            return;
                        }
                        SkCommandProcessor.Notify("Restaurado " + stacks + " pilhas, " + equipped + " equipados.");
                    },
                });
                form.Actions.Add(new SkMenuController.SkFormAction
                {
                    Label = "Excluir",
                    Run = (SkMenuController.SkForm f) =>
                    {
                        string pick = f.Field("set").SelectedOption;
                        if (string.IsNullOrEmpty(pick)) return;
                        SkLoadouts.Delete(pick);
                        SkCommandProcessor.Notify("Conjunto excluído: " + pick);
                    },
                });
            }
            SkMC.ShowForm(form);
        }

        // ------------------------------------------------------------------ tuning, reach, inventory

        /// <summary>
        /// The knobs that used to be reachable only through /set. They are written straight onto the player rather
        /// than run as commands, which means ReapplyOnSpawn can restore them; through /set they quietly reverted on
        /// every death.
        /// </summary>
        private void ShowTuningForm()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                SkCommandProcessor.Notify("Jogador ainda não carregado.");
                return;
            }
            SkCommandProcessor.CaptureTuningBaseline(player);

            SkMenuController.SkForm form = new SkMenuController.SkForm
            {
                Title = "Ajustes",
                Note = "Cada controle começa no valor atual do jogo. Eles são restaurados ao morrer, a menos que PersistCheatsOnDeath esteja desativado.",
            };
            form.Fields.Add(Slider("carry", "Limite de peso", 50, 2000,
                                   SkCommandProcessor.carryWeight > 0 ? SkCommandProcessor.carryWeight : Mathf.RoundToInt(player.m_maxCarryWeight)));
            // The pickup sweep uses a fixed 100-entry buffer, so a huge radius quietly collects less, not more.
            form.Fields.Add(Slider("pickup", "Raio de coleta automática", 2, 20,
                                   SkCommandProcessor.pickupRange > 0 ? SkCommandProcessor.pickupRange : Mathf.RoundToInt(player.m_autoPickupRange)));
            form.Fields.Add(Slider("jump", "Força do pulo", 4, 40,
                                   SkCommandProcessor.jumpForce > 0 ? SkCommandProcessor.jumpForce : Mathf.RoundToInt(player.m_jumpForce)));
            form.Fields.Add(Slider("run", "Velocidade de corrida", 2, 40,
                                   SkCommandProcessor.runSpeed > 0 ? SkCommandProcessor.runSpeed : Mathf.RoundToInt(player.m_runSpeed)));
            form.Fields.Add(Slider("swim", "Velocidade de natação", 1, 20,
                                   SkCommandProcessor.swimSpeed > 0 ? SkCommandProcessor.swimSpeed : Mathf.RoundToInt(player.m_swimSpeed)));
            form.Fields.Add(Slider("explore", "Raio de revelação do mapa", 50, 500,
                                   SkCommandProcessor.exploreRadius > 0 ? SkCommandProcessor.exploreRadius
                                   : (Minimap.instance != null ? Mathf.RoundToInt(Minimap.instance.m_exploreRadius) : 100)));

            form.Actions.Add(new SkMenuController.SkFormAction
            {
                Label = "Aplicar",
                Run = (SkMenuController.SkForm f) =>
                {
                    Player lp = Player.m_localPlayer;
                    if (lp == null) { SkCommandProcessor.Notify("Jogador ainda não carregado."); return; }
                    SkCommandProcessor.carryWeight = f.Field("carry").IntValue;
                    SkCommandProcessor.pickupRange = f.Field("pickup").IntValue;
                    SkCommandProcessor.jumpForce = f.Field("jump").IntValue;
                    SkCommandProcessor.runSpeed = f.Field("run").IntValue;
                    SkCommandProcessor.swimSpeed = f.Field("swim").IntValue;
                    SkCommandProcessor.exploreRadius = f.Field("explore").IntValue;
                    SkCommandProcessor.ApplyTuning(lp);
                    SkCommandProcessor.Notify("Ajustes aplicados.");
                },
            });
            form.Actions.Add(new SkMenuController.SkFormAction
            {
                Label = "Padrões",
                Run = (SkMenuController.SkForm f) =>
                {
                    SkCommandProcessor.ResetTuning(Player.m_localPlayer);
                    SkCommandProcessor.Notify("Ajustes restaurados para o padrão.");
                },
            });
            SkMC.ShowForm(form);
        }

        /// <summary>Interaction reach. A slider rather than a toggle, since the distance is the point.</summary>
        private void ShowReachForm()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                SkCommandProcessor.Notify("Jogador ainda não carregado.");
                return;
            }

            SkMenuController.SkForm form = new SkMenuController.SkForm
            {
                Title = "Alcance",
                Note = "A distância máxima para interagir com coisas e posicionar construções. O padrão é cerca de 5.",
            };
            form.Fields.Add(Slider("reach", "Alcance", 20, 100, Mathf.RoundToInt(SkCommandProcessor.farInteractDistance)));
            form.Actions.Add(new SkMenuController.SkFormAction
            {
                Label = "Aplicar",
                Run = (SkMenuController.SkForm f) =>
                {
                    Player lp = Player.m_localPlayer;
                    if (lp == null) { SkCommandProcessor.Notify("Jogador ainda não carregado."); return; }
                    SkCommandProcessor.farInteractDistance = f.Field("reach").IntValue;
                    SkCommandProcessor.farInteract = true;
                    SkCommandProcessor.ApplyFarInteract(lp, applyOffValues: true);
                    SkCommandProcessor.Notify("Alcance definido para " + f.Field("reach").IntValue);
                },
            });
            form.Actions.Add(new SkMenuController.SkFormAction
            {
                Label = "Voltar ao normal",
                Run = (SkMenuController.SkForm f) =>
                {
                    Player lp = Player.m_localPlayer;
                    if (lp == null) { SkCommandProcessor.Notify("Jogador ainda não carregado."); return; }
                    SkCommandProcessor.farInteract = false;
                    SkCommandProcessor.ApplyFarInteract(lp, applyOffValues: true);
                    SkCommandProcessor.Notify("Alcance restaurado ao normal.");
                },
            });
            SkMC.ShowForm(form);
        }

        /// <summary>Emptying your pockets has no undo, so it asks first rather than being a bare cell.</summary>
        private void ShowClearInventoryForm()
        {
            SkMenuController.SkForm form = new SkMenuController.SkForm
            {
                Title = "Limpar Inventário",
                Warning = "Isso DESTRÓI todos os itens que você está carregando.",
                Note = "Inclui equipamentos vestidos e espaços extras. Nada é jogado no chão, nenhuma lápide é criada e não há como recuperar. Guarde o que quiser manter em um baú primeiro.",
            };
            form.Fields.Add(new SkMenuController.SkFormField
            {
                Id = "confirm",
                Label = "Eu entendo que tudo será destruído",
                Kind = SkMenuController.SkFieldKind.Toggle,
            });
            form.Validate = (SkMenuController.SkForm f) =>
            {
                SkMenuController.SkFormField box = f.Field("confirm");
                return (box != null && !box.BoolValue) ? "Ative a confirmação primeiro." : null;
            };
            form.Actions.Add(new SkMenuController.SkFormAction
            {
                Label = "Destruir tudo",
                Run = (SkMenuController.SkForm f) =>
                    SkCommandProcessor.ProcessCommand("/clearinventory", SkCommandProcessor.LogTo.Chat),
            });
            SkMC.ShowForm(form);
        }

        private static SkMenuController.SkFormField Slider(string id, string label, int min, int max, int value)
        {
            return new SkMenuController.SkFormField
            {
                Id = id,
                Label = label,
                Kind = SkMenuController.SkFieldKind.IntSlider,
                Min = min,
                Max = max,
                IntValue = Mathf.Clamp(value, min, max),
            };
        }

        public void ToggleNoSupport()
        {
            SkCommandProcessor.ProcessCommand("/nosup", SkCommandProcessor.LogTo.Chat);
        }

        public void ToggleGhost()
        {
            SkCommandProcessor.ProcessCommand("/ghost", SkCommandProcessor.LogTo.Chat);
        }

        // ------------------------------------------------------------------ inventory and status

        /// <summary>Inventory rows. The game clamps this to 9 and drops anything that no longer fits.</summary>
        private void ShowInventoryForm()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                SkCommandProcessor.Notify("Jogador ainda não carregado.");
                return;
            }

            int rows = 4;
            try { rows = player.GetInventory().GetHeight(); } catch (Exception) { }

            SkMenuController.SkForm form = new SkMenuController.SkForm
            {
                Title = "Tamanho do Inventário",
                Note = "Linhas no seu inventário. 4 é o tamanho normal; diminuir descarta o que não couber.",
            };
            form.Fields.Add(new SkMenuController.SkFormField
            {
                Id = "rows",
                Label = "Linhas",
                Kind = SkMenuController.SkFieldKind.IntSlider,
                Min = 1,
                Max = 9,
                IntValue = Mathf.Clamp(rows, 1, 9),
            });
            form.Actions.Add(new SkMenuController.SkFormAction
            {
                Label = "Aplicar",
                Run = (SkMenuController.SkForm f) =>
                    SkRun.CmdNotify("inventorysize " + f.Field("rows").IntValue, "Inventário definido para " + f.Field("rows").IntValue + " linhas."),
            });
            form.Actions.Add(new SkMenuController.SkFormAction
            {
                Label = "Voltar para 4",
                Run = (SkMenuController.SkForm f) => SkRun.CmdNotify("inventorysize 4", "Inventário restaurado para 4 linhas."),
            });
            SkMC.ShowForm(form);
        }

        /// <summary>Every status effect the game ships, by prefab name, which is what addstatus expects.</summary>
        private static string StatusLabel(StatusEffect effect)
        {
            try
            {
                if (Localization.instance != null && !string.IsNullOrEmpty(effect.m_name))
                {
                    string localized = Localization.instance.Localize(effect.m_name);
                    if (!string.IsNullOrEmpty(localized) && !localized.StartsWith("[")) return localized;
                }
            }
            catch (Exception)
            {
            }
            return Spaced(effect.name);
        }

        private void ShowStatusForm()
        {
            ObjectDB db = ObjectDB.instance;
            List<StatusEffect> effects = new List<StatusEffect>();
            if (db != null && db.m_StatusEffects != null)
            {
                foreach (StatusEffect effect in db.m_StatusEffects)
                {
                    if (effect != null && !string.IsNullOrEmpty(effect.name) && !effects.Exists(e => e.name == effect.name))
                    {
                        effects.Add(effect);
                    }
                }
            }
            if (effects.Count == 0)
            {
                SkCommandProcessor.Notify("A lista de efeitos de status não está disponível ainda. Entre em um mundo primeiro.");
                return;
            }

            effects.Sort((a, b) => string.Compare(StatusLabel(a), StatusLabel(b), StringComparison.OrdinalIgnoreCase));

            SkMenuController.SkFormField picker = new SkMenuController.SkFormField
            {
                Id = "status",
                Label = "Efeito de status",
                Kind = SkMenuController.SkFieldKind.Choice,
                Height = 220,
            };

            foreach (StatusEffect effect in effects)
            {
                picker.Options.Add(effect.name);
                picker.OptionLabels.Add(StatusLabel(effect));
            }

            SkMenuController.SkForm form = new SkMenuController.SkForm
            {
                Title = "Efeitos de Status",
                Note = "Adiciona um efeito com sua duração normal. Limpar remove tudo o que você possui atualmente, incluindo comida.",
            };
            form.Fields.Add(picker);

            form.Actions.Add(new SkMenuController.SkFormAction
            {
                Label = "Adicionar efeito",
                Run = (SkMenuController.SkForm f) =>
                {
                    string name = f.Field("status").SelectedOption;
                    if (string.IsNullOrEmpty(name)) return;
                    Player target = Player.m_localPlayer;
                    if (target == null)
                    {
                        SkCommandProcessor.Notify("Jogador ainda não carregado.");
                        return;
                    }
                    target.GetSEMan().AddStatusEffect(name.GetStableHashCode(), true, 0, 0f, -1);
                    int sel = f.Field("status").Selected;
                    string labelName = (sel >= 0 && sel < f.Field("status").OptionLabels.Count) ? f.Field("status").OptionLabels[sel] : name;
                    SkCommandProcessor.Notify("Adicionado: " + labelName);
                },
            });
            form.Actions.Add(new SkMenuController.SkFormAction
            {
                Label = "Limpar todos",
                Run = (SkMenuController.SkForm f) =>
                {
                    Player target = Player.m_localPlayer;
                    if (target == null)
                    {
                        SkCommandProcessor.Notify("Jogador ainda não carregado.");
                        return;
                    }
                    target.ClearHardDeath();
                    target.GetSEMan().RemoveAllStatusEffects();
                    SkCommandProcessor.Notify("Efeitos de status limpos.");
                },
            });
            SkMC.ShowForm(form);
        }

        // ------------------------------------------------------------------ food

        private enum FoodKind { Health, Stamina, Eitr, Balanced }

        private static List<ItemDrop> foodCache;
        private static int foodCacheFor = 0;

        /// <summary>Every consumable in the game that grants health, stamina or eitr. Cached per ObjectDB.</summary>
        private static List<ItemDrop> AllFoods()
        {
            ObjectDB db = ObjectDB.instance;
            if (db == null || db.m_items == null)
            {
                return new List<ItemDrop>();
            }
            int key = db.GetInstanceID();
            if (foodCache != null && foodCacheFor == key)
            {
                return foodCache;
            }

            List<ItemDrop> found = new List<ItemDrop>();
            foreach (GameObject prefab in db.m_items)
            {
                if (prefab == null) continue;
                ItemDrop drop = prefab.GetComponent<ItemDrop>();
                if (drop == null || drop.m_itemData == null || drop.m_itemData.m_shared == null) continue;
                ItemDrop.ItemData.SharedData shared = drop.m_itemData.m_shared;
                // The game's own test for "you may eat this": Humanoid.CanConsumeItem refuses anything that is
                // not Consumable before it looks at anything else. Carrying food values is not enough, because
                // the crafting intermediates carry them too - the uncooked prefabs were made by copying the
                // cooked one and changing only the item type, so BreadDough has 120 stamina against Bread's 70
                // and LoxPieUncooked has 120 health against LoxPie's 75. Being the highest numbers in the game
                // they won every ranking, and Player.EatFood does not check the type itself, so they really did
                // get eaten. Raw meat and the eggs came in the same way.
                if (shared.m_itemType != ItemDrop.ItemData.ItemType.Consumable) continue;
                if (shared.m_food <= 0f && shared.m_foodStamina <= 0f && shared.m_foodEitr <= 0f) continue;
                if (shared.m_foodBurnTime <= 0f) continue; // not actually edible as a buff
                // The prefab asset does not carry m_dropPrefab; EatFood dereferences it, so record it now.
                drop.m_itemData.m_dropPrefab = prefab;
                found.Add(drop);
            }
            foodCache = found;
            foodCacheFor = key;
            return foodCache;
        }

        private static float Stat(ItemDrop drop, FoodKind kind)
        {
            ItemDrop.ItemData.SharedData s = drop.m_itemData.m_shared;
            switch (kind)
            {
                case FoodKind.Health: return s.m_food;
                case FoodKind.Stamina: return s.m_foodStamina;
                case FoodKind.Eitr: return s.m_foodEitr;
                default: return s.m_food + s.m_foodStamina + s.m_foodEitr;
            }
        }

        /// <summary>Top food for one stat, ignoring anything already chosen.</summary>
        private static ItemDrop Best(List<ItemDrop> pool, FoodKind kind, List<ItemDrop> taken)
        {
            ItemDrop best = null;
            float bestValue = 0f;
            foreach (ItemDrop candidate in pool)
            {
                if (Stat(candidate, kind) <= 0f) continue;
                bool already = false;
                foreach (ItemDrop chosen in taken)
                {
                    if (chosen.m_itemData.m_shared.m_name == candidate.m_itemData.m_shared.m_name) { already = true; break; }
                }
                if (already) continue;
                float value = Stat(candidate, kind);
                if (best == null || value > bestValue)
                {
                    best = candidate;
                    bestValue = value;
                }
            }
            return best;
        }

        /// <summary>The three foods to eat for a given goal. Balanced takes the best of each stat.</summary>
        private static List<ItemDrop> PickFoods(FoodKind kind)
        {
            List<ItemDrop> pool = AllFoods();
            List<ItemDrop> picked = new List<ItemDrop>();
            if (pool.Count == 0) return picked;

            if (kind == FoodKind.Balanced)
            {
                foreach (FoodKind part in new FoodKind[] { FoodKind.Health, FoodKind.Stamina, FoodKind.Eitr })
                {
                    ItemDrop choice = Best(pool, part, picked);
                    if (choice != null) picked.Add(choice);
                }
            }
            else
            {
                while (picked.Count < 3)
                {
                    ItemDrop choice = Best(pool, kind, picked);
                    if (choice == null) break;
                    picked.Add(choice);
                }
            }
            return picked;
        }

        private static string FoodName(ItemDrop drop)
        {
            try
            {
                if (Localization.instance != null)
                {
                    string localized = Localization.instance.Localize(drop.m_itemData.m_shared.m_name);
                    if (!string.IsNullOrEmpty(localized)) return localized;
                }
            }
            catch (Exception)
            {
            }
            return drop.name;
        }

        /// <summary>
        /// Replaces all three food slots. ClearFood empties them first, so each EatFood lands in a free slot
        /// instead of being refused by the "already eaten / slots full" checks.
        /// </summary>
        private static void Feed(FoodKind kind)
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                SkCommandProcessor.Notify("Jogador ainda não carregado.");
                return;
            }

            List<ItemDrop> picks = PickFoods(kind);
            if (picks.Count == 0)
            {
                SkCommandProcessor.Notify("Nenhuma comida encontrada para isso. Entre em um mundo primeiro.");
                return;
            }

            player.ClearFood();
            List<string> eaten = new List<string>();
            foreach (ItemDrop drop in picks)
            {
                ItemDrop.ItemData data = drop.m_itemData.Clone();
                data.m_dropPrefab = drop.m_itemData.m_dropPrefab;
                data.m_stack = 1;
                if (player.EatFood(data))
                {
                    eaten.Add(FoodName(drop));
                }
            }

            SkCommandProcessor.Notify(eaten.Count > 0 ? "Comeu: " + string.Join(", ", eaten.ToArray()) : "Não foi possível comer nada.");
        }

        private static Sprite IconOf(ItemDrop drop)
        {
            if (drop == null || drop.m_itemData == null || drop.m_itemData.m_shared == null) return null;
            Sprite[] icons = drop.m_itemData.m_shared.m_icons;
            return icons != null && icons.Length > 0 ? icons[0] : null;
        }

        /// <summary>One food cell. The icon is whatever it will eat first, unless an override is given.</summary>
        private void AddFoodCell(List<SkMenuController.SkGridItem> grid, string label, FoodKind kind, Sprite iconOverride)
        {
            List<ItemDrop> picks = PickFoods(kind);
            string tip = picks.Count == 0
                ? label + "  -  preenche os três espaços de comida"
                : label + "  -  " + string.Join(", ", picks.ConvertAll(FoodName).ToArray());
            grid.Add(new SkMenuController.SkGridItem
            {
                Name = label,
                Display = label,
                Tip = tip,
                Icon = iconOverride != null ? iconOverride : (picks.Count > 0 ? IconOf(picks[0]) : null),
                Section = "Personagem",
                Scope = SkScope.Client,
                OnClick = (string ignored) => Feed(kind),
            });
        }

        public void ToggleTeleport()
        {
            bTeleport = !bTeleport;
            if (bTeleport)
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Teleporte ativado. Pressione til (~)!", 0, null);
            }
            else
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Teleporte desativado.", 0, null);
            }
            BeginMenu();
        }

        public void Heal()
        {
            SkCommandProcessor.ProcessCommand("/heal", SkCommandProcessor.LogTo.Chat);
        }
        public void Tame()
        {
            SkCommandProcessor.ProcessCommand("/tame", SkCommandProcessor.LogTo.Chat);
        }

        public void ToggleNoCost()
        {
            SkCommandProcessor.ProcessCommand("/nocost", SkCommandProcessor.LogTo.Chat);
            BeginMenu();
        }

        public void ToggleESPEnemies()
        {
            SkCommandProcessor.ProcessCommand("/detect 20", SkCommandProcessor.LogTo.Chat);
            BeginMenu();
        }

        public void ToggleCoords()
        {
            SkCommandProcessor.ProcessCommand("/coords", SkCommandProcessor.LogTo.Chat);
            BeginMenu();
        }

        public void ToggleAnywhere()
        {
            SkCommandProcessor.ProcessCommand("/nores", SkCommandProcessor.LogTo.Chat);
            BeginMenu();
        }

        public void ToggleGodmode()
        {
            SkCommandProcessor.ProcessCommand("/god", SkCommandProcessor.LogTo.Chat);
            BeginMenu();
        }

        public void ToggleFlying()
        {
            SkCommandProcessor.ProcessCommand("/fly", SkCommandProcessor.LogTo.Chat);
            BeginMenu();
        }

        public void ToggleInfStam()
        {
            SkCommandProcessor.ProcessCommand("/infstam", SkCommandProcessor.LogTo.Chat);
            BeginMenu();
        }

        public void ToggleInfEitr()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                SkCommandProcessor.Notify("Jogador ainda não carregado.");
                return;
            }
            SkCommandProcessor.infEitr = !SkCommandProcessor.infEitr;
            SkCommandProcessor.ApplyInfEitr(player, applyOffValues: true);

            if (!SkCommandProcessor.infEitr)
            {
                SkCommandProcessor.Notify("Eitr infinito desativado.");
            }
            else
            {
                // Max eitr comes entirely from food, so on an empty stomach the toggle has nothing to work with.
                // Say so rather than let it look broken.
                SkCommandProcessor.Notify(player.GetMaxEitr() > 0f
                    ? "Eitr infinito ativado."
                    : "Eitr infinito ativado - mas você não comeu comida de eitr, então seu máximo ainda é zero.");
            }
        }

        /// <summary>
        /// Being over the carry limit is what causes both the slowdown and the encumbered state, so lifting the
        /// limit removes the whole condition rather than papering over its symptoms.
        /// </summary>
        public void ToggleNoCarryLimit()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                SkCommandProcessor.Notify("Jogador ainda não carregado.");
                return;
            }
            SkCommandProcessor.CaptureTuningBaseline(player);
            SkCommandProcessor.noCarryLimit = !SkCommandProcessor.noCarryLimit;

            SkCommandProcessor.ApplyTuning(player);
            if (!SkCommandProcessor.noCarryLimit && SkCommandProcessor.carryWeight <= 0)
            {
                // Nothing on the Tweaks slider to fall back to, so put the game's own value back.
                SkCommandProcessor.RestoreCarryBaseline(player);
            }
            SkCommandProcessor.Notify(SkCommandProcessor.noCarryLimit
                ? "Limite de carga removido."
                : "Limite de carga restaurado ao normal.");
        }

        public void RepairAll()
        {
            SkCommandProcessor.ProcessCommand("/repair", SkCommandProcessor.LogTo.Chat);
        }

        void Update()
        {
            if (bTeleport)
            {
                if (Input.GetKeyDown(KeyCode.BackQuote))
                {
                    Ray rayCast = Camera.main.ScreenPointToRay(Input.mousePosition);
                    RaycastHit hit;
                    if (Physics.Raycast(rayCast, out hit))
                    {
                        Vector3 targetLoc = hit.point;
                        Debug.DrawRay(Player.m_localPlayer.transform.position, targetLoc, Color.white);
                        Player.m_localPlayer.transform.position = targetLoc;
                        Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Teletransporte!", 0, null);
                    }
                }
            }
        }

        void OnGUI()
        {
        }
    }
}
