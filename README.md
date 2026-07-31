# ☀️ SkyTray Weather

[![Build & Release](https://github.com/tomas-barros1/SkyTray-Weather/actions/workflows/build-and-release.yml/badge.svg)](https://github.com/tomas-barros1/SkyTray-Weather/actions/workflows/build-and-release.yml)
![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D4?logo=windows)
![License](https://img.shields.io/badge/License-MIT-green.svg)

**SkyTray Weather** é um aplicativo desktop nativo para Windows 11/10 projetado para rodar de forma discreta, elegante e ultra-rápida na bandeja do sistema (System Tray).

Com uma interface moderna inspirada nas diretrizes do Windows 11 (Acrylic/Mica), o SkyTray exibe um painel completo com 11 métricas meteorológicas em tempo real, previsão para as próximas 6 horas, suporte a internacionalização (Português e Inglês) e ícones vetoriais de alta definição na barra de tarefas.

---

## 🌟 Funcionalidades

- ☀️ **Ícone Vetorial Dinâmico no Tray**: Renderização vetorial GDI+ nativa em alta definição (32bpp ARGB) que altera o ícone de acordo com o tempo (Sol, Lua, Parcialmente Nublado, Chuva, Tempestade, Neve).
- 📍 **Localização Automática**: Geolocalização nativa do Windows com fallback automático via IP para nunca deixar você sem previsão.
- 📊 **Painel com 11 Métricas de Clima**:
  - 🌡️ Temperatura e Sensação Térmica
  - ☁️ Nebulosidade (%)
  - 💧 Umidade (%)
  - ⏲️ Pressão Atmosférica (hPa)
  - 🌬️ Velocidade do Vento (km/h)
  - ☔ Precipitação / Chuva (mm/h)
  - 🍃 Qualidade do Ar (AQI)
  - ☀️ Índice UV Max
  - 🌅 Horário do Nascer do Sol
  - 🌇 Horário do Pôr do Sol
  - 🕒 Previsão Hora a Hora para as Próximas 6 Horas
- 🌐 **Internacionalização (i18n)**: Detecção automática do idioma do sistema operacional com dicionários JSON (`pt_BR.json` e `en_US.json`).
- ⚙️ **Configurações e Inicialização**:
  - Opção para **Iniciar com o Windows** (Registro `HKCU\...\Run`).
  - Intervalo de Atualização Personalizável (5 min, 10 min, 15 min [padrão], 30 min, 60 min).
- ❌ **Execução em Segundo Plano**: Fechar a janela minimiza o app diretamente para a bandeja do sistema. Clique com o botão direito no ícone para acessar as Configurações ou Sair.

---

## 🏗️ Arquitetura do Projeto

O repositório é estruturado de forma desacoplada seguindo boas práticas de engenharia de software:

```text
├── WinuiWheaterForecastTray.Core/      # Biblioteca de domínio .NET 8 (DTOs, Serviços, i18n, APIs)
├── WinuiWheaterForecastTray/           # Aplicação UI WinUI 3 (Windows App SDK, Renderizador Tray, XAML)
├── WinuiWheaterForecastTray.Tests/     # Suíte de Testes Automatizados xUnit (16 testes unitários e de integração)
├── Install-SkyTray.ps1                 # Script de instalação de 1-clique para Windows
└── .github/workflows/                  # Automação CI/CD GitHub Actions
```

---

## 🚀 Instalação Rápida (1-Clique)

### Opção 1: Baixar o Instalador Pronto (GitHub Release)
1. Vá até a seção **[Releases](https://github.com/tomas-barros1/SkyTray-Weather/releases)** e baixe o `SkyTray-Weather-Setup.zip`.
2. Extraia o conteúdo e clique duas vezes em `Install-SkyTray.ps1` ou execute no PowerShell:

```powershell
powershell -File Install-SkyTray.ps1
```

O aplicativo será instalado automaticamente em `%LocalAppData%\SkyTrayWeather` com atalho criado no seu **Menu Iniciar**.

---

### Opção 2: Compilar a partir do Código Fonte

#### Pré-requisitos
- SDK do .NET 8.0
- Windows 10 Versão 1809 (build 17763) ou superior / Windows 11

#### Comandos de Compilação
```powershell
# Clonar o repositório
git clone https://github.com/tomas-barros1/SkyTray-Weather.git
cd SkyTray-Weather

# Executar a suíte de testes automatizados
dotnet test WinuiWheaterForecastTray.Tests/WinuiWheaterForecastTray.Tests.csproj

# Compilar e publicar a versão Release
dotnet publish WinuiWheaterForecastTray/WinuiWheaterForecastTray.csproj -c Release -r win-x64 --self-contained true -p:Platform=x64

# Executar o instalador local
powershell -File Install-SkyTray.ps1
```

---

## 🧪 Testes Automatizados

O repositório inclui uma suíte completa de testes xUnit cobrindo:
- Deserialização de DTOs da API Open-Meteo.
- Mapeamento de códigos meteorológicos WMO para ícones e condições.
- Serviço de internacionalização (i18n) e fallbacks.
- Testes de integração contratual com a API live do Open-Meteo.

```powershell
dotnet test WinuiWheaterForecastTray.Tests/WinuiWheaterForecastTray.Tests.csproj --logger "console;verbosity=normal"
```

---

## 📄 Licença

Distribuído sob a licença MIT. Veja `LICENSE` para mais informações.
