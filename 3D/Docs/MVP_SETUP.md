# Museu Soares dos Reis - Base do MVP

O projeto atual ja tem a mecanica da Sala 2 (puzzle de reconstrucao) em andamento. A base adicionada neste passo organiza o jogo como um MVP alinhado ao PDF:

1. `MuseumGameManager`
Controla o objetivo global, o numero de placas recuperadas e quais salas ja foram concluidas.

2. `MuseumRoom`
Marca cada sala com identidade propria, texto de objetivo e recompensa da placa ao concluir o desafio.

3. `MuseumHud`
Mostra objetivo atual, progresso das placas e mensagem de estado.

4. `ScenePortal`
Permite transicao entre salas/cenas, com bloqueio opcional ate a sala anterior estar concluida.

5. `SimpleDialogueTrigger`
Entrega o sistema base de falas para Dom Pedro, retratos e estatuas.

6. `FinalCollectionManager` e `PlaqueSocket`
Prepararam o ultimo passo do jogo: recolocar as quatro placas na colecao restaurada no lobby final.

7. `LobbyIntroController`
Organiza a cena inicial: reseta o progresso, arranca a fala inicial do Dom Pedro, atualiza o objetivo e desbloqueia a ida para a Sala 2.

8. `RoomCompletionListener`
Escuta a conclusao de uma sala para ativar portas, objetos, efeitos ou pequenos eventos visuais.

## Como ligar no Unity

1. Criar um objeto `GameManager` na cena inicial e adicionar `MuseumGameManager`.
2. Criar um canvas global com `MuseumHud` e ligar os `Text` de objetivo, placas e estado.
3. Em cada sala, adicionar um objeto com `MuseumRoom`:
   - `Lobby`
   - `Reconstruction`
   - `Conversations`
   - `Lights`
   - `Minigames`
   - `FinalGallery`
4. Na Sala 2, ligar o `PuzzleManager.linkedRoom` ao `MuseumRoom` dessa sala.
5. Para personagens narrativos, adicionar `SimpleDialogueTrigger`, preencher as falas e ligar os campos de UI.
6. Para transicoes, adicionar `ScenePortal` nas portas e preencher `targetSceneName`.
7. Na cena final, criar um objeto com `FinalCollectionManager` e quatro sockets com `PlaqueSocket`.

## Como montar o Lobby agora

1. Criar a cena `Lobby`.
2. Adicionar um objeto `GameManager` com `MuseumGameManager`.
3. Adicionar um canvas com `MuseumHud`.
4. Criar um objeto `LobbyRoom` com `MuseumRoom`:
   - `roomId = Lobby`
   - `awardsPlaqueOnComplete = false`
5. Criar um NPC ou retrato do Dom Pedro com `SimpleDialogueTrigger`.
6. Criar um objeto `LobbyIntro` com `LobbyIntroController` e ligar:
   - `lobbyRoom`
   - `introDialogue`
   - `firstPortal`
   - `hud`
7. Na porta para a Sala 2, adicionar `ScenePortal`:
   - `targetSceneName = SalaReconstruction`
   - desativar o componente no inicio da cena para o `LobbyIntroController` o ativar quando a fala acabar.

## Falas iniciais sugeridas para Dom Pedro

1. Bem-vindo ao Museu Soares dos Reis. Preciso da tua ajuda para restaurar uma parte muito importante desta colecao.
2. Quatro placas desapareceram, e sem elas a historia exposta nesta galeria ficou incompleta.
3. Cada sala do museu guarda pistas e desafios. Resolve-os e recupera as placas perdidas.
4. Comeca pela Sala da Reconstrucao. Quando terminares, regressa para devolvermos a memoria ao museu.

## Sugestao de cenas para o MVP

- `Lobby`
- `SalaReconstruction`
- `SalaConversations`
- `SalaLights`
- `SalaMinigames`
- `FinalGallery`

## Ordem recomendada de desenvolvimento

1. Fechar o `Lobby` com intro do Dom Pedro.
2. Polir a Sala 2 usando o puzzle atual.
3. Fazer a Sala 3 com retratos interativos e uma escolha correta.
4. Prototipar a Sala 4 com puzzle simples de luz/sombra.
5. Fazer a Sala 5 com tres minigames curtos.
6. Finalizar o retorno ao lobby e reposicao das placas.
