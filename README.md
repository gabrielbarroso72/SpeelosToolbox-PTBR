# Speelo's Toolbox - Tradução PT-BR 🇧🇷

Um menu interativo de trapaças e utilitários clicável dentro do jogo para **Valheim 1.0+**, além de um extensor de comandos para console e chat. Pressione **F6** e clique no que desejar, sem precisar memorizar atalhos no teclado!

Esta é a versão **100% traduzida para o Português do Brasil (PT-BR)** por **Fox72**, baseada no projeto mantido por Speelo ([GitHub Original](https://github.com/Speelo/SpeelosToolbox)) e no mod original de Skrip / derekShaheen ([SkToolbox](https://github.com/derekShaheen/SkToolbox-for-Valheim)), sob a licença GPL-3.0.

> ⚠️ **Mod Exclusivo para o Cliente (Client-Side):** Este mod **NÃO** deve ser instalado em um servidor dedicado. Ele é uma interface gráfica do usuário (GUI) e o servidor não possui tela para renderizá-la. Remova-o do servidor dedicado. Os comandos que alteram o estado do mundo enviado continuam alcançando o servidor normalmente a partir do seu cliente através do canal de admin do próprio jogo.

---

## 🌟 Recursos Exclusivos da Versão PT-BR

Além de todos os textos, menus, botões, dicas de ferramentas (*tooltips*) e notificações do console traduzidos para o Português do Brasil, esta versão inclui melhorias de usabilidade:

- 📸 **Geração de Criaturas com Câmera Livre (*Free Fly*):** Ao ativar a *Câmera Livre* (na aba *Sistema > Exibição*), as criaturas invocadas no menu *Invocação* são geradas exatamente no **ponto focal do terreno ou construção para onde a sua câmera está apontando**, e não mais presas ao corpo do seu personagem!
- 🛠️ **Ocultamento Inteligente de Bancadas para Screenshots:** Ao ativar a câmera livre enquanto trabalha em uma bancada, a interface gráfica da bancada/inventário é ocultada automaticamente, **mantendo o seu personagem na postura e animação de trabalho na bancada** para tirar *prints* perfeitos!
- 🎯 **Tradução Fiel do Valheim:** Todos os nomes de status (*"Molhado"*, *"Frio"*, *"Conforto"*), alimentos, habilidades e comandos correspondem à localização oficial em Português do jogo.

---

## 🎮 O Menu (F6)

Pressione **F6** para abrir ou fechar o menu (**ESC** também fecha). A tecla pode ser alterada no arquivo de configuração `com.speelo.speelostoolbox.cfg` no campo `MenuToggleKey`.

O menu possui 5 abas principais na parte superior:

### 1. 👤 Jogador (Player)
- **Personagem:** Cura instantânea, seleção automática das melhores combinações de comida (Saúde Máxima, Estamina Máxima, Eitr Máximo ou Balanceada), reparar equipamentos, limpar inventário, ajustar habilidades e tamanho do inventário.
- **Bookmarks & Loadouts:** Salve pontos do mapa para se teleportar de volta a qualquer momento e crie *snapshots* completos do seu inventário para restaurar depois.
- **Trapaças (Toggles):** Modo Deus (God Mode com bloqueio real de dano), Voar, Construção sem custo, Construir em qualquer lugar, Estamina infinita, Eitr infinito, Sem limite de peso, Domesticar ao redor.
- **Efeitos e Poderes:** Aplique/remova efeitos de status, poderes dos guardiões e imunidades específicas por tipo de dano (fogo, queda, afogamento, etc.).
- **Informações:** Leitura em tempo real de posição, coordenadas, portais ativos e contagem de inimigos próximos.

### 2. 🎁 Entregar (Give)
- Grade de ícones com **todos os itens do jogo** (incluindo itens de outros mods).
- Barra de pesquisa integrada com suporte a acentos e nomes em português.
- Controles deslizantes para definir a **Quantidade** (1 a 100) e o **Nível de Melhoria** (1 a 4). Os itens vão direto para o seu inventário.

### 3. 🐉 Invocação (Spawn)
- Grade com **todas as criaturas e chefes do jogo**.
- Controles para quantidade e **nível de estrelas** (1 a 3 estrelas).
- Alternador para **gerar criatura domesticada**.
- *Suporte à Câmera Livre:* gera as criaturas onde a câmera livre está focando!

### 4. 🌍 Mundo (World)
- **Terreno:** Ferramentas de nivelar, elevar, escavar e desfazer edições com ajuste de raio e altura, além do otimizador de terreno (`/optterrain`).
- **Construção:** Reparar todas as estruturas ao redor e reabastecer tochas/fogueiras/lareiras instantaneamente.
- **Tempo e Clima:** Forçar qualquer tipo de clima, travar/definir hora do dia, controlar direção e intensidade do vento, avançar tempo ou dormir.
- **Regras:** Ajustar dificuldade simulada de jogadores, desativar geração natural de inimigos (`nospawn`), sem mapa, sem portais e modificadores/predefinições de mundo.
- **Progressão:** Definir/remover chaves globais de chefes e chaves do jogador.
- **Viagem:** Teletransporte por coordenadas, buscar e pinar estruturas/biomas no mapa, revelar mapa por raio ou por completo.
- **Limpeza:** Eliminar inimigos/ninhos, eliminar domesticados, limpar itens caídos no chão, limpar peixes/pássaros e apagar fogo/fumaça.
- **Servidor:** Expulsar (*kick*), banir, desbanir, lista de banidos e ping.

### 5. ⚙️ Sistema (System)
- **Exibição:** Campo de visão (FOV), limite de FPS, distância de detalhes (LOD), acúmulo de neve, Câmera Livre (*freefly*), Modo Debug e velocidade do jogo.
- **Caixa de Ferramentas:** Recarregar mod, descarregar mod e abrir pasta de logs.

---

## 🕹️ Movimentação com o Menu Aberto

Enquanto o menu estiver aberto:
- A seta do mouse fica livre para clicar nas opções.
- Você pode continuar andando normalmente com **WASD**.
- Segurar o **Botão Direito do Mouse** permite girar a câmera.
- Ataques, esquivas e saltos acidentais são bloqueados para evitar acidentes enquanto você navega ou digita na barra de busca.

---

## 💬 Comandos no Console (F5) e Chat

Ao abrir o console do jogo (**F5**), digite `/?` para ver a lista completa de comandos. A maioria dos comandos também pode ser digitada no chat do jogo com o prefixo `/`.

Exemplos de comandos:
- **Jogador:** `/god`, `/ghost`, `/fly`, `/heal`, `/infstam`, `/nocost`, `/repair`, `/give`, `/tame`, `/tp`, `/coords`
- **Mundo:** `/spawn`, `/spawntamed`, `/env`, `/tod`, `/wind`, `/stopevent`, `/tl`, `/tr`, `/td`, `/tu`, `/removedrops`, `/revealmap`, `/resetmap`, `/seed`

Você pode encadear comandos usando ponto e vírgula, por exemplo: `/god; /fly`.

---

## 🏆 Conquistas (Achievements)

Por padrão, o Valheim desativa as conquistas da Steam assim que detecta qualquer mod ou código de trapaça.

A opção `KeepAchievements` (ativada por padrão nas configurações do mod) mantém o seletor `s_bypassCheatChecks` ativado. Isso garante que:
- Você continue progredindo e liberando conquistas na Steam mesmo usando o mod.
- Itens e construções invocados não fiquem marcados com a *flag* de trapaça.
- Limpa o sinalizador de trapaça permanente do seu personagem ao entrar no jogo.

---

## ⚙️ Arquivo de Configuração

O arquivo de configuração é gerado automaticamente em:
`BepInEx/config/com.speelo.speelostoolbox.cfg`

Opções importantes:
- `MenuToggleKey`: Tecla para abrir o menu (Padrão: `F6`).
- `MenuOpacity`: Opacidade do fundo do menu.
- `KeepAchievements`: Mantém as conquistas ativas (`true`/`false`).
- `WalkWithMenuOpen`: Permite andar com o menu aberto (`true`/`false`).

---

## 📋 Requisitos

- **Valheim 1.0+**
- **BepInExPack_Valheim** (v5.4.2350 ou superior)
- Executar no cliente (jogo do jogador).

> *Jotunn **NÃO** é necessário.*

---

## 📜 Créditos e Licença

- **Fox72**: Tradução completa para o Português do Brasil (PT-BR), adaptação das interfaces, suporte à Câmera Livre para *spawn* e ocultamento de bancadas.
- **Speelo**: Desenvolvedor do fork mantido [SpeelosToolbox](https://github.com/Speelo/SpeelosToolbox) para Valheim 1.0.
- **Skrip / derekShaheen**: Criadores originais do [SkToolbox-for-Valheim](https://github.com/derekShaheen/SkToolbox-for-Valheim).

Licenciado sob **GPL-3.0**.
