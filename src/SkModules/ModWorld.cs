using System;
using System.Collections.Generic;
using SkToolbox.Utility;
using UnityEngine;
using SkScope = SkToolbox.SkMenuController.SkScope;

namespace SkToolbox.SkModules
{
    /// <summary>
    /// Speelo's Toolbox: a guia Mundo (World). Agrupada em seções para cobrir desde
    /// terraformagem até regras de mundo, chaves de progressão, viagem, limpeza e administração do servidor.
    /// </summary>
    internal class ModWorld : SkBaseModule, IModule
    {
        private int radius = 5;
        private int height = 5;
        private int timeOfDay = 50;      // percent of a day, sent to /tod as 0..1
        private int windAngle = 0;       // degrees
        private int windIntensity = 50;  // percent, sent to /wind as 0..1

        public ModWorld() : base()
        {
            base.ModuleName = "World";
            base.Loading();
        }

        public void Start()
        {
            BeginMenu();
            base.Ready(); // Must be called when the module has completed initialization.
        }

        /// <summary>This tab renders an icon grid rather than a list, so the normal item list stays empty.</summary>
        public void BeginMenu()
        {
            MenuOptions = new SkMenu();
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>Runs one of the toolbox's own commands, which are registered with a leading slash.</summary>
        private static void Run(string command)
        {
            SkCommandProcessor.ProcessCommand(command, SkCommandProcessor.LogTo.Chat);
        }

        /// <summary>Formats a 0..1 value the way the console expects, regardless of the machine's decimal comma.</summary>
        private static string Fraction(int percent)
        {
            return (percent / 100f).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static SkMenuController.SkGridItem Cell(string label, string tip, Sprite icon, Action run, SkScope scope)
        {
            return Cell(null, label, tip, icon, run, scope);
        }

        private static SkMenuController.SkGridItem Cell(string section, string label, string tip, Sprite icon, Action run, SkScope scope)
        {
            return new SkMenuController.SkGridItem
            {
                Name = label,
                Display = label,
                Tip = label + "  -  " + tip,
                Icon = icon,
                Section = section,
                Scope = scope,
                OnClick = (string ignored) => run(),
            };
        }

        /// <summary>A cell that lights up while the thing it controls is on.</summary>
        private static SkMenuController.SkGridItem Toggle(string section, string label, string tip, Sprite icon, Action run, Func<bool> isOn, SkScope scope)
        {
            SkMenuController.SkGridItem cell = Cell(section, label, tip, icon, run, scope);
            cell.IsOn = isOn;
            return cell;
        }

        /// <summary>True when this game is the one running the world, rather than a client joined to someone else's.</summary>
        private static bool IsWorldHost()
        {
            return ZNet.instance != null && ZNet.instance.IsServer();
        }

        /// <summary>
        /// The game's event commands are marked onlyServer and are not forwarded to a server the way the world-key
        /// commands are. Our IsValid patch would happily run them on a client, but RandEventSystem is driven by
        /// whoever runs the world, so the change is discarded on the next sync. Saying why beats a dead button.
        /// </summary>
        private static void RunEvent(string command, string done)
        {
            if (!IsWorldHost())
            {
                SkCommandProcessor.Notify("Apenas o anfitrião ou o servidor pode alterar eventos.");
                return;
            }
            SkRun.CmdNotify(command, done);
        }

        private SkMenuController.SkForm NewForm(string title, string note)
        {
            return new SkMenuController.SkForm { Title = title, Note = note };
        }

        private static SkMenuController.SkFormField Choice(string id, string label, List<string> options, List<string> labels, int height)
        {
            SkMenuController.SkFormField field = new SkMenuController.SkFormField
            {
                Id = id,
                Label = label,
                Kind = SkMenuController.SkFieldKind.Choice,
                Height = height,
            };
            if (options != null) field.Options.AddRange(options);
            if (labels != null) field.OptionLabels.AddRange(labels);
            return field;
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
                IntValue = value,
            };
        }

        private static SkMenuController.SkFormField TextBox(string id, string label, string value)
        {
            return new SkMenuController.SkFormField
            {
                Id = id,
                Label = label,
                Kind = SkMenuController.SkFieldKind.Text,
                TextValue = value ?? "",
            };
        }

        /// <summary>The tick a destructive command has to have before it will run.</summary>
        private static SkMenuController.SkFormField ConfirmBox(string label)
        {
            return new SkMenuController.SkFormField
            {
                Id = "confirm",
                Label = label,
                Kind = SkMenuController.SkFieldKind.Toggle,
            };
        }

        private static readonly Func<SkMenuController.SkForm, string> ConfirmRequired =
            (SkMenuController.SkForm f) =>
            {
                SkMenuController.SkFormField box = f.Field("confirm");
                return (box != null && !box.BoolValue) ? "Ative a chave de confirmação primeiro." : null;
            };

        private static List<string> EnumNames(Type type, params string[] skip)
        {
            List<string> names = new List<string>();
            foreach (string name in Enum.GetNames(type))
            {
                bool skipped = false;
                foreach (string unwanted in skip)
                {
                    if (string.Equals(name, unwanted, StringComparison.Ordinal)) { skipped = true; break; }
                }
                if (!skipped) names.Add(name);
            }
            return names;
        }

        // ------------------------------------------------------------------ root

        internal void ShowGrid()
        {
            List<SkMenuController.SkGridItem> grid = new List<SkMenuController.SkGridItem>
            {
                // -------- terreno
                Cell("Terreno", "Ferramentas de Terreno", "Nivelar, elevar, escavar ou desfazer alterações no terreno ao seu redor",
                     SkIcons.First("Hoe"), ShowTerrain, SkScope.Server),
                Cell("Construção", "Reparar Próximos", "Repara todas as peças de construção ao seu redor",
                     SkIcons.First("Hammer"), ShowRepairForm, SkScope.Server),
                Cell("Construção", "Reabastecer Fogo", "Reabastece tochas, fogueiras e lareiras ao seu redor",
                     SkIcons.First("Resin", "Coal"), ShowRefuelForm, SkScope.Server),

                Cell("Terreno", "Otimizar Terreno", "Compacta modificações antigas do terreno para melhorar o desempenho",
                     SkIcons.First("PickaxeIron", "PickaxeStone", "PickaxeAntler"), () => Run("/optterrain"), SkScope.Server),

                // -------- tempo e clima
                Cell("Tempo e Clima", "Clima", "Força um tipo de clima ou devolve o controle ao jogo",
                     SkIcons.First("Thunderstone", "Ruby"), ShowWeather, SkScope.Client),
                Cell("Tempo e Clima", "Hora do Dia", "Define e trava a hora do dia, ou a destrava",
                     SkIcons.First("SurtlingCore", "Coins"), ShowTime, SkScope.Client),
                Cell("Tempo e Clima", "Vento", "Define a direção e intensidade do vento, ou o reseta",
                     SkIcons.First("Feathers"), ShowWind, SkScope.Client),
                Cell("Tempo e Clima", "Eventos", "Inicia ou interrompe um evento aleatório",
                     SkIcons.First("TrophyEikthyr", "Wishbone"), ShowEvents, SkScope.HostOnly),
                Cell("Tempo e Clima", "Dormir", "Avança direto para a manhã seguinte",
                     SkIcons.First("DeerHide", "LeatherScraps"), () => SkRun.CmdNotify("sleep", "Avançou para a manhã."), SkScope.Admin),
                Cell("Tempo e Clima", "Avançar Tempo", "Avança no tempo por um número de horas do jogo",
                     SkIcons.First("Coins", "Ruby"), ShowSkipTimeForm, SkScope.Admin),
                Cell("Tempo e Clima", "Velocidade do Jogo", "Acelera ou desacelera todo o mundo do jogo",
                     SkIcons.First("GreydwarfEye", "SurtlingCore"), ShowTimescaleForm, SkScope.Client),

                // -------- regras
                Cell("Regras", "Dificuldade", "Ajusta os inimigos como se houvesse uma quantidade definida de jogadores próximos",
                     SkIcons.First("TrophyDeer", "Coins"), ShowDifficultyForm, SkScope.Admin),
                Toggle("Regras", "Sem Inimigos (No Spawn)", "Impede que criaturas gerem naturalmente",
                       SkIcons.First("TrophyGreydwarf", "GreydwarfEye"),
                       () => SkRun.CmdNotify("nospawn", "Invocação natural alternada."),
                       () => SpawnSystem.m_nospawn, SkScope.Client),
                Toggle("Regras", "Sem Mapa", "Desativa o mapa para este personagem",
                       SkIcons.First("Chain", "LeatherScraps"),
                       () => SkRun.CmdNotify("nomap", "Mapa alternado."),
                       () => ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoMap), SkScope.Client),
                Toggle("Regras", "Sem Portais", "Desativa portais para este mundo",
                       SkIcons.First("SurtlingCore"),
                       () => SkRun.CmdNotify("noportals", "Portais alternados."),
                       () => ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoPortals), SkScope.Server),
                Cell("Regras", "Modificador do Mundo", "Altera um modificador do mundo, com as mesmas opções da tela de configurações",
                     SkIcons.First("Bronze", "Copper"), ShowWorldModifierForm, SkScope.Admin),
                Cell("Regras", "Predefinição do Mundo", "Aplica uma predefinição completa de dificuldade, substituindo todos os modificadores",
                     SkIcons.First("Iron", "Bronze"), ShowWorldPresetForm, SkScope.Admin),

                // -------- progressao
                Cell("Progressão", "Definir Chave", "Marca um chefe como derrotado ou define outra chave global",
                     SkIcons.First("CryptKey", "Wishbone"), ShowSetKeyForm, SkScope.Admin),
                Cell("Progressão", "Remover Chave", "Remove uma chave do mundo ativada",
                     SkIcons.First("Flint", "Stone"), ShowRemoveKeyForm, SkScope.Admin),
                Cell("Progressão", "Listar Chaves", "Exibe todas as chaves definidas neste mundo e personagem",
                     SkIcons.First("Coins"), () => SkRun.Show("Chaves do Mundo", "listkeys", "Nenhuma chave definida."), SkScope.Admin),
                Cell("Progressão", "Redefinir Chaves do Mundo", "Restaura todos os modificadores do mundo para o padrão",
                     SkIcons.First("Ruby"), ShowResetWorldKeysForm, SkScope.Admin),
                Cell("Progressão", "Chaves do Jogador", "Adiciona ou remove uma chave no seu personagem em vez do mundo",
                     SkIcons.First("Amber", "Coins"), ShowPlayerKeyForm, SkScope.Client),

                // -------- viagem
                Cell("Viagem", "Ir Para", "Teleporta para uma coordenada do mapa",
                     SkIcons.First("SurtlingCore", "Thunderstone"), ShowGoToForm, SkScope.HostOnly),
                Cell("Viagem", "Puxar Jogadores", "Puxa todos os outros jogadores para perto de você",
                     SkIcons.First("Chain", "LeatherScraps"), () => SkRun.CmdNotify("recall", "Jogadores puxados."), SkScope.HostOnly),
                Cell("Viagem", "Buscar", "Marca objetos e locais carregados no mapa, ou teleporta para o mais próximo",
                     SkIcons.First("Wishbone"), ShowFindForm, SkScope.Client),
                Cell("Viagem", "Buscar Bioma", "Marca um bioma no mapa, ou teleporta para ele",
                     SkIcons.First("Raspberry", "Blueberries"), ShowFindBiomeForm, SkScope.Client),
                Cell("Viagem", "Listar Locais", "Exibe os locais carregados ao seu redor",
                     SkIcons.First("Stone", "Wood"), () => SkRun.Show("Locais Carregados", "printlocations", "Nenhum local carregado."), SkScope.Client),
                Cell("Viagem", "Revelar Mapa", "Explora o minimapa inteiro",
                     SkIcons.First("Torch"), () => Run("/revealmap"), SkScope.Client),
                Cell("Viagem", "Revelar ao Meu Redor", "Explora o mapa até uma distância escolhida, em vez dele todo",
                     SkIcons.First("Coal", "Resin"), ShowRevealForm, SkScope.Client),
                Cell("Viagem", "Redefinir Mapa", "Apaga a exploração do mapa",
                     SkIcons.First("Coins", "Ruby"), () => Run("/resetmap"), SkScope.Client),
                Cell("Viagem", "Listar Portais", "Imprime todas as tags de portais",
                     SkIcons.First("GreydwarfEye", "SurtlingCore"), () => Run("/portals"), SkScope.Client),
                Cell("Viagem", "Exibir Semente", "Imprime a semente do mundo",
                     SkIcons.First("Barley", "Carrot"), () => Run("/seed"), SkScope.Client),

                // -------- limpeza
                // killenemycreatures elimina criaturas e para por aí. killenemies também varre todas as SpawnArea.
                Cell("Limpeza", "Eliminar Inimigos", "Elimina todas as criaturas hostis próximas. Mantém os ninhos intocados",
                     SkIcons.First("SwordIron", "SwordBronze", "AxeStone"), () => SkRun.CmdNotify("killenemycreatures", "Inimigos próximos eliminados."), SkScope.Server),
                Cell("Limpeza", "Eliminar Inimigos e Ninhos", "Também destrói todos os geradores de criaturas em todas as zonas carregadas, permanentemente",
                     SkIcons.First("PickaxeIron", "SledgeStagbreaker", "Club"), ShowKillSpawnersForm, SkScope.Server),
                Cell("Limpeza", "Eliminar Domesticados", "Elimina todas as criaturas domesticadas próximas",
                     SkIcons.First("Carrot", "Raspberry"), () => SkRun.CmdNotify("killtame", "Criaturas domesticadas próximas eliminadas."), SkScope.Server),
                Cell("Limpeza", "Remover Itens no Chão", "Limpa os itens caídos no chão na área",
                     SkIcons.First("Wood"), () => Run("/removedrops"), SkScope.Server),
                Cell("Limpeza", "Remover Peixes", "Limpa os peixes na área",
                     SkIcons.First("FishRaw", "FishingRod"), () => SkRun.CmdNotify("removefish", "Pássaros próximos removidos."), SkScope.Server),
                Cell("Limpeza", "Remover Pássaros", "Limpa os pássaros na área",
                     SkIcons.First("Feathers"), () => SkRun.CmdNotify("removebirds", "Pássaros próximos removidos."), SkScope.Server),
                Cell("Limpeza", "Apagar Fogo", "Apaga o fogo e a fumaça se espalhando",
                     SkIcons.First("Resin", "Coal"), () => SkRun.CmdNotify("stopfire", "Fogo em propagação apagado."), SkScope.HostOnly),
                Cell("Limpeza", "Apagar Fumaça", "Limpa a fumaça acumulada",
                     SkIcons.First("Coal", "Resin"), () => SkRun.CmdNotify("stopsmoke", "Fumaça limpa."), SkScope.HostOnly),
                Cell("Limpeza", "Salvar Mundo", "Força o salvamento do mundo agora",
                     SkIcons.First("Coins", "Ruby"), () => SkRun.CmdNotify("save", "Mundo salvo."), SkScope.Admin),

                // -------- servidor
                Cell("Servidor", "Expulsar (Kick)", "Desconecta um jogador",
                     SkIcons.First("Club", "AxeStone"), () => ShowUserForm("Expulsar Jogador", "kick", "Expulsar"), SkScope.Admin),
                Cell("Servidor", "Banir", "Bane um jogador deste mundo",
                     SkIcons.First("SledgeStagbreaker", "Club"), () => ShowUserForm("Banir Jogador", "ban", "Banir"), SkScope.Admin),
                Cell("Servidor", "Desbanir", "Remove o banimento por IP ou ID de usuário",
                     SkIcons.First("MeadHealthMinor", "Honey"), ShowUnbanForm, SkScope.Admin),
                Cell("Servidor", "Quem está Online", "Lista os jogadores conectados no momento",
                     SkIcons.First("Amber", "Coins"), () => SkRun.Show("Jogadores Online", "/whois", "Ninguém mais está conectado."), SkScope.Client),
                Cell("Servidor", "Lista de Banidos", "Exibe quem está banido",
                     SkIcons.First("Coins"), () => SkRun.Show("Usuários Banidos", "banned", "Ninguém está banido."), SkScope.Admin),
                Cell("Servidor", "Ping", "Mede o tempo de resposta até o servidor",
                     SkIcons.First("Feathers"), () => SkRun.CmdNotify("ping", "Ping enviado. A resposta será exibida no console."), SkScope.Client),
            };

