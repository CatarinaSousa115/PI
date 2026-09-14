# Escape the Museum

Escape the Museum é uma experiência de realidade virtual desenvolvida em Unity, inspirada no Museu Nacional Soares dos Reis (MNSR), no Porto.

O projeto recria em 3D algumas das salas do museu e integra diferentes desafios e mini-jogos que o utilizador deve completar para progredir na experiência.

## Demonstração

[![Escape the Museum — Demonstração](https://img.youtube.com/vi/wL8ZE0XOc0o/maxresdefault.jpg)](https://youtu.be/wL8ZE0XOc0o)

## Documentação

- [Relatório do projeto](PE29_ESCAPE_THE_MUSEUM_RELATORIO.pdf)

- [Poster do projeto](PE29_ESCAPE_THE_MUSEUM_POSTER.pdf)

## Sobre o projeto

O objetivo do projeto é proporcionar uma experiência imersiva de exploração e interação com o património do MNSR através de realidade virtual.

O utilizador percorre diferentes espaços do museu, interage com objetos e resolve desafios relacionados com as obras e elementos presentes em cada sala.

A experiência foi desenvolvida para Meta Quest, utilizando o sistema de interação XR disponibilizado pelo Unity.

## Funcionalidades

- Exploração de ambientes 3D inspirados no Museu Nacional Soares dos Reis;
- Experiência de realidade virtual;
- Sistema de locomoção por teletransporte;
- Interação com objetos através dos controladores VR;
- Diferentes salas e desafios;
- Mini-jogos integrados nos espaços do museu;
- Reconstrução e interação com elementos artísticos;
- Puzzle de reconstrução de uma pintura;
- Interação com personagens e elementos históricos;
- Sistema de progressão através da resolução dos desafios.

## Tecnologias

- Unity 6
- C#
- XR Interaction Toolkit
- OpenXR
- Meta Quest
- Git / Git LFS
- Modelação e geração de conteúdos 3D

## Estrutura do projeto

A estrutura principal do projeto Unity encontra-se diretamente na raiz do repositório:

```Plain text
PI/
├── Assets/
│ ├── Materials/
│ ├── Scenes/
│ ├── Scripts/
│ ├── ...
├── Packages/
├── ProjectSettings/
├── UserSettings/
├── PI.sln
├── PE29_ESCAPE_THE_MUSEUM_RELATORIO.pdf
├── PE29_ESCAPE_THE_MUSEUM_POSTER.pdf
└── README.md
```

As principais cenas do projeto incluem:

- Lobby.unity — espaço inicial da experiência;
- PaintingScene.unity — ambiente associado ao desafio da pintura.

## Experiência

A experiência começa no lobby, onde o utilizador pode iniciar a exploração do museu.

Ao longo do percurso, o utilizador encontra diferentes salas com desafios específicos. A interação com os objetos e a resolução dos mini-jogos permitem avançar na experiência.

Entre os elementos desenvolvidos encontram-se representações de figuras e obras associadas ao MNSR, incluindo uma representação de Luís de Camões e um desafio relacionado com a reconstrução de uma pintura.

## Instalação

Para abrir e executar o projeto é necessário ter o Unity 6 instalado, juntamente com os componentes necessários para desenvolvimento para Android e realidade virtual.

### 1. Clonar o repositório

```bash
git clone git@github.com:CatarinaSousa115/PI.git
cd PI
```

Como o projeto utiliza Git LFS, é necessário garantir que o Git LFS está instalado e inicializado:

```bash
git lfs install
git lfs pull
```

### 2. Abrir no Unity

Abrir o projeto através do Unity Hub, selecionando a pasta raiz:

```Plain text
PI/
```

O Unity deverá reconhecer automaticamente a estrutura do projeto e importar os recursos necessários.

### 3. Executar

Depois de abrir o projeto, selecionar a cena inicial através do Unity Editor e executar o projeto.

Para utilizar a experiência em realidade virtual, é necessário ter um dispositivo Meta Quest devidamente configurado para desenvolvimento.

## Controlo e interação

A experiência foi concebida para utilização com os controladores de realidade virtual do Meta Quest.

A movimentação utiliza um sistema de teletransporte, permitindo ao utilizador deslocar-se pelos ambientes sem necessidade de locomoção contínua.

A interação com os elementos da cena é realizada através dos sistemas de interação do XR Interaction Toolkit.

## Desenvolvimento

O projeto foi desenvolvido no âmbito da unidade curricular de Projeto Integrador, tendo como objetivo explorar a utilização de tecnologias de realidade virtual na recriação e divulgação de património cultural.

### Equipa

- Ana Sousa
- Arthur Teixeira
- João Silva
- Gustavo Teixeira

### Orientação

**Tutor:** António Fernando Vasconcelos Cunha Castro Coelho

**Proponentes:** Maria van Zeller de Macedo de Oliveira e Sousa _&_ Mariana Oliveira Magalhães

## Objetivos

O projeto pretende demonstrar o potencial da realidade virtual como ferramenta de exploração de espaços culturais, permitindo ao utilizador interagir com uma representação virtual do museu de uma forma imersiva.

Para além da componente tecnológica, o projeto procura aproximar o utilizador do património cultural através de uma experiência interativa e orientada para a descoberta.

## Repositório

O código-fonte e os restantes recursos do projeto estão disponíveis no [repositório do projeto](https://github.com/CatarinaSousa115/PI).

## Licença

Projeto desenvolvido para fins académicos.
