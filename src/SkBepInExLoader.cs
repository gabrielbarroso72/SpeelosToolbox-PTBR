using BepInEx;
using SkToolbox.Configuration;
using SkToolbox.Utility;
using System;
using UnityEngine;
// Thank you to wh0am15533 for the BepInEx examples
namespace SkToolbox
{
    [BepInPlugin(GUID, MODNAME, VERSION)]
    public class SkBepInExLoader : BaseUnityPlugin
    {
        // Speelo's Toolbox: a fork of Skrip's SkToolbox for Valheim 1.0. Distinct GUID so it never
        // collides with the original (deprecated) package if both end up in a profile.
        public const string
            MODNAME = "Speelo's Toolbox Tradução BR",
            DISPLAYNAME = "Speelo's Toolbox Tradução BR",
            AUTHOR = "Speelo",
            TRANSLATOR = "Fox72",
            ORIGINAL_AUTHOR = "Skrip",
            GUID = "com.speelo.speelostoolbox",
            VERSION = "1.1.6";

        private void Start()
        {
            // Speelo's Toolbox is a client-side menu and has nothing to offer a dedicated server. A server runs with
            // -nographics -batchmode, where there is no graphics device, no camera, no cursor and no IMGUI state.
            // Reading any of those from managed code does not raise a catchable NullReferenceException: it walks
            // straight into native Unity with a null pointer and takes the whole process down, which is what v1.1.1
            // did about two seconds into server startup. Nothing below is useful without a screen, so load nothing.
            if (Application.isBatchMode
                || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Logger.LogInfo("Servidor sem interface gráfica detectado - SpeelosToolbox é apenas para cliente e não será carregado.");
                return;
            }

            InitConfig();
            // Speelo's Toolbox: flip the achievement bypass as early as possible, before the player can spawn anything.
            SkCommandPatcher.ApplyAchievementBypass();

            base.transform.parent = null;
            UnityEngine.Object.DontDestroyOnLoad(this);
            SkLoader.InitBepThreading(this);
        }