            // Long o suficiente para valer a busca; o filtro mantém as caixas de seção que combinam.
            SkMC.RequestGridMenu(grid, (List<SkMenuController.SkMenuSlider>)null, "World", showFilter: true);
        }

        // ------------------------------------------------------------------ terreno

        private void ShowTerrain()
        {
            List<SkMenuController.SkGridItem> grid = new List<SkMenuController.SkGridItem>
            {
                Cell("Nivelar Terreno", "Nivela o terreno na sua altura, usando o Raio", SkIcons.First("Hoe"),
                     () => Run("/tl " + radius), SkScope.Server),
                Cell("Elevar Terreno", "Eleva o terreno, usando o Raio e a Altura", SkIcons.First("Stone", "Wood"),
                     () => Run("/tr " + radius + " " + height), SkScope.Server),
                Cell("Escavar Terreno", "Escava o terreno, usando o Raio e a Altura", SkIcons.First("PickaxeIron", "PickaxeStone", "PickaxeAntler"),
                     () => Run("/td " + radius + " " + height), SkScope.Server),
                Cell("Desfazer Edições de Terreno", "Reverte as modificações de terreno para o estado original, usando o Raio", SkIcons.First("Cultivator"),
                     () => Run("/tu " + radius), SkScope.Server),
            };

            List<SkMenuController.SkMenuSlider> sliders = new List<SkMenuController.SkMenuSlider>
            {
                new SkMenuController.SkMenuSlider
                {
                    Label = "Raio", Min = 1, Max = 50,
                    Get = () => radius, Set = (int value) => radius = value,
                },
                new SkMenuController.SkMenuSlider
                {
                    Label = "Altura", Min = 1, Max = 8,
                    Get = () => height, Set = (int value) => height = value,
                },
            };

            SkMC.RequestGridMenu(grid, sliders, "Terrain", showFilter: false);
        }

