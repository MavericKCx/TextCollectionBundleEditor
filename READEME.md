<div align="center">

# TextCollection Bundle Editor (TCBE)

**Open-source Unity AssetBundle Localization Editor**

Edite e traduza arquivos **TextCollection** de jogos Unity diretamente do AssetBundle, sem precisar recompilar manualmente.

![Version](https://img.shields.io/badge/version-2.2.1-blue)
![Platform](https://img.shields.io/badge/platform-Windows%2064-bit-success)
![Framework](https://img.shields.io/badge/.NET-8.0-purple)
![License](https://img.shields.io/badge/license-MIT-green)

</div>

---

# Download

A versão mais recente pode ser baixada em:

➡ **Releases**

https://github.com/MavericKCx/TextCollectionBundleEditor/releases

---

# O que é?

O **TextCollection Bundle Editor (TCBE)** é uma ferramenta desenvolvida para editar localizações de jogos Unity que utilizam a estrutura **TextCollection**.

Ela permite modificar textos diretamente dentro de arquivos **AssetBundle**, sem necessidade de extrair, reconstruir ou recompilar manualmente os bundles.

---

# Recursos

## Editor

- ✅ Abrir arquivos `.bundle`
- ✅ Detectar automaticamente TextCollections
- ✅ Editar traduções diretamente
- ✅ Salvar novamente no AssetBundle
- ✅ Backup automático antes de salvar

---

## Interfaces

- ✅ Interface V1 (Tabela)
- ✅ Interface V2 (Editor)
- ✅ Alternância entre interfaces

---

## Ferramentas

- ✅ Pesquisa por ID
- ✅ Pesquisa por texto
- ✅ Filtros
- ✅ Próximo texto não traduzido

---

## CSV

- ✅ Exportar CSV
- ✅ Importar CSV
- ✅ Compatível com Excel
- ✅ Compatível com Google Sheets
- ✅ Compatível com LibreOffice Calc
- ✅ Detecção automática do separador (`;`, `,` ou TAB)

---

## Validação

- ✅ Verificação de Placeholders
- ✅ Verificação de Tags
- ✅ Verificação de Variáveis
- ✅ Verificação de Quebras de Linha

---

# Como usar

## 1

Abra o programa.

---

## 2

Clique em

```
Abrir Bundle
```

---

## 3

Escolha a coleção desejada.

Exemplo:

```
english-texts-fallback
```

---

## 4

Edite os textos.

---

## 5

Clique em

```
Salvar
```

Pronto.

---

# Fluxo recomendado

```
Bundle

↓

TCBE

↓

Exportar CSV

↓

Google Sheets

↓

Traduzir

↓

Baixar CSV

↓

Importar CSV

↓

Salvar Bundle

↓

Jogo
```

---

# Atalhos

| Atalho | Função |
|---------|---------|
| Ctrl + S | Salvar |
| Ctrl + F | Pesquisar |
| Ctrl + 1 | Interface V1 |
| Ctrl + 2 | Interface V2 |
| F3 | Próximo texto não traduzido |

---

# Compatibilidade

Atualmente testado em:

| Jogo | Status |
|------|--------|
| Tattoo Tycoon | ✅ Funcionando |

---

# Capturas de tela

## Interface V1

> *(adicione uma imagem futuramente)*

---

## Interface V2

> *(adicione uma imagem futuramente)*

---

## Tradução funcionando no jogo

> *(adicione a imagem do "NOVO JOGO" que você me mostrou)*

---

# Tecnologias

- C#
- .NET 8
- Windows Forms
- AssetsTools.NET

---

# Licença

MIT License

---

# Autor

**MaverickCX**

GitHub

https://github.com/MavericKCx

---

# História

O TextCollection Bundle Editor nasceu durante o desenvolvimento da tradução do jogo **Tattoo Tycoon**.

Inicialmente o objetivo era apenas traduzir esse jogo específico, porém durante o desenvolvimento surgiu a necessidade de criar uma ferramenta capaz de editar diretamente arquivos **AssetBundle** contendo estruturas **TextCollection**.

Com o tempo o projeto evoluiu para uma ferramenta completa de tradução de jogos Unity, tornando-se um projeto **Open Source** disponível gratuitamente para toda a comunidade.

---

# Roadmap

## v2.3

- Estatísticas de tradução
- Pesquisa avançada
- Melhorias na interface
- Mais filtros

---

## v3.0

- Suporte a TextAsset
- Suporte a Texture2D
- Visualizador de imagens
- Editor de outros tipos de AssetBundle
- Sistema de Plugins

---

⭐ Se esta ferramenta foi útil para você, considere deixar uma estrela no GitHub!
