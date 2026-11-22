using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkToolbox.SkModules
{
    /// <summary>
    /// Speelo's Toolbox: a aba de entrega de itens. Exibe cada item do jogo em uma grade de ícones com campo de busca
    /// e controle deslizante de quantidade, colocando o item escolhido diretamente no inventário do jogador.
    /// </summary>
    internal class ModGive : SkBaseModule, IModule
    {
        private int quantity = 1;
        private int level = 1;
        private List<SkMenuController.SkGridItem> cache;
        private int cachedFor = 0;

        public ModGive() : base()
        {
            base.ModuleName = "Give";
            base.Loading();
        }

        public void Start()
        {
            BeginMenu();
            base.Ready(); // Deve ser chamado quando o módulo concluir a inicialização.
        }

        /// <summary>Esta aba renderiza uma grade em vez de uma lista, portanto a lista normal de itens permanece vazia.</summary>
        public void BeginMenu()
        {
            MenuOptions = new SkMenu();
        }

        /// <summary>Ponto de entrada da aba. Constrói a grade uma vez e depois a exibe.</summary>
        internal void ShowGrid()
        {
            // O ObjectDB é reconstruído a cada carregamento de mundo e os mods registram seus itens nele.
            int key = ObjectDB.instance != null ? ObjectDB.instance.GetInstanceID() : 0;
            if (cache == null || cache.Count == 0 || cachedFor != key)
            {
                cache = BuildItems();
                cachedFor = key;
            }
            if (cache.Count == 0)
            {
                SkCommandProcessor.Notify("A lista de itens ainda não está disponível. Entre em um mundo primeiro.");
                return;
            }
            List<SkMenuController.SkMenuSlider> sliders = new List<SkMenuController.SkMenuSlider>
            {
                new SkMenuController.SkMenuSlider
                {
                    Label = "Quantidade", Min = 1, Max = 100,
                    Get = () => quantity, Set = (int value) => quantity = value,
                },
                new SkMenuController.SkMenuSlider
                {
                    // Quatro é o nível máximo de melhoria de qualquer item vanilla.
                    Label = "Nível", Min = 1, Max = 4,
                    Get = () => level, Set = (int value) => level = value,
                },
            };
            SkMC.RequestGridMenu(cache, sliders, "Give", showFilter: true);
        }

        /// <summary>Cada prefab de item que possui um ItemDrop, ordenado pelo nome de exibição.</summary>
        private List<SkMenuController.SkGridItem> BuildItems()
        {
            List<SkMenuController.SkGridItem> list = new List<SkMenuController.SkGridItem>();
            if (ObjectDB.instance == null || ObjectDB.instance.m_items == null)
            {
                return list;
            }

            foreach (GameObject prefab in ObjectDB.instance.m_items)
            {
                if (prefab == null || string.IsNullOrEmpty(prefab.name))
                {
                    continue;
                }
                ItemDrop drop = prefab.GetComponent<ItemDrop>();
                if (drop == null || drop.m_itemData == null || drop.m_itemData.m_shared == null)
                {
                    continue;
                }

                Sprite icon = null;
                Sprite[] icons = drop.m_itemData.m_shared.m_icons;
                if (icons != null && icons.Length > 0)
                {
                    icon = icons[0];
                }

                string display = prefab.name;
                try
                {
                    if (Localization.instance != null && !string.IsNullOrEmpty(drop.m_itemData.m_shared.m_name))
                    {
                        string localized = Localization.instance.Localize(drop.m_itemData.m_shared.m_name);
                        if (!string.IsNullOrEmpty(localized))
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
                    Icon = icon,
                    Tip = display + "   (" + prefab.name + ")",
                    Scope = SkMenuController.SkScope.Client,
                    OnClick = Give,
                });
            }

            list.Sort((SkMenuController.SkGridItem a, SkMenuController.SkGridItem b) =>
                string.Compare(a.Display, b.Display, StringComparison.OrdinalIgnoreCase));
            return list;
        }

        /// <summary>
        /// Adiciona o item ao inventário do jogador nos pilhas (stacks) permitidos pelo item.
        /// </summary>
        private void Give(string prefabName)
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                SkCommandProcessor.Notify("Nenhum jogador encontrado.");
                return;
            }

            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefabName) : null;
            ItemDrop drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            if (drop == null || drop.m_itemData == null || drop.m_itemData.m_shared == null)
            {
                SkCommandProcessor.Notify("Item não encontrado: " + prefabName);
                return;
            }

            int maxQuality = Mathf.Max(1, drop.m_itemData.m_shared.m_maxQuality);
            int quality = Mathf.Clamp(level, 1, maxQuality);

            int remaining = Mathf.Max(1, quantity);
            int maxStack = Mathf.Max(1, drop.m_itemData.m_shared.m_maxStackSize);
            bool cheated = !PlayerProfile.s_bypassCheatChecks;
            int given = 0;

            while (remaining > 0)
            {
                int take = Mathf.Min(remaining, maxStack);
                ItemDrop.ItemData added = player.GetInventory().AddItem(
                    prefabName, take, quality, drop.m_itemData.m_variant, 0L, "", cheated, true);
                if (added == null)
                {
                    break; // sem espaço restante
                }
                given += take;
                remaining -= take;
            }

            if (given > 0)
            {
                SkCommandProcessor.Notify("Entregue " + given + "x " + prefabName
                    + (quality > 1 ? " (nível " + quality + ")" : "")
                    + (level > maxQuality ? ", o máximo suportado por este item" : "")
                    + (remaining > 0 ? " (inventário cheio)" : ""));
            }
            else
            {
                SkCommandProcessor.Notify("Inventário cheio, não foi possível entregar " + prefabName);
            }
        }
    }
}