        // ------------------------------------------------------------------ clima

        private void ShowWeather()
        {
            SkMenu menu = new SkMenu();
            menu.AddItem("Deixar o jogo controlar o clima", new Action(() => Run("/env -1")), "Para de forçar um tipo de clima");

            if (EnvMan.instance != null && EnvMan.instance.m_environments != null)
            {
                List<string> names = new List<string>();
                foreach (EnvSetup setup in EnvMan.instance.m_environments)
                {
                    if (setup != null && !string.IsNullOrEmpty(setup.m_name))
                    {
                        names.Add(setup.m_name);
                    }
                }
                names.Sort(StringComparer.OrdinalIgnoreCase);
                foreach (string name in names)
                {
                    string captured = name;
                    menu.AddItem(captured, new Action(() => Run("/env " + captured)), "Forçar clima: " + captured);
                }
            }
            else
            {
                menu.AddItem("Lista de climas indisponível", new Action(() => { }), "Entre em um mundo primeiro");
            }

            SkMC.RequestSubMenu(menu.FlushMenu());
        }

        // ------------------------------------------------------------------ hora do dia

        private void ShowTime()
        {
            List<SkMenuController.SkGridItem> grid = new List<SkMenuController.SkGridItem>
            {
                Cell("Aplicar Hora", "Define e trava a hora no valor do controle deslizante", SkIcons.First("SurtlingCore"),
                     () => Run("/tod " + Fraction(timeOfDay)), SkScope.Client),
                Cell("Manhã", "Define a hora para o nascer do sol", SkIcons.First("Coins"), () => Run("/tod 0.25"), SkScope.Client),
                Cell("Meio-dia", "Define a hora para o meio-dia", SkIcons.First("Ruby"), () => Run("/tod 0.5"), SkScope.Client),
                Cell("Noite", "Define a hora para a meia-noite", SkIcons.First("Thunderstone"), () => Run("/tod 0"), SkScope.Client),
                Cell("Destravar Hora", "Devolve o controle do relógio ao jogo", SkIcons.First("Feathers"), () => Run("/tod -1"), SkScope.Client),
            };

            List<SkMenuController.SkMenuSlider> sliders = new List<SkMenuController.SkMenuSlider>
            {
                new SkMenuController.SkMenuSlider
                {
                    Label = "Hora %", Min = 0, Max = 100,
                    Get = () => timeOfDay, Set = (int value) => timeOfDay = value,
                },
            };

            SkMC.RequestGridMenu(grid, sliders, "Time of Day", showFilter: false);
        }

