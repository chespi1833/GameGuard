# 🛡️ GameGuard

> **Open-Source Real-Time Gaming Process Guardian, Strict Whitelist Enforcer & Deep RAM Optimizer for Windows 10/11**

[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Platform: Windows](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D6.svg)]()
[![.NET: 10.0](https://img.shields.io/badge/.NET-10.0%20WinForms-512BD4.svg)]()

**GameGuard** es una herramienta ligera de código abierto para Windows diseñada para convertir cualquier laptop o PC de escritorio en un entorno de **Modo Juego real sin interrupciones**.

A diferencia de los "Game Boosters" comerciales tradicionales (que suelen ser pesados, consumen cientos de MB de RAM en telemetría/anuncios y solo hacen un barrido único al iniciar), **GameGuard** actúa como un **guardián en tiempo real a nivel de eventos del sistema (WMI)** que aplica una **Lista Blanca estricta**, impidiendo que procesos en segundo plano, actualizadores o servicios de telemetría vuelvan a abrirse mientras estás jugando.

---

## 🎯 ¿Por qué nace este proyecto? (Intenciones del Proyecto)

1. **Cero interrupciones en plena partida:** Los sistemas modernos ejecutan decenas de servicios de telemetría, actualizadores silenciosos (Office, navegadores, periféricos, suites de fabricantes OEM) e indexadores de disco que despiertan en medio de una partida provocando caídas de FPS (*stuttering*), picos de latencia o saturación de RAM en equipos de 8 GB o 16 GB.
2. **Lista Blanca Real + Vigilancia en Tiempo Real:** En lugar de intentar adivinar todos los programas del mundo que podrían molestarte, **GameGuard** permite definir exactamente qué aplicaciones tienen permiso de ejecutarse mientras juegas y **bloquea en milisegundos cualquier proceso no autorizado que intente abrirse**.
3. **Multimarca y Auto-Adaptable:** Ya sea que uses una laptop **HP, Lenovo, ASUS, MSI, Acer, Dell/Alienware, Sony VAIO, Samsung**, o una **PC de Escritorio armada** con procesador **Intel o AMD Ryzen** y gráfica **NVIDIA o AMD Radeon**, **GameGuard** detecta automáticamente el hardware en su primer arranque:
   * **Protege en la Lista Blanca** únicamente los micro-servicios esenciales que controlan la curva de ventiladores, sensores térmicos, límites de potencia (PL1/PL2), audio, Bluetooth, touchpad y sistemas **Anti-Cheat** (*BattlEye, Zakynthos, EasyAntiCheat*).
   * **Envía a la Lista Negra y pausa** todo el *bloatware*, interfaces pesadas y telemetría del fabricante.
4. **100% Open Source, Transparente y Ultraligero:** Sin cuentas obligatorias, sin anuncios, sin telemetría y con un consumo mínimo de memoria.

---

## ✨ Características Principales

* 🛡️ **Guardián en Tiempo Real por Eventos WMI (`Win32_ProcessStartTrace`):**
  No consume CPU haciendo *polling* constante. Se suscribe a los eventos de creación de procesos de Windows (con respaldo diferencial ligero) y termina instantáneamente cualquier proceso prohibido o fuera de la Lista Blanca que intente iniciarse mientras el Modo Juego está activo.
* 🎮 **Detección Inteligente de Juegos e Hijos de Launchers (`Steam`, `Epic`, `Battle.net`, etc.):**
  Autoriza automáticamente cualquier juego que lances desde tu launcher (o que resida en carpetas de bibliotecas de juegos como `\steamapps\common\`), eleva su prioridad de CPU a **Alta (`High`)** y protege sus procesos **Anti-Cheat** (`BEService`, `zksvc`, `EasyAntiCheat`).
* 🧹 **Purgado Profundo de Memoria RAM (Estilo *MemReduct*) con Protección de Juego:**
  Utiliza las APIs nativas de Windows (`EmptyWorkingSet` y `NtSetSystemInformation`) para vaciar los *Working Sets* de procesos en segundo plano y la *Standby List* de caché del sistema, **excluyendo automáticamente los PIDs de tu juego activo** para jamás enviar las texturas del juego al archivo de paginación.
* 🪶 **Modo "Steam Ligero" (Ahorro de ~450–500 MB de RAM):**
  Reduce la prioridad de los procesos `steamwebhelper.exe` y recorta periódicamente su huella de memoria mientras estás jugando, sin cerrar Steam ni romper el overlay (`Shift+Tab`).
* ⏸️ **Congelador Inteligente de Servicios de Windows y OEM:**
  Toma una instantánea de los servicios prescindibles en ejecución (`Windows Search`, `SysMain / Superfetch`, `Orquestador de Windows Update`, `Office Click-to-Run`, telemetría OEM, etc.), los detiene durante tu sesión de juego y **restaura exactamente los que estaban encendidos** cuando desactivas el Modo Juego.
* ⚡ **Conmutador Automático de Plan de Energía:**
  Activa el perfil **Ultimate Performance / Alto Rendimiento** de Windows al entrar en Modo Juego y restaura el plan **Equilibrado** al salir.
* 🤖 **Motor de Auto-Detección Multimarca (`HardwareProfileDetector`):**
  Detecta fabricante, tipo de chasis (*Laptop vs. Escritorio*), CPU (*Intel vs. AMD*) y GPU (*NVIDIA / Radeon / Intel*), además de escanear heurísticamente servicios de telemetría y actualizadores instalados en la máquina.
* 🔍 **Modo Simulación (*Dry Run*):**
  Permite previsualizar en el registro en vivo exactamente qué procesos se cerrarían, cuánta RAM liberarían y qué servicios se pausarían antes de activar el Modo Juego.

---

## 🔒 Arquitectura de Seguridad de 3 Niveles (Prevención de BSOD)

Matar procesos del sistema operativo a ciegas en Windows provoca pantallazos azules (BSOD). **GameGuard** implementa una arquitectura de evaluación en capas en [`ProcessGuardian.cs`](GameGuard/Services/ProcessGuardian.cs):

1. **`KernelSafeList` (Inmunidad Absoluta del Kernel y Sesión):** Procesos críticos de Windows (`System`, `smss`, `csrss`, `wininit`, `winlogon`, `services`, `lsass`, `svchost`, `dwm`, `explorer`, `WUDFHost`, `wlanext`, `audiodg`, etc.) y binarios nativos en `C:\Windows\System32` están codificados como intocables.
2. **Lista Blanca de Hardware, Periféricos y Juegos:** Demonios térmicos del equipo, agentes de mouse/teclado (Logitech, Razer, Corsair, SteelSeries), drivers de GPU/Audio/Bluetooth, clientes de juegos y servicios Anti-Cheat.
3. **Lista Negra Explícita + Lista Blanca Estricta de Usuario:** Cierra todo el *bloatware* conocido (incluso si se esconde en `DriverStore` o `SystemApps`) y cualquier aplicación de usuario que no esté en tu Lista Blanca.

---

## 🚀 Instalación y Uso

### Opción 1: Usar el Instalador Gráfico (`GameGuard_Setup.exe`)
1. Ejecuta `Instalador\GameGuard_Setup.exe` (solicitará permisos de Administrador).
2. El asistente instalará **GameGuard** para todos los usuarios del equipo en `C:\Program Files\GameGuard`, creará accesos directos en el Escritorio y Menú Inicio, y configurará el perfil compartido en `C:\ProgramData\GameGuard\config.json`.
3. También soporta instalación silenciosa por consola:
   ```powershell
   .\Instalador\GameGuard_Setup.exe --silent
   ```

### Opción 2: Compilar desde el Código Fuente
Requisitos: **.NET 10.0 SDK** en Windows.

```powershell
# Compilar la aplicación principal
dotnet build GameGuard\GameGuard.csproj -c Release

# O generar automáticamente el instalador de archivo único (GameGuard_Setup.exe)
.\build_installer.ps1
```

---

## 📂 Estructura del Proyecto

* `GameGuard/` — Aplicación principal de escritorio (C# / .NET 10 WinForms + System Tray).
  * `Models/GameGuardConfig.cs` — Modelo de configuración (`config.json`), listas por defecto y `KernelSafeList`.
  * `Services/ProcessGuardian.cs` — Motor de vigilancia en tiempo real por eventos WMI (`Win32_ProcessStartTrace`), árbol de hijos de launchers y evaluación de procesos.
  * `Services/HardwareProfileDetector.cs` — Detector automático multimarca (OEM, Laptop/Desktop, CPU Intel/AMD, GPU NVIDIA/AMD/Intel y heurística).
  * `Services/MemoryOptimizer.cs` — Purgado profundo de RAM (`EmptyWorkingSet` + `NtSetSystemInformation`) y Modo *Steam Ligero*.
  * `Services/WindowsServiceManager.cs` — Pausa y restauración segura de servicios de Windows.
  * `Services/PowerPlanManager.cs` — Gestión de planes de energía de Windows (`powercfg`).
  * `MainForm.cs` — Interfaz gráfica oscura con telemetría en vivo, editor de listas con selector de ejecutables (`Examinar...`) e icono en la bandeja del sistema.
* `GameGuard.Setup/` — Instalador y desinstalador gráfico multi-usuario de archivo único (`GameGuard_Setup.exe`).
* `ANALISIS_PROCESOS_Y_MODO_JUEGO.md` — Documentación técnica de procesos de Windows, telemetría OEM, matriz multimarca y análisis de procesos de juego/Anti-Cheat.

---

## 🤝 Contribuciones

¡Las contribuciones de la comunidad son bienvenidas! Si quieres añadir reglas de detección para nuevas marcas de laptops, placas base, periféricos o sistemas Anti-Cheat:
1. Haz un *Fork* del repositorio.
2. Agrega los procesos/servicios correspondientes en `GameGuard/Services/HardwareProfileDetector.cs` o `GameGuard/Models/GameGuardConfig.cs`.
3. Abre un *Pull Request* explicando qué procesos controlan el hardware (Lista Blanca) y cuáles son telemetría/bloatware (Lista Negra).

---

## 📄 Licencia

Distribuido bajo la licencia **MIT**. Consulta el archivo [LICENSE](LICENSE) para más detalles.
