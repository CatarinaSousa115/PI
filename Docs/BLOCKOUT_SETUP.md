# Whitebox inicial do museu

Sim, as salas `1.1`, `1.2`, `1.3`, `1.5` e `1.13` sao suficientes para comecar.

Com elas ja da para:

1. Montar o fluxo principal do jogo.
2. Testar escala e navegacao.
3. Posicionar o quadro do Dom Pedro no lobby.
4. Reservar o espaco de cada puzzle antes de ter arte final.

## Recomendacao

Faz primeiro um `whitebox`:

- chao
- paredes brancas
- marcadores de porta, que geram a abertura e a porta automaticamente
- objetos simples temporarios

As portas usam marcadores simples. O jeito recomendado e colocar o marcador como filho da parede correta (`NorthWall`, `SouthWall`, `EastWall` ou `WestWall`). Qualquer filho de uma dessas paredes com nome a comecar por `To ` vira uma abertura: a parede cheia e escondida, os segmentos laterais/superior sao gerados e a folha da porta aparece em `GeneratedDoorways`.

## Script de blockout

Usa o componente `MuseumBlockoutGenerator` em qualquer objeto vazio da cena.

### Passos

1. Criar um objeto vazio chamado `MuseumBlockout`.
2. Adicionar o componente `MuseumBlockoutGenerator`.
3. No menu do componente, usar `Apply Recommended Layout`.
4. Depois usar `Generate Blockout`.

Isso gera:

- `Lobby 1.1`
- `Sala 2 - 1.2`
- `Sala 3 - 1.3`
- `Sala 4 - 1.5`
- `Sala 5 - 1.13`

## Portas

Para adicionar uma porta numa sala existente:

1. Escolher a parede onde a porta deve existir: `NorthWall`, `SouthWall`, `EastWall` ou `WestWall`.
2. Criar um cubo como filho dessa parede.
3. Dar um nome que comece por `To `, por exemplo `To 1.5`.
4. Mover o cubo pela parede ate ficar no ponto da porta.
5. Ajustar a largura visualmente: numa parede norte/sul, alargar no eixo X; numa parede este/oeste, alargar no eixo Z. Ajustar a altura no eixo Y.
6. No componente `MuseumWhiteboxRuntimeLayout`, usar `Rebuild Doorways Now`. Se tambem quiser reaplicar posicao/tamanho das salas, usar `Apply Layout Now`.

Tambem funciona deixar o marcador como filho direto da sala, mas ai a logica precisa adivinhar a parede pela posicao. Como filho da parede, nao ha ambiguidade.

O marcador fica como referencia/trigger, mas o renderer dele e escondido para nao sobrepor a porta. A parede original daquela face tambem fica escondida e e substituida pelos segmentos em `GeneratedDoorways`.

## Escala usada

O layout base foi ajustado para estas medidas:

- `1.1`: `3.60 x 5.20`
- `1.2`: `7.80 x 5.20`
- `1.3`: `7.20 x 5.20`
- `1.5`: `5.40 x 5.20`
- `1.13`: `3.60 x 5.20`

Serve como base de trabalho, nao como reconstrucao final de arquitetura.

## Ordem recomendada

1. Gerar o blockout.
2. Colocar uma camera e um player temporario.
3. Posicionar o quadro do Dom Pedro no `Lobby 1.1`.
4. Escolher um ponto de entrada para cada sala.
5. So depois comecar a montar interacoes e puzzles.