        /// <summary>The game counts time in seconds, so the slider is converted using this world's day length.</summary>
        private void ShowSkipTimeForm()
        {
            SkMenuController.SkForm form = NewForm("Avançar Tempo", "Avança o relógio do jogo. Tudo o que depende do tempo avança junto, incluindo plantações, comidas e invasões.");
            form.Fields.Add(Slider("hours", "Horas do jogo", 1, 72, 12));
            form.Actions.Add(new SkMenuController.SkFormAction
            {
                Label = "Avançar",
                Run = (SkMenuController.SkForm f) =>
                {
                    long dayLength = EnvMan.instance != null ? EnvMan.instance.m_dayLengthSec : 1200L;
                    int seconds = Mathf.RoundToInt(f.Field("hours").IntValue * (dayLength / 24f));
                    SkRun.CmdNotify("skiptime " + seconds, "Avançou " + f.Field("hours").IntValue + " horas.");
                },
            });
            SkMC.ShowForm(form);
        }

        private void ShowTimescaleForm()
        {
            SkMenuController.SkForm form = NewForm("Velocidade do Jogo", "Altera a velocidade de execução do mundo. 100% é o normal.");
            form.Fields.Add(Slider("speed", "Velocidade %", 25, 500, 100));
            form.Actions.Add(new SkMenuController.SkFormAction
            {
                Label = "Aplicar",
                Run = (SkMenuController.SkForm f) =>
                {
                    SkRun.Cmd("timescale " + Fraction(f.Field("speed").IntValue) + " 1");
                    SkCommandProcessor.Notify("Velocidade do jogo: " + f.Field("speed").IntValue + "%");
                },
            });
            form.Actions.Add(new SkMenuController.SkFormAction
            {
                Label = "Voltar ao normal",
                Run = (SkMenuController.SkForm f) =>
                {
                    SkRun.Cmd("timescale 1 1");
                    SkCommandProcessor.Notify("Velocidade do jogo voltou ao normal.");
                },
            });
            SkMC.ShowForm(form);
        }

        // ------------------------------------------------------------------ vento

        private void ShowWind()
        {
            List<SkMenuController.SkGridItem> grid = new List<SkMenuController.SkGridItem>
            {
                Cell("Aplicar Vento", "Define o vento para os valores do controle deslizante", SkIcons.First("Feathers"),
                     () => Run("/wind " + windAngle + " " + Fraction(windIntensity)), SkScope.Client),
                Cell("Resetar Vento", "Devolve o controle do vento ao jogo", SkIcons.First("Thunderstone"), () => Run("/resetwind"), SkScope.Client),
            };

            List<SkMenuController.SkMenuSlider> sliders = new List<SkMenuController.SkMenuSlider>
            {
                new SkMenuController.SkMenuSlider
                {
                    Label = "Ângulo", Min = 0, Max = 360,
                    Get = () => windAngle, Set = (int value) => windAngle = value,
                },
                new SkMenuController.SkMenuSlider
                {
                    Label = "Intensidade %", Min = 0, Max = 100,
                    Get = () => windIntensity, Set = (int value) => windIntensity = value,
                },
            };

            SkMC.RequestGridMenu(grid, sliders, "Wind", showFilter: false);
        }

