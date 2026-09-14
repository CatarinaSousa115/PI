# Escape the Museum

> Experiência de realidade virtual em Unity, inspirada no Museu Nacional Soares dos Reis (MNSR), no Porto.

## Autores

| Nome                                 | E-Mail                   |
| ------------------------------------ | ------------------------ |
| Ana Catarina Monteiro de Sousa       | up202306419@edu.fe.up.pt |
| Arthur Pessoa de Mello Teixeira      | up202300368@edu.fe.up.pt |
| Gustavo Luis Gommans Aguiar Teixeira | up202304718@edu.fe.up.pt |
| João Miguel Teixeira da Silva        | up202306429@edu.fe.up.pt |

**Tutor:** António Fernando Vasconcelos Cunha Castro Coelho

**Proponentes:** Maria van Zeller de Macedo de Oliveira e Sousa e Mariana Oliveira Magalhães

## Contexto Académico

- **Unidade Curricular:** Projeto Integrador
- **Instituição:** FEUP — Faculdade de Engenharia da Universidade do Porto
- **Ano/Semestre:** 2025/2026 — 2.º semestre
- **Nota obtida:** 19/20 (Trabalho (50%): 20.0 | Relatório (35%): 18.0 | Apresentação Final (15%): 18.7)

## Descrição

Escape the Museum recria em 3D algumas das salas do Museu Nacional Soares dos Reis e integra diferentes desafios e mini-jogos que o utilizador deve completar para progredir na experiência.

O objetivo é proporcionar uma experiência imersiva de exploração e interação com o património do MNSR através de realidade virtual: o utilizador percorre diferentes espaços do museu, interage com objetos e resolve desafios relacionados com as obras e elementos presentes em cada sala. Entre os elementos desenvolvidos encontram-se representações de figuras e obras associadas ao MNSR, incluindo uma representação de Luís de Camões e um desafio relacionado com a reconstrução de uma pintura.

A experiência foi desenvolvida e testada em dispositivos Meta Quest, utilizando OpenXR e o sistema de interação XR disponibilizado pelo Unity.

### Demonstração

[![Escape the Museum — Demonstração](https://img.youtube.com/vi/wL8ZE0XOc0o/maxresdefault.jpg)](https://youtu.be/wL8ZE0XOc0o)

### Funcionalidades

- Exploração de ambientes 3D inspirados no Museu Nacional Soares dos Reis
- Experiência de realidade virtual
- Sistema de locomoção por teletransporte
- Interação com objetos através dos controladores VR
- Diferentes salas e desafios
- Mini-jogos integrados nos espaços do museu
- Reconstrução e interação com elementos artísticos
- Puzzle de reconstrução de uma pintura
- Interação com personagens e elementos históricos
- Sistema de progressão através da resolução dos desafios

## Tecnologias Utilizadas

- Unity 6
- C#
- XR Interaction Toolkit
- OpenXR
- Meta Quest
- Git / Git LFS
- Modelação e geração de conteúdos 3D

## Estrutura do Projeto

```text
PI/
├── Assets/
│   ├── Materials/
│   ├── Scenes/
│   ├── Scripts/
│   ├── ...
├── Packages/
├── ProjectSettings/
├── UserSettings/
├── PI.sln
├── PE29_ESCAPE_THE_MUSEUM_RELATORIO.pdf
├── PE29_ESCAPE_THE_MUSEUM_POSTER.pdf
└── README.md
```

Principais cenas do projeto:

- `Lobby.unity` — espaço inicial da experiência
- `PaintingScene.unity` — ambiente associado ao desafio da pintura

## Requisitos

- Unity 6, com os módulos necessários para desenvolvimento Android e realidade virtual
- Git e Git LFS
- Dispositivo compatível com realidade virtual para a experiência completa (testado em Meta Quest)

## Como Compilar / Executar

### 1. Clonar o repositório

```bash
git clone git@github.com:CatarinaSousa115/PI.git
cd PI
```

Como o projeto utiliza Git LFS, é necessário garantir que está instalado e inicializado:

```bash
git lfs install
git lfs pull
```

### 2. Abrir no Unity

Abrir o projeto através do Unity Hub, selecionando a pasta raiz `PI/`. O Unity deverá reconhecer automaticamente a estrutura do projeto e importar os recursos necessários.

### 3. Executar

Selecionar a cena inicial (`Lobby.unity`) no Unity Editor e executar o projeto.

Para utilizar a experiência em realidade virtual, é necessário ter um dispositivo compatível devidamente configurado para desenvolvimento (o projeto foi desenvolvido e testado com Meta Quest).

## Como Usar

A experiência começa no lobby, onde o utilizador pode iniciar a exploração do museu. Ao longo do percurso, encontra diferentes salas com desafios específicos — a interação com os objetos e a resolução dos mini-jogos permitem avançar na experiência.

A movimentação utiliza um sistema de teletransporte, e a interação com os elementos da cena é feita através dos sistemas de interação do XR Interaction Toolkit, usando os controladores VR.

## Download

Versão Android da aplicação: [Download do APK](https://drive.google.com/file/d/1TAANNyBBF_yvaf0mGMTIbh3g5I2UvCvx/view?usp=share_link)

## Documentação

- [Relatório do projeto](PE29_ESCAPE_THE_MUSEUM_RELATORIO.pdf)
- [Poster do projeto](PE29_ESCAPE_THE_MUSEUM_POSTER.pdf)

## Notas Adicionais

O projeto explora a utilização de tecnologias de realidade virtual na recriação e divulgação do património cultural, procurando aproximar o utilizador do património do MNSR através de uma experiência interativa e orientada para a descoberta.

Futuramente, pretende-se expandir o projeto de forma a abranger as restantes salas do museu, proporcionando uma experiência de exploração mais completa do MNSR.
