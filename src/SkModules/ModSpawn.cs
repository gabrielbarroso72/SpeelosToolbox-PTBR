using System;
using System.Collections.Generic;
using SkToolbox.Utility;
using UnityEngine;

namespace SkToolbox.SkModules
{
    /// <summary>
    /// Speelo's Toolbox: a aba de Invocação/Criaturas. Cada criatura no jogo em uma grade, com controles deslizantes
    /// para quantidade e nível de estrelas, e um alternador para gerá-las domesticadas.
    /// </summary>
    internal class ModSpawn : SkBaseModule, IModule
    {
        private const int MaxCount = 20;   // mais do que isso causa travamentos quando acordam juntas
        private const int MaxLevel = 3;

        private int count = 1;
        private int level = 1;
        private bool tamed = false;

        private List<SkMenuController.SkGridItem> cache;
        private int cachedFor = 0;

        /// <summary>
        /// Criaturas cujo troféu não tem o mesmo nome do prefab.
        /// </summary>
        private static readonly Dictionary<string, string> TrophyAliases = new Dictionary<string, string>
        {
            { "Troll", "TrophyForestTroll" },
            { "gd_king", "TrophyTheElder" },
            { "Dragon", "TrophyDragonQueen" },
            { "StoneGolem", "TrophySGolem" },
            { "Greyling", "TrophyGreydwarf" },
            { "Greydwarf_Elite", "TrophyGreydwarfBrute" },
            { "Greydwarf_Shaman", "TrophyGreydwarfShaman" },
            { "Draugr_Elite", "TrophyDraugrElite" },
            { "BlobTar", "TrophyBlobTar" },
            { "Hen", "TrophyHen" },
            { "Chicken", "TrophyHen" },
        };

        public ModSpawn() : base()
        {
            base.ModuleName = "Spawn";
            base.Loading();
        }

        public void Start()
        {
            BeginMenu();
            base.Ready(); // Deve ser chamado quando o módulo concluir a inicialização.
        }

        /// <summary>Esta aba renderiza uma grade de ícones em vez de uma lista.</summary>
        public void BeginMenu()
        {
            MenuOptions = new SkMenu();
        }

        internal void ShowGrid()
        {
            // ZNetScene é reconstruído ao carregar o mundo, portanto recria a lista quando ele muda.
            int key = ZNetScene.instance != null ? ZNetScene.instance.GetInstanceID() : 0;
            if (cache == null || cache.Count == 0 || cachedFor != key)
            {
                cache = BuildCreatures();
                cachedFor = key;
            }
            if (cache.Count == 0)
            {
                SkCommandProcessor.Notify("A lista de criaturas ainda não está disponível. Entre em um mundo primeiro.");
                return;
            }

            List<SkMenuController.SkMenuSlider> sliders = new List<SkMenuController.SkMenuSlider>
            {
                new SkMenuController.SkMenuSlider
                {
                    Label = "Quantidade", Min = 1, Max = MaxCount,
                    Get = () => count, Set = (int value) => count = value,
                },
                new SkMenuController.SkMenuSlider
                {
                    Label = "Nível", Min = 1, Max = MaxLevel,
                    Get = () => level, Set = (int value) => level = value,
                },
            };

            List<SkMenuController.SkMenuToggle> toggles = new List<SkMenuController.SkMenuToggle>
            {
                new SkMenuController.SkMenuToggle
                {
                    Label = "Gerar domesticado",
                    Get = () => tamed, Set = (bool value) => tamed = value,
                },
            };

            SkMC.RequestGridMenu(cache, sliders, "Criaturas", showFilter: true, toggles: toggles);
        }

