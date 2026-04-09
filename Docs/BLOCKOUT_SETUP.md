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
- marcadores de porta
- objetos simples temporarios

As portas reais podem ficar para depois. Nesta fase basta deixar um marcador de onde a transicao entre salas vai acontecer.

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

## Escala usada

O layout base foi ajustado para estas medidas:

- `1.1`: `1.80 x 2.60`
- `1.2`: `3.90 x 2.60`
- `1.3`: `3.60 x 2.60`
- `1.5`: `2.70 x 2.60`
- `1.13`: `1.80 x 2.60`

Serve como base de trabalho, nao como reconstrucao final de arquitetura.

## Ordem recomendada

1. Gerar o blockout.
2. Colocar uma camera e um player temporario.
3. Posicionar o quadro do Dom Pedro no `Lobby 1.1`.
4. Escolher um ponto de entrada para cada sala.
5. So depois comecar a montar interacoes e puzzles.