        public void InitConfig()
        {
            try
            {
                SkConfigEntry.CDescriptor = Config.Bind("- Index", "ThisIsJustAnIndex-NotASetting", true
                    , "Seções de configuração:" +
                    "\n0 - Geral" +
                    "\n1 - Execução Automática" +
                    "\n2 - Personalizar Visual do Console" +
                    "\n3 - Apelidos de Comandos" +
                    "\n4 - Menu na Tela" +
                    "\n5 - Atalhos de Comandos" +
                    "\n");

                SkConfigEntry.CConsoleEnabled = Config.Bind("0 - General", "ConsoleEnabled", true
                    , "Ativa o console sem a necessidade de opção de inicialização na Steam.");
                SkConfigEntry.CScrollable = Config.Bind("0 - General", "ConsoleScrollable", true
                    , "Permite que a rolagem de texto seja ativada no console.");
                SkConfigEntry.CScrollableLimit = Config.Bind("0 - General", "ConsoleScrollableLimit", 500
                    , "Número máximo de linhas armazenadas no console. Padrão do jogo = 30.");
                SkConfigEntry.CConsoleAutoComplete = Config.Bind("0 - General", "AutoComplete", true
                    , "Pressione Tab para autocompletar os comandos da Caixa de Ferramentas ao digitar parcialmente.");
                SkConfigEntry.CAllowChatCommandInput = Config.Bind("0 - General", "AllowChatCommandInput", true
                    , "Alterne esta opção para permitir ou desativar o envio de comandos pelo chat. Se desativado, os comandos só poderão ser digitados no console.");
                SkConfigEntry.CAllowPublicChatOutput = Config.Bind("0 - General", "AllowPublicResponse", true
                    , "Ative para permitir que o mod responda publicamente a certos comandos quando digitados no chat." +
                    "\nPor exemplo, se o comando /portal for usado no chat com isso ativo, jogadores próximos verão a resposta." +
                    "\nNOTA: Se a resposta sair com o seu nome, ela é pública e todos podem ver. Se for uma resposta de (Speelo's Toolbox), apenas você a vê.");
                SkConfigEntry.CAllowChatOutput = Config.Bind("0 - General", "AllowResponseInChat", true
                    , "Ative para permitir que o mod exiba retornos no chat. Se desativado, o mod não mostrará nenhuma mensagem no chat, seja pública ou privada.");
                SkConfigEntry.CAllowExecuteOnClear = Config.Bind("0 - General", "AllowExecuteOnClear", false
                    , "Ative para permitir a execução de comandos ao limpar a caixa de texto (pressionando Esc ou apagando todo o texto).");
                SkConfigEntry.CKeepAchievements = Config.Bind("0 - General", "KeepAchievements", true
                    , "Mantém as conquistas da Steam funcionando mesmo ao usar trapaças." +
                    "\nO Valheim desativa o progresso de conquistas ao detectar modificações (carregar o BepInEx já define Game.isModded)." +
                    "\nEsta opção ativa a chave interna PlayerProfile.s_bypassCheatChecks para continuar contabilizando conquistas." +
                    "\nTambém impede que o personagem seja marcado permanentemente por trapaças e limpa marcas anteriores." +
                    "\nDefina como false para manter o comportamento padrão do Valheim.");
                SkConfigEntry.CPersistCheatsOnDeath = Config.Bind("0 - General", "PersistCheatsOnDeath", true
                    , "Reaplica suas trapaças automaticamente após morrer." +
                    "\nA morte destrói o objeto do personagem e o jogo cria um novo, fazendo com que trapaças ativas" +
                    " (modo deus, voo, construção sem custo, stamina infinita, etc.) sejam desativadas ao reaparecer." +
                    "\nCom isto ativo, todas as trapaças que estavam ligadas serão reativadas ao reaparecer." +
                    "\nDefina como false para que a morte desative todas as trapaças normalmente.");
                SkConfigEntry.CGodModeBlocksDamage = Config.Bind("0 - General", "GodModeBlocksDamage", true
                    , "Faz o modo deus bloquear o dano completamente." +
                    "\nO modo deus padrão do Valheim não impede dano: ele reduz sua vida e só impede a morte no último instante, mantendo 1 de vida." +
                    "\nCom isto ativo, todo dano recebido (incluindo fogo, veneno e fumaça) é anulado antes de atingir você." +
                    "\nDefina como false para manter o comportamento padrão do Valheim.");
                SkConfigEntry.COpenConsoleWithSlash = Config.Bind("0 - General", "OpenConsoleWithSlash", false
                    , "Ative para permitir abrir o console pressionando a tecla barra (/)." +
                    "\nEsta opção tem prioridade sobre OpenChatWithSlash se ambas estiverem ativas.");
                SkConfigEntry.COpenChatWithSlash = Config.Bind("0 - General", "OpenChatWithSlash", false
                    , "Ative para permitir abrir o chat pressionando a tecla barra (/).");

                SkConfigEntry.CAutoRun = Config.Bind("1 - AutoRun", "AutoRunEnabled", false
                    , "Ative para executar comandos automaticamente ao entrar no servidor." +
                    "\nNota: ocorre apenas uma vez por inicialização do jogo para evitar execuções indesejadas.");
                SkConfigEntry.CAutoRunCommand = Config.Bind("1 - AutoRun", "AutoRunCommand", "/nosup; /god"
                    , "Insira aqui os comandos para executar ao reaparecer. Separe múltiplos comandos com ponto e vírgula (;).");

                SkConfigEntry.OMenuToggleKey = Config.Bind("4 - OnScreenMenu", "MenuToggleKey", "F6"
                    , "Tecla que abre e fecha o menu interativo. Nomes de teclas válidos: https://docs.unity3d.com/ScriptReference/KeyCode.html" +
                    "\nJá ocupadas: F11 e F12 tiram captura de tela, F5 abre o console do jogo, F4 abre o log do mod," +
                    " F2 abre o painel de conexão, F9 alterna layout do controle, Ctrl+F1 alterna captura do mouse, Ctrl+F3 oculta o HUD." +
                    "\nTeclas de função livres sugeridas: F6, F7, F8, F10.");
                SkConfigEntry.OMenuWalk = Config.Bind("4 - OnScreenMenu", "WalkWithMenuOpen", true
                    , "Permite andar com o menu aberto, em vez de congelar seu personagem no lugar." +
                    "\nWASD movimenta normalmente. Atacar, bloquear, pular e esquivar permanecem desativados enquanto o menu estiver aberto." +
                    "\nAo digitar na caixa de busca, o movimento é pausado. Segure a tecla de olhar para girar a câmera." +
                    "\nApenas teclado e mouse: com controle (gamepad), o menu congela a movimentação normalmente.");
                SkConfigEntry.OMenuLookKey = Config.Bind("4 - OnScreenMenu", "MenuLookKey", "Mouse1"
                    , "Segure esta tecla para girar a câmera com o menu aberto. Solte para ter o ponteiro do mouse de volta." +
                    "\nMouse1 é o botão direito do mouse. É seguro usar pois a defesa fica desativada com o menu aberto." +
                    "\nLeftAlt é uma ótima alternativa se preferir não usar o botão direito." +
                    "\nNomes de teclas válidos: https://docs.unity3d.com/ScriptReference/KeyCode.html");
                SkConfigEntry.OBookmarks = Config.Bind("4 - OnScreenMenu", "Bookmarks", ""
                    , "Locais de teletransporte salvos, gerenciados pelo menu (não é necessário editar manualmente)." +
                    "\nCada entrada é seed|nome|x|y|z, e apenas os locais do mundo atual são exibidos.");
                SkConfigEntry.OLoadouts = Config.Bind("4 - OnScreenMenu", "Loadouts", ""
                    , "Conjuntos de equipamentos salvos, gerenciados pelo menu (não é necessário editar manualmente)." +
                    "\nCada entrada segue o formato nome|prefab:qualidade,prefab:qualidade.");
                SkConfigEntry.OMenuOpacity = Config.Bind("4 - OnScreenMenu", "MenuOpacity", 0.96f
                    , "Opacidade do fundo do menu. 1 = totalmente opaco, 0.5 = meio transparente. Aplicação imediata.");


                SkConfigEntry.CAllowLookCustomizations = Config.Bind("2 - CustomizeConsoleLook", "ConsoleAllowLookCustomizations", true
                    , "Ative para habilitar ou desabilitar as configurações de visual do console abaixo.");

                SkConfigEntry.CConsoleFont = Config.Bind("2 - CustomizeConsoleLook", "ConsoleFont", "Consola.ttl"
                    , "Define a fonte do texto do console. Padrão do jogo = AveriaSansLibre-Bold");
                SkConfigEntry.CConsoleFontSize = Config.Bind("2 - CustomizeConsoleLook", "ConsoleFontSize", 18
                    , "Define o tamanho da fonte do texto no console. Padrão do jogo = 18");

                SkConfigEntry.CConsoleOutputTextColor = Config.Bind("2 - CustomizeConsoleLook", "ConsoleOutputTextColor", "#E6F7FFFF"
                    , "Define a cor do texto de saída no console. Padrão do jogo = #FFFFFFFF. Formato de cor: #RRGGBBAA");
                SkConfigEntry.CConsoleInputTextColor = Config.Bind("2 - CustomizeConsoleLook", "ConsoleInputTextColor", "#E6F7FFFF"
                    , "Define a cor do texto de entrada no console. Padrão do jogo = #FFFFFFFF. Formato de cor: #RRGGBBAA");
                SkConfigEntry.CConsoleSelectionColor = Config.Bind("2 - CustomizeConsoleLook", "ConsoleSelectionColor", "#A8CEFFC0"
                    , "Define a cor do destaque de seleção no console. Padrão do jogo = #A8CEFFC0. Formato de cor: #RRGGBBAA");
                SkConfigEntry.CConsoleCaretColor = Config.Bind("2 - CustomizeConsoleLook", "ConsoleCaretColor", "#DCE6F5FF"
                    , "Define a cor do cursor de texto no console. Padrão do jogo = #FFFFFFFF. Formato de cor: #RRGGBBAA");

                SkConfigEntry.CAlias1 = Config.Bind("3 - CommandAliasing", "Alias1", "/creative: /god; /ghost; /imacheater; /nores; /nocost; /echo Creative mode toggled!"
                    , "Defina isto para criar um apelido de comando (alias). Especifique o que o usuário digitará e o que será executado. " +
                    "Encadeamento de comandos é suportado e pode ser usado com a Execução Automática." +
                    " Apelidos não podem referenciar outros apelidos. O prefixo barra (/) não é obrigatório - apelidos de comandos podem ser o que você quiser!" +
                    " Apelidos também funcionam pelo chat (se a opção de chat estiver ativa) e aparecem nos menus de ajuda /?." +
                    "\nFormato: OQueDigitar: OQueExecutar" +
                    "\nExemplo: Cria um novo comando '/Cmd1' que executa /god e /fly" +
                    "\n/Cmd1: /god; /fly" +
                    "\n##################" +
                    "\nDefina para criar o apelido de comando 1.");
                SkConfigEntry.CAlias2 = Config.Bind("3 - CommandAliasing", "Alias2", ""
                    , "Defina para criar o apelido de comando 2.");
                SkConfigEntry.CAlias3 = Config.Bind("3 - CommandAliasing", "Alias3", ""
                    , "Defina para criar o apelido de comando 3.");
                SkConfigEntry.CAlias4 = Config.Bind("3 - CommandAliasing", "Alias4", ""
                    , "Defina para criar o apelido de comando 4.");
                SkConfigEntry.CAlias5 = Config.Bind("3 - CommandAliasing", "Alias5", ""
                    , "Defina para criar o apelido de comando 5.");
                SkConfigEntry.CAlias6 = Config.Bind("3 - CommandAliasing", "Alias6", ""
                    , "Defina para criar o apelido de comando 6.");
                SkConfigEntry.CAlias7 = Config.Bind("3 - CommandAliasing", "Alias7", ""
                    , "Defina para criar o apelido de comando 7.");
                SkConfigEntry.CAlias8 = Config.Bind("3 - CommandAliasing", "Alias8", ""
                    , "Defina para criar o apelido de comando 8.");
                SkConfigEntry.CAlias9 = Config.Bind("3 - CommandAliasing", "Alias9", ""
                    , "Defina para criar o apelido de comando 9.");
                SkConfigEntry.CAlias10 = Config.Bind("3 - CommandAliasing", "Alias10", ""
                    , "Defina para criar o apelido de comando 10.");
                SkConfigEntry.CAlias11 = Config.Bind("3 - CommandAliasing", "Alias11", ""
                    , "Defina para criar o apelido de comando 11.");
                SkConfigEntry.CAlias12 = Config.Bind("3 - CommandAliasing", "Alias12", ""
                    , "Defina para criar o apelido de comando 12.");
                SkConfigEntry.CAlias13 = Config.Bind("3 - CommandAliasing", "Alias13", ""
                    , "Defina para criar o apelido de comando 13.");
                SkConfigEntry.CAlias14 = Config.Bind("3 - CommandAliasing", "Alias14", ""
                    , "Defina para criar o apelido de comando 14.");
                SkConfigEntry.CAlias15 = Config.Bind("3 - CommandAliasing", "Alias15", ""
                    , "Defina para criar o apelido de comando 15.");

                SkConfigEntry.CHotkey1 = Config.Bind("5 - CommandHotkeys", "Hotkey 1", ""
                    , "Defina isto para criar um atalho de teclado para um comando ou sequência de comandos." +
                    "\nFormato válido: OQuePressionar: OQueExecutar" +
                    "\nExemplo 1: Pressione z para executar /fly; /god       | z: /fly; /god" +
                    "\nExemplo 2: Pressione Shift+Z para executar /creative  | Z: /creative" +
                    "\nExemplo 3: Pressione Acento para abrir o console      | `: /console" +
                    "\nApenas caracteres ASCII imprimíveis são válidos! (https://theasciicode.com.ar/). Letras maiúsculas exigem pressionar Shift junto." +
                    " Cada tecla só pode ser atribuída a um atalho (se o atalho 1 for q, o atalho 2 não pode ser q)." +
                    "\nApenas o primeiro dois-pontos separa a tecla do comando (o comando em si pode conter dois-pontos)." +
                    " A tecla não pode ser dois-pontos nem espaço." +
                    "\nAtalhos não são acionados durante a digitação no chat, console, placas, marcadores de mapa, inventário ou menu do mod." +
                    "\nAs alterações são lidas na inicialização; reinicie o jogo ou use Recarregar Toolbox na aba Sistema após editar." +
                    " Atalhos inválidos avisarão no console ao iniciar.");
                SkConfigEntry.CHotkey2 = Config.Bind("5 - CommandHotkeys", "Hotkey 2", ""
                    , "Defina para criar um atalho de teclado para um comando ou sequência de comandos.");
                SkConfigEntry.CHotkey3 = Config.Bind("5 - CommandHotkeys", "Hotkey 3", ""
                    , "Defina para criar um atalho de teclado para um comando ou sequência de comandos.");
                SkConfigEntry.CHotkey4 = Config.Bind("5 - CommandHotkeys", "Hotkey 4", ""
                    , "Defina para criar um atalho de teclado para um comando ou sequência de comandos.");
                SkConfigEntry.CHotkey5 = Config.Bind("5 - CommandHotkeys", "Hotkey 5", ""
                    , "Defina para criar um atalho de teclado para um comando ou sequência de comandos.");
                SkConfigEntry.CHotkey6 = Config.Bind("5 - CommandHotkeys", "Hotkey 6", ""
                    , "Defina para criar um atalho de teclado para um comando ou sequência de comandos.");
                SkConfigEntry.CHotkey7 = Config.Bind("5 - CommandHotkeys", "Hotkey 7", ""
                    , "Defina para criar um atalho de teclado para um comando ou sequência de comandos.");
                SkConfigEntry.CHotkey8 = Config.Bind("5 - CommandHotkeys", "Hotkey 8", ""
                    , "Defina para criar um atalho de teclado para um comando ou sequência de comandos.");
                SkConfigEntry.CHotkey9 = Config.Bind("5 - CommandHotkeys", "Hotkey 9", ""
                    , "Defina para criar um atalho de teclado para um comando ou sequência de comandos.");
                SkConfigEntry.CHotkey10 = Config.Bind("5 - CommandHotkeys", "Hotkey 10", ""
                    , "Defina para criar um atalho de teclado para um comando ou sequência de comandos.");
                SkConfigEntry.CHotkey11 = Config.Bind("5 - CommandHotkeys", "Hotkey 11", ""
                    , "Defina para criar um atalho de teclado para um comando ou sequência de comandos.");
                SkConfigEntry.CHotkey12 = Config.Bind("5 - CommandHotkeys", "Hotkey 12", ""
                    , "Defina para criar um atalho de teclado para um comando ou sequência de comandos.");
                SkConfigEntry.CHotkey13 = Config.Bind("5 - CommandHotkeys", "Hotkey 13", ""
                    , "Defina para criar um atalho de teclado para um comando ou sequência de comandos.");
                SkConfigEntry.CHotkey14 = Config.Bind("5 - CommandHotkeys", "Hotkey 14", ""
                    , "Defina para criar um atalho de teclado para um comando ou sequência de comandos.");
                SkConfigEntry.CHotkey15 = Config.Bind("5 - CommandHotkeys", "Hotkey 15", ""
                    , "Defina para criar um atalho de teclado para um comando ou sequência de comandos.");

            }
            catch (Exception Ex)
            {
                SkUtilities.Logz(new string[] { "ERR" }, new string[] { "Não foi possível carregar a configuração. Verifique se há uma versão funcional do BepInEx instalada.",
                                                                            Ex.Message, Ex.Source}, LogType.Error);
            }
        }
    }
}
