# Player temporario para explorar o museu

Usa o script `SimpleFirstPersonController` para testar a escala e circular no whitebox.

## Criacao

1. Criar um objeto `Capsule` chamado `Player`.
2. Definir a tag como `Player`.
3. Adicionar um `CharacterController`.
4. Adicionar o componente `SimpleFirstPersonController`.
5. Arrastar a `Main Camera` para dentro do objeto `Player`.
6. No script, ligar `cameraRoot` a `Main Camera`.

## Posicao inicial sugerida

Para o lobby:

- `Player`: `X = 5.4`, `Y = 1`, `Z = 4.0`
- `Main Camera`: `X = 0`, `Y = 0.8`, `Z = 0`

## Controlos

- `WASD`: mover
- `Rato`: olhar
- `Shift`: correr
- `Espaco`: saltar
- `Esc`: libertar cursor