        // ------------------------------------------------------------------ eventos

        private void ShowEvents()
        {
            if (RandEventSystem.instance == null || RandEventSystem.instance.m_events == null)
            {
                SkCommandProcessor.Notify("A lista de eventos ainda não está disponível. Entre em um mundo primeiro.");
                return;
            }

            List<string> names = new List<string>();
            List<string> labels = new List<string>();
            foreach (RandomEvent randomEvent in RandEventSystem.instance.m_events)
            {
                if (randomEvent == null || string.IsNullOrEmpty(randomEvent.m_name)) continue;
                names.Add(randomEvent.m_name);
                labels.Add(randomEvent.m_name + EventBlurb(randomEvent));
            }
            if (names.Count == 0)
            {
                SkCommandProcessor.Notify("Este mundo não possui eventos registrados.");
                return;
            }
            SortPairs(names, labels);

            SkMenuController.SkForm form = NewForm("Eventos", IsWorldHost()
                ? "Invasões surgem ao seu redor e duram um tempo definido. Iniciar uma substitui qualquer evento em andamento."
                : "Apenas o anfitrião ou servidor pode alterar eventos. Estas opções serão recusadas se você for um cliente.");
            form.Fields.Add(Choice("event", "Evento", names, labels, 220));
            form.Actions.Add(new SkMenuController.SkFormAction
            {
                Label = "Iniciar evento",
                Run = (SkMenuController.SkForm f) =>
                {
                    string name = f.Field("event").SelectedOption;
                    if (string.IsNullOrEmpty(name)) return;
                    RunEvent("event " + name, "Evento iniciado: " + name);
                },
            });
            form.Actions.Add(new SkMenuController.SkFormAction
            {
                Label = "Aleatório",
                Run = (SkMenuController.SkForm f) => RunEvent("randomevent", "Evento aleatório iniciado."),
            });
            form.Actions.Add(new SkMenuController.SkFormAction
            {
                Label = "Interromper atual",
                Run = (SkMenuController.SkForm f) => RunEvent("stopevent", "Evento atual interrompido."),
            });
            SkMC.ShowForm(form);
        }

        private static string EventBlurb(RandomEvent randomEvent)
        {
            try
            {
                if (Localization.instance != null && !string.IsNullOrEmpty(randomEvent.m_startMessage))
                {
                    string localized = Localization.instance.Localize(randomEvent.m_startMessage);
                    if (!string.IsNullOrEmpty(localized) && !localized.StartsWith("["))
                    {
                        return "     <color=#9FB6CC>" + localized + "</color>";
                    }
                }
            }
            catch (Exception)
            {
            }
            return "";
        }

        private static void SortPairs(List<string> values, List<string> labels)
        {
            int[] order = new int[values.Count];
            for (int i = 0; i < order.Length; i++) order[i] = i;
            Array.Sort(order, (int a, int b) => string.Compare(values[a], values[b], StringComparison.OrdinalIgnoreCase));

            List<string> sortedValues = new List<string>(values.Count);
            List<string> sortedLabels = new List<string>(labels.Count);
            foreach (int index in order)
            {
                sortedValues.Add(values[index]);
                sortedLabels.Add(labels[index]);
            }
            values.Clear();
            values.AddRange(sortedValues);
            labels.Clear();
            labels.AddRange(sortedLabels);
        }

        private void ShowKillSpawnersForm()
        {
            SkMenuController.SkForm form = NewForm("Eliminar Inimigos e Ninhos",
                "Geradores não regeneram. Todos neste mundo os perderão, e a única forma de recuperar é usando um mundo onde essas zonas nunca foram carregadas.");
            form.Warning = "Isso DESTRÓI todos os geradores de criaturas em todas as zonas carregadas.";
            form.Fields.Add(ConfirmBox("Destruir os geradores também"));
            form.Validate = ConfirmRequired;
            form.Actions.Add(new SkMenuController.SkFormAction
            {
                Label = "Eliminar e destruir",
                Run = (SkMenuController.SkForm f) => SkRun.CmdNotify("killenemies", "Inimigos e geradores próximos eliminados."),
            });
            SkMC.ShowForm(form);
        }

