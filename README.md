# Conversor XML NF-e para DANFE PDF

Aplicativo desktop local para Windows em .NET 8/WinForms. Ele carrega XMLs de NF-e em lote, reconhece documentos `NFe` e `nfeProc/procNFe`, gera PDFs no padrão DANFE e salva os resultados na pasta de saída escolhida pelo usuário.

## Requisitos de desenvolvimento

- Windows
- Visual Studio 2022 ou SDK .NET 8
- Restauração dos pacotes NuGet do projeto

O aplicativo não consulta SEFAZ e não depende de internet em tempo de execução.

## Como executar em desenvolvimento

```powershell
dotnet restore
dotnet run
```

Nesta máquina o SDK .NET não está instalado, então a compilação local não foi executada aqui.

## Como gerar executável Windows

```powershell
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

O executável ficará em:

```text
bin\Release\net8.0-windows\win-x64\publish
```

## Instalador

O pacote de instalação é gerado com Inno Setup:

```powershell
& "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe" "Installer\ConversorXmlNFeDanfePdf.iss"
```

O instalador final fica em:

```text
dist\Instalador_Conversor_XML_NFe_DANFE_PDF.exe
```

## Funcionalidades

- Carregamento de XMLs por seleção de arquivos.
- Carregamento de XMLs, pastas e compactados por arrastar e soltar na janela.
- Extração de XMLs de arquivos compactados como `.zip`, `.rar`, `.7z`, `.tar`, `.gz`, `.tgz`, `.bz2`, `.xz` e similares suportados pelo SharpCompress.
- Seleção obrigatória da pasta de saída.
- Opção para gerar também um PDF único com todos os DANFEs convertidos.
- Tratamento de PDF existente: ignorar, sobrescrever ou gerar sufixo incremental.
- Geração de PDF A4 retrato em layout DANFE clássico, com canhoto, chave de acesso, Code 128, blocos fiscais, tabela de produtos e dados adicionais.
- Ícone próprio de scanner de código de barras no aplicativo, atalhos e instalador.
- Execução na bandeja do Windows, com menu para abrir ou sair.
- Auto-update pela aba Releases do GitHub, com verificação inicial e periódica a cada 5 minutos.
- Relatório `.txt` automático ao final.
- Exportação de relatório `.csv`.

## Fluxo de uso

1. Clique em `Carregar arquivos` ou arraste XMLs, pastas e compactados para a janela.
2. Escolha a pasta de saída.
3. Configure o comportamento para PDFs já existentes e marque `Gerar PDF unico` quando quiser consolidar tudo.
4. Clique em `Converter XMLs`.

## Estrutura

```text
Models/
Services/
Templates/
UI/
Utils/
```

O conversor Python criado anteriormente continua na pasta apenas como referência, mas o sistema principal agora é o projeto .NET `ConversorXmlNFeDanfePdf.csproj`.