        /// <summary>Cada prefab com um componente Character, ordenado pelo seu nome localizado.</summary>
        private List<SkMenuController.SkGridItem> BuildCreatures()
        {
            List<SkMenuController.SkGridItem> list = new List<SkMenuController.SkGridItem>();
            if (ZNetScene.instance == null || ZNetScene.instance.m_prefabs == null)
            {
                return list;
            }

            foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
            {
                if (prefab == null || string.IsNullOrEmpty(prefab.name))
                {
                    continue;
                }
                Character character = prefab.GetComponent<Character>();
                if (character == null)
                {
                    continue;
                }

                string display = prefab.name;
                try
                {
                    if (Localization.instance != null && !string.IsNullOrEmpty(character.m_name))
                    {
                        string localized = Localization.instance.Localize(character.m_name);
                        if (!string.IsNullOrEmpty(localized) && !localized.StartsWith("["))
                        {
                            display = localized;
                        }
                    }
                }
                catch (Exception)
                {
                }

                list.Add(new SkMenuController.SkGridItem
                {
                    Name = prefab.name,
                    Display = display,
                    Tip = display + "   (" + prefab.name + ")" + (character.m_boss ? "   [chefe]" : ""),
                    Icon = TrophyIcon(prefab.name),
                    Scope = SkMenuController.SkScope.Server,
                    OnClick = Spawn,
                });
            }

            list.Sort((SkMenuController.SkGridItem a, SkMenuController.SkGridItem b) =>
                string.Compare(a.Display, b.Display, StringComparison.OrdinalIgnoreCase));
            return list;
        }

        /// <summary>Criaturas não possuem ícone próprio, portanto usam o do troféu quando existe.</summary>
        private static Sprite TrophyIcon(string prefabName)
        {
            string alias;
            if (TrophyAliases.TryGetValue(prefabName, out alias))
            {
                Sprite aliased = SkIcons.Item(alias);
                if (aliased != null)
                {
                    return aliased;
                }
            }
            return SkIcons.Item("Trophy" + prefabName);
        }

        /// <summary>Calcula a posição de geração: usa o ponto onde a câmera livre está apontando se ela estiver ativa.</summary>
        private static Vector3 GetSpawnOrigin(Player player)
        {
            Vector3 defaultOrigin = player.transform.position + player.transform.forward * 2.5f + Vector3.up;
            if (Camera.main == null) return defaultOrigin;

            float camDist = Vector3.Distance(Camera.main.transform.position, player.transform.position);
            if (camDist > 6f)
            {
                Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
                RaycastHit[] hits = Physics.RaycastAll(ray, 500f);
                Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                foreach (RaycastHit hit in hits)
                {
                    if (hit.collider != null && hit.collider.transform.root == player.transform) continue;
                    return hit.point + Vector3.up * 0.5f;
                }
                return Camera.main.transform.position + Camera.main.transform.forward * 10f;
            }
            return defaultOrigin;
        }

        /// <summary>Gera a criatura escolhida na posição do jogador ou para onde a câmera livre está apontando.</summary>
        private void Spawn(string prefabName)
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                SkCommandProcessor.Notify("Nenhum jogador encontrado.");
                return;
            }
            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefabName) : null;
            if (prefab == null)
            {
                SkCommandProcessor.Notify("Criatura não encontrada: " + prefabName);
                return;
            }

            int wanted = Mathf.Clamp(count, 1, MaxCount);
            int spawned = 0;
            Vector3 origin = GetSpawnOrigin(player);

            for (int i = 0; i < wanted; i++)
            {
                try
                {
                    float angle = (wanted <= 1) ? 0f : (360f / wanted) * i;
                    float radius = (wanted <= 1) ? 0f : Mathf.Clamp(wanted * 0.25f, 1f, 4f);
                    Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius;

                    GameObject instance = UnityEngine.Object.Instantiate(prefab, origin + offset, Quaternion.identity);
                    if (instance == null)
                    {
                        continue;
                    }
                    spawned++;

                    Character character = instance.GetComponent<Character>();
                    if (character != null && level > 1)
                    {
                        character.SetLevel(Mathf.Clamp(level, 1, MaxLevel));
                    }
                    if (tamed)
                    {
                        MonsterAI ai = instance.GetComponent<MonsterAI>();
                        if (ai != null)
                        {
                            ai.MakeTame();
                        }
                        else if (character != null)
                        {
                            character.SetTamed(true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    SkUtilities.Logz(new string[] { "SPAWN", "ERROR" }, new string[] { ex.Message }, LogType.Error);
                }
            }

            SkCommandProcessor.Notify(spawned > 0
                ? "Gerado(s) " + spawned + "x " + prefabName + (level > 1 ? " (nível " + level + ")" : "") + (tamed ? ", domesticado" : "")
                : "Não foi possível gerar " + prefabName);
        }
    }
}