        private void ShowRepairForm()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                SkCommandProcessor.Notify("Nenhum jogador encontrado.");
                return;
            }

            SkMenuController.SkForm form = NewForm("Reparar Próximos",
                "Repara estruturas desgastadas e danificadas ao seu redor. Peças intactas são ignoradas.");
            form.Fields.Add(Slider("radius", "Raio", 5, 100, 30));
            form.Actions.Add(new SkMenuController.SkFormAction
            {
                Label = "Reparar",
                Run = (SkMenuController.SkForm f) =>
                {
                    Player lp = Player.m_localPlayer;
                    if (lp == null) { SkCommandProcessor.Notify("Nenhum jogador encontrado."); return; }

                    float radius = f.Field("radius").IntValue;
                    Vector3 origin = lp.transform.position;
                    int repaired = 0;

                    WearNTear[] pieces = UnityEngine.Object.FindObjectsByType<WearNTear>(FindObjectsSortMode.None);
                    foreach (WearNTear piece in pieces)
                    {
                        if (piece == null) continue;
                        if (Vector3.Distance(piece.transform.position, origin) > radius) continue;
                        try
                        {
                            if (piece.Repair()) repaired++;
                        }
                        catch (Exception)
                        {
                        }
                    }

                    SkCommandProcessor.Notify(repaired > 0
                        ? "Reparou " + repaired + " peças."
                        : "Nada próximo precisava de reparos.");
                },
            });
            SkMC.ShowForm(form);
        }

        private void ShowRefuelForm()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                SkCommandProcessor.Notify("Nenhum jogador encontrado.");
                return;
            }

            SkMenuController.SkForm form = NewForm("Reabastecer Fogo",
                "Reabastece tochas, fogueiras e lareiras ao seu redor até o máximo. As que já estão cheias são mantidas.");
            form.Fields.Add(Slider("radius", "Raio", 5, 100, 30));
            form.Actions.Add(new SkMenuController.SkFormAction
            {
                Label = "Reabastecer próximos",
                Run = (SkMenuController.SkForm f) => Refuel(f.Field("radius").IntValue, true),
            });
            form.Actions.Add(new SkMenuController.SkFormAction
            {
                Label = "Apagar",
                Run = (SkMenuController.SkForm f) => Refuel(f.Field("radius").IntValue, false),
            });
            SkMC.ShowForm(form);
        }

        private static void Refuel(float radius, bool fill)
        {
            Player lp = Player.m_localPlayer;
            if (lp == null) { SkCommandProcessor.Notify("Nenhum jogador encontrado."); return; }

            Vector3 origin = lp.transform.position;
            int changed = 0;

            Fireplace[] fires = UnityEngine.Object.FindObjectsByType<Fireplace>(FindObjectsSortMode.None);
            foreach (Fireplace fire in fires)
            {
                if (fire == null) continue;
                if (Vector3.Distance(fire.transform.position, origin) > radius) continue;
                try
                {
                    ZNetView view = SkUtilities.GetPrivateField<ZNetView>(fire, "m_nview");
                    if (view == null || !view.IsValid()) continue;
                    float now = view.GetZDO().GetFloat(ZDOVars.s_fuel);
                    float target = fill ? fire.m_maxFuel : 0f;
                    if (Mathf.Approximately(now, target)) continue;
                    fire.SetFuel(target);
                    changed++;
                }
                catch (Exception)
                {
                }
            }

            if (changed > 0)
            {
                SkCommandProcessor.Notify((fill ? "Reabasteceu " : "Apagou ") + changed + " próximos.");
            }
            else
            {
                SkCommandProcessor.Notify(fill ? "Nada próximo precisava de combustível." : "Nada próximo estava aceso.");
            }
        }

        private static System.Reflection.MethodInfo exploreAround;

        private void ShowRevealForm()
        {
            if (Player.m_localPlayer == null || Minimap.instance == null)
            {
                SkCommandProcessor.Notify("Nenhum jogador encontrado.");
                return;
            }

            SkMenuController.SkForm form = NewForm("Revelar ao Meu Redor",
                "Revela o mapa até esta distância a partir de onde você está. O mundo inteiro tem cerca de 10000m de extensão, então algumas centenas é uma boa distância.");
            form.Fields.Add(Slider("radius", "Distância", 50, 3000, 500));
            form.Actions.Add(new SkMenuController.SkFormAction
            {
                Label = "Revelar",
                Run = (SkMenuController.SkForm f) =>
                {
                    Player lp = Player.m_localPlayer;
                    if (lp == null || Minimap.instance == null) { SkCommandProcessor.Notify("Nenhum jogador encontrado."); return; }

                    if (exploreAround == null)
                    {
                        exploreAround = typeof(Minimap).GetMethod(
                            "Explore",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                            null,
                            new Type[] { typeof(Vector3), typeof(float) },
                            null);
                    }
                    if (exploreAround == null)
                    {
                        SkCommandProcessor.Notify("Esta versão do jogo não suporta revelação parcial de mapa.");
                        return;
                    }

                    int radius = f.Field("radius").IntValue;
                    exploreAround.Invoke(Minimap.instance, new object[] { lp.transform.position, (float)radius });
                    SkCommandProcessor.Notify("Mapa revelado dentro de " + radius + "m.");
                },
            });
            SkMC.ShowForm(form);
        }

        // ------------------------------------------------------------------ regras

        private void ShowDifficultyForm()
        {
            SkMenuController.SkForm form = NewForm("Forçar Dificuldade",
                "Ajusta a vida e dano dos inimigos como se houvesse essa quantidade de jogadores próximos. 0 devolve o controle ao jogo.");
            form.Fields.Add(Slider("players", "Jogadores", 0, 10, 1));
            form.Actions.Add(new SkMenuController.SkFormAction
            {
                Label = "Aplicar",
                Run = (SkMenuController.SkForm f) =>
                    SkRun.CmdNotify("players " + f.Field("players").IntValue, "Dificuldade definida."),
            });
            SkMC.ShowForm(form);
        }

        private void ShowWorldModifierForm()
        {
            SkMenuController.SkForm form = NewForm("Modificador do Mundo",
                "As mesmas opções fornecidas na tela de configurações do mundo. Afetam a todos neste mundo.");
            form.Fields.Add(Choice("modifier", "Modificador", EnumNames(typeof(WorldModifiers), "Default"), null, 120));
            form.Fields.Add(Choice("value", "Valor", EnumNames(typeof(WorldModifierOption)), null, 120));
            form.Actions.Add(new SkMenuController.SkFormAction
            {
                Label = "Aplicar",
                Run = (SkMenuController.SkForm f) =>
                {
                    string modifier = f.Field("modifier").SelectedOption;
                    string value = f.Field("value").SelectedOption;
                    if (string.IsNullOrEmpty(modifier) || string.IsNullOrEmpty(value)) return;
                    SkRun.Cmd("setworldmodifier " + modifier + " " + value);
                    SkCommandProcessor.Notify(modifier + ": " + value);
                },
            });
            SkMC.ShowForm(form);
        }

        private void ShowWorldPresetForm()
        {
            SkMenuController.SkForm form = NewForm("Predefinição do Mundo",
                "Substitui todos os modificadores do mundo pelos valores da predefinição. Ajustes manuais anteriores serão perdidos.");
            form.Fields.Add(Choice("preset", "Predefinição", EnumNames(typeof(WorldPresets), "Custom"), null, 150));
            form.Fields.Add(ConfirmBox("Substituir todos os modificadores atuais"));
            form.Validate = ConfirmRequired;
            form.Actions.Add(new SkMenuController.SkFormAction
            {
                Label = "Aplicar predefinição",
                Run = (SkMenuController.SkForm f) =>
                {
                    string preset = f.Field("preset").SelectedOption;
                    if (string.IsNullOrEmpty(preset)) return;
                    SkRun.Cmd("setworldpreset " + preset);
                    SkCommandProcessor.Notify("Predefinição do mundo: " + preset);
                },
            });
            SkMC.ShowForm(form);
        }

        // ------------------------------------------------------------------ chaves de progressao

        private static List<string> KnownKeys()
        {
            List<string> keys = EnumNames(typeof(GlobalKeys), "NonServerOption", "Count", "Preset");

            if (ZNetScene.instance != null && ZNetScene.instance.m_prefabs != null)
            {
                foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
                {
                    if (prefab == null) continue;
                    Character character = prefab.GetComponent<Character>();
                    if (character == null || string.IsNullOrEmpty(character.m_defeatSetGlobalKey)) continue;
                    if (!keys.Contains(character.m_defeatSetGlobalKey)) keys.Add(character.m_defeatSetGlobalKey);
                }
            }
            return keys;
        }

        private void ShowSetKeyForm()
        {
            if (ZoneSystem.instance == null)
            {
                SkCommandProcessor.Notify("Entre em um mundo primeiro.");
                return;
            }

            List<string> keys = KnownKeys();
            List<string> labels = new List<string>();
            foreach (string key in keys)
            {
                bool already = false;
                try { already = ZoneSystem.instance.GetGlobalKey(key); } catch (Exception) { }
                labels.Add(already ? key + "     <color=#7CFC00>já definida</color>" : key);
            }

            SkMenuController.SkForm form = NewForm("Definir Chave do Mundo",
                "Chaves de chefes desbloqueiam criação e alteram inimigos gerados. Isso afeta o mundo inteiro para todos.");
            form.Fields.Add(Choice("key", "Chave", keys, labels, 200));
            form.Fields.Add(ConfirmBox("Alterar este mundo para todos"));
            form.Validate = ConfirmRequired;
            form.Actions.Add(new SkMenuController.SkFormAction
            {
                Label = "Definir chave",
                Run = (SkMenuController.SkForm f) =>
                {
                    string key = f.Field("key").SelectedOption;
                    if (string.IsNullOrEmpty(key)) return;
                    SkRun.CmdNotify("setkey " + key, "Chave definida: " + key);
                },
            });
            SkMC.ShowForm(form);
        }

        private void ShowRemoveKeyForm()
        {
            List<string> keys = ZoneSystem.instance != null ? ZoneSystem.instance.GetGlobalKeys() : null;
            if (keys == null || keys.Count == 0)
            {
                SkCommandProcessor.Notify("Nenhuma chave do mundo está definida.");
                return;
            }

            SkMenuController.SkForm form = NewForm("Remover Chave do Mundo", "Apenas as chaves ativadas neste mundo estão listadas.");
            form.Fields.Add(Choice("key", "Chave", keys, null, 220));
            form.Actions.Add(new SkMenuController.SkFormAction
            {
                Label = "Remover chave",
                Run = (SkMenuController.SkForm f) =>
                {
                    string key = f.Field("key").SelectedOption;
                    if (string.IsNullOrEmpty(key)) return;
                    SkRun.CmdNotify("removekey " + key, "Chave removida: " + key);
                },
            });
            SkMC.ShowForm(form);
        }

        private void ShowResetWorldKeysForm()
        {
            SkMenuController.SkForm form = NewForm("Redefinir Chaves do Mundo",
                "Restaura todos os modificadores do mundo para o padrão. O progresso de chefes associado será redefinido.");
            form.Fields.Add(ConfirmBox("Redefinir os modificadores do mundo"));
            form.Validate = ConfirmRequired;
            form.Actions.Add(new SkMenuController.SkFormAction
            {
                Label = "Redefinir",
                Run = (SkMenuController.SkForm f) => SkRun.CmdNotify("resetworldkeys", "Chaves do mundo redefinidas.")
            });
            SkMC.ShowForm(form);
        }

        private void ShowPlayerKeyForm()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                SkCommandProcessor.Notify("Nenhum jogador encontrado.");
                return;
            }

            List<string> keys = EnumNames(typeof(PlayerKeys));
            List<string> owned = player.GetUniqueKeys();
            if (owned != null)
            {
                foreach (string key in owned)
                {
                    if (!string.IsNullOrEmpty(key) && !keys.Contains(key)) keys.Add(key);
                }
            }

            List<string> labels = new List<string>();
            foreach (string key in keys)
            {
                bool has = owned != null && owned.Contains(key);
                labels.Add(has ? key + "     <color=#7CFC00>no seu personagem</color>" : key);
            }

            SkMenuController.SkForm form = NewForm("Chaves do Jogador",
                "Chaves armazenadas no seu personagem em vez do mundo. Elas são salvas no arquivo do personagem.");
            form.Fields.Add(Choice("key", "Chave", keys, labels, 220));
            form.Actions.Add(new SkMenuController.SkFormAction
            {
                Label = "Adicionar ao jogador",
                Run = (SkMenuController.SkForm f) =>
                {
                    string key = f.Field("key").SelectedOption;
                    if (string.IsNullOrEmpty(key)) return;
                    SkRun.CmdNotify("setkeyplayer " + key, "Chave do jogador adicionada: " + key);
                },
            });
            form.Actions.Add(new SkMenuController.SkFormAction
            {
                Label = "Remover do jogador",
                Run = (SkMenuController.SkForm f) =>
                {
                    string key = f.Field("key").SelectedOption;
                    if (string.IsNullOrEmpty(key)) return;
                    SkRun.CmdNotify("removekeyplayer " + key, "Chave do jogador removida: " + key);
                },
            });
            SkMC.ShowForm(form);
        }

        // ------------------------------------------------------------------ viagem

        private void ShowGoToForm()
        {
            Player player = Player.m_localPlayer;
            string x = "0", z = "0";
            if (player != null)
            {
                x = Mathf.RoundToInt(player.transform.position.x).ToString(System.Globalization.CultureInfo.InvariantCulture);
                z = Mathf.RoundToInt(player.transform.position.z).ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            SkMenuController.SkForm form = NewForm("Ir Para", "Coordenadas do mapa, o mesmo par exibido na leitura de posição. Inicia na sua localização atual.");
            form.Fields.Add(TextBox("x", "X", x));
            form.Fields.Add(TextBox("z", "Z", z));
            form.Validate = (SkMenuController.SkForm f) =>
            {
                int ignored;
                if (!int.TryParse((f.Field("x").TextValue ?? "").Trim(), out ignored)) return "X precisa ser um número inteiro.";
                if (!int.TryParse((f.Field("z").TextValue ?? "").Trim(), out ignored)) return "Z precisa ser um número inteiro.";
                return null;
            };
            form.Actions.Add(new SkMenuController.SkFormAction
            {
                Label = "Teleportar",
                Run = (SkMenuController.SkForm f) =>
                {
                    string target = (f.Field("x").TextValue ?? "").Trim() + " " + (f.Field("z").TextValue ?? "").Trim();
                    SkRun.Cmd("goto " + target);
                    SkCommandProcessor.Notify("Teleportando para " + target);
                },
            });
            SkMC.ShowForm(form);
        }

        private void ShowFindForm()
        {
            SkMenuController.SkForm form = NewForm("Buscar",
                "Busca objetos e locais conhecidos pelo nome. Pinar marca no mapa; teleportar leva você para o mais próximo.");
            form.Fields.Add(TextBox("text", "Buscar por", ""));
            form.Fields.Add(Slider("pings", "Máximo de marcações", 1, 20, 1));
            form.Validate = (SkMenuController.SkForm f) =>
                string.IsNullOrEmpty((f.Field("text").TextValue ?? "").Trim()) ? "Digite algo para buscar." : null;
            form.Actions.Add(new SkMenuController.SkFormAction
            {
                Label = "Marcar no mapa",
                Run = (SkMenuController.SkForm f) =>
                    SkRun.CmdNotify("find " + (f.Field("text").TextValue ?? "").Trim() + " " + f.Field("pings").IntValue, "Mapa pesquisado."),
            });
            form.Actions.Add(new SkMenuController.SkFormAction
            {
                Label = "Teleportar para o mais próximo",
                Run = (SkMenuController.SkForm f) =>
                    SkRun.CmdNotify("findtp " + (f.Field("text").TextValue ?? "").Trim(), "Buscando a correspondência mais próxima."),
            });
            SkMC.ShowForm(form);
        }

        private void ShowFindBiomeForm()
        {
            SkMenuController.SkForm form = NewForm("Buscar Bioma", "Marca cada setor desse bioma no mapa, ou leva você ao mais próximo.");
            form.Fields.Add(Choice("biome", "Bioma", EnumNames(typeof(Heightmap.Biome), "None", "All", "Land"), null, 180));
            form.Actions.Add(new SkMenuController.SkFormAction
            {
                Label = "Marcar no mapa",
                Run = (SkMenuController.SkForm f) =>
                    SkRun.CmdNotify("findbiome " + f.Field("biome").SelectedOption, "Bioma marcado."),
            });
            form.Actions.Add(new SkMenuController.SkFormAction
            {
                Label = "Teleportar até lá",
                Run = (SkMenuController.SkForm f) =>
                    SkRun.CmdNotify("findbiometp " + f.Field("biome").SelectedOption, "Buscando esse bioma."),
            });
            SkMC.ShowForm(form);
        }

        // ------------------------------------------------------------------ servidor

        private static List<string> ConnectedPlayers()
        {
            List<string> names = new List<string>();
            if (ZNet.instance == null) return names;
            List<ZNetPeer> peers = ZNet.instance.GetPeers();
            if (peers == null) return names;
            foreach (ZNetPeer peer in peers)
            {
                if (peer != null && !string.IsNullOrEmpty(peer.m_playerName) && !names.Contains(peer.m_playerName))
                {
                    names.Add(peer.m_playerName);
                }
            }
            return names;
        }

        private void ShowUserForm(string title, string command, string actionLabel)
        {
            List<string> players = ConnectedPlayers();
            SkMenuController.SkForm form = NewForm(title, players.Count > 0
                ? "Escolha alguém conectado ou digite um nome, IP ou ID de usuário."
                : "Ninguém mais está conectado. Digite um nome, IP ou ID de usuário.");

            if (players.Count > 0)
            {
                form.Fields.Add(Choice("peer", "Jogadores conectados", players, null, 130));
            }
            form.Fields.Add(TextBox("name", "Ou digite nome / IP / ID de usuário", ""));
            form.Fields.Add(ConfirmBox(actionLabel + " este jogador"));
            form.Validate = (SkMenuController.SkForm f) =>
            {
                if (string.IsNullOrEmpty(UserTarget(f))) return "Escolha um jogador ou digite um nome.";
                return ConfirmRequired(f);
            };
            form.Actions.Add(new SkMenuController.SkFormAction
            {
                Label = actionLabel,
                Run = (SkMenuController.SkForm f) =>
                {
                    string target = UserTarget(f);
                    SkRun.CmdNotify(command + " " + target, actionLabel + ": " + target);
                },
            });
            SkMC.ShowForm(form);
        }

        private static string UserTarget(SkMenuController.SkForm form)
        {
            SkMenuController.SkFormField typed = form.Field("name");
            string text = typed != null ? (typed.TextValue ?? "").Trim() : "";
            if (text.Length > 0) return text;
            SkMenuController.SkFormField picker = form.Field("peer");
            return picker != null ? picker.SelectedOption : null;
        }

        private void ShowUnbanForm()
        {
            SkMenuController.SkForm form = NewForm("Desbanir", "Remove um banimento. A lista de banidos mostra o que digitar aqui.");
            form.Fields.Add(TextBox("name", "IP ou ID de usuário", ""));
            form.Validate = (SkMenuController.SkForm f) =>
                string.IsNullOrEmpty((f.Field("name").TextValue ?? "").Trim()) ? "Digite um IP ou ID de usuário." : null;
            form.Actions.Add(new SkMenuController.SkFormAction
            {
                Label = "Desbanir",
                Run = (SkMenuController.SkForm f) =>
                {
                    string target = (f.Field("name").TextValue ?? "").Trim();
                    SkRun.CmdNotify("unban " + target, "Desbanido " + target);
                },
            });
            SkMC.ShowForm(form);
        }
    }
}
