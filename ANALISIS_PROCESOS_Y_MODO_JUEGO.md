# Auditoría de Procesos y Servicios Activos — Laptop HP OMEN

Este documento contiene el inventario real de los **procesos y servicios que están corriendo actualmente en tu laptop**, ordenados desde **los más prescindibles (Nivel 1: Matar / Bloquear siempre al jugar)** hasta **los más críticos e intocables del sistema (Nivel 5: Kernel / Sistema Operativo)**.

---

## Resumen de Consumo Detectado (Top Procesos)
En el escaneo actual se detectaron **78 grupos de procesos activos** y **114 servicios en ejecución**. Solo cerrando los procesos de las **Categorías 1 y 2** liberarás entre **600 MB y 1.5 GB de RAM** y eliminarás despertares de CPU/disco en segundo plano.

---

## Categoría 1: 100% Prescindibles en Modo Juego (Matar y Prohibir Reapertura)
*Estos procesos son telemetría, widgets, caché de feeds, actualizadores o utilidades en segundo plano que no aportan nada mientras juegas y suelen causar picos de uso de CPU, red o disco.*

| Proceso (`.exe`) | Servicio Asociado | RAM Actual | ¿Qué es y qué hace? | ¿Por qué SI puedes matarlo en Modo Juego? |
| :--- | :--- | :---: | :--- | :--- |
| `TouchpointAnalyticsClientService` | `HpTouchpointAnalyticsService` | 49.8 MB | **HP Insights Analytics:** Recolecta telemetría de uso y diagnóstico de tu laptop para enviarla a HP. | Es puro spyware/telemetría corporativa de HP. Consume RAM, disco y red sin darte ningún beneficio. |
| `SysInfoCap` | `HPSysInfoCap` | 72.5 MB | **HP System Info HSA Service:** Recopila información del sistema para mostrarla en las apps de soporte de HP. | Consume más de 72 MB de RAM solo para leer datos del sistema para el software de HP. No afecta a los ventiladores. |
| `DiagsCap` | `HPDiagsCap` | 29.7 MB | **HP Diagnostics HSA Service:** Servicio de diagnóstico de hardware para HP Support Assistant. | Inútil mientras juegas. Solo sirve si estás corriendo un test de diagnóstico de HP. |
| `NetworkCap` | `HPNetworkCap` | 30.9 MB | **HP Network HSA Service:** Módulo del "Network Booster" de OMEN. | A menos que uses la priorización de red de OMEN, suele meter latencia extra inspeccionando paquetes. |
| `AppHelperCap` | `HPAppHelperCap` | 30.6 MB | **HP App Helper HSA Service:** Servicio auxiliar para conectar apps UWP de HP con el sistema. | No controla hardware crítico; es un intermediario de software de HP. |
| `BridgeCommunication` | *(Hijo de servicios HP)* | 32.1 MB | Puente de comunicación interna entre los servicios de HP y la interfaz de usuario. | Si no tienes abierta la ventana de OMEN Gaming Hub o HP Support, no sirve de nada. |
| `OfficeClickToRun` | `ClickToRunSvc` | 41.1 MB | **Microsoft Office Click-to-Run:** Mantiene Word/Excel listos para abrir rápido y busca actualizaciones de Office en segundo plano. | Mientras juegas no vas a actualizar Office. Matarlo evita que descargue parches en plena partida. |
| `MicrosoftStartFeedProvider` | *(Tarea UWP)* | 35.8 MB | Descarga noticias, clima y anuncios para el panel de Widgets de Windows 11. | Consume red y CPU periódicamente para actualizar noticias que no estás viendo. |
| `WidgetBoard` / `WidgetService` | *(Tarea UWP)* | 51.9 MB | Motor del tablero de Widgets de Windows 11 (WebView2 en segundo plano). | Son más de 50 MB de RAM desperdiciados en el panel de noticias de la barra de tareas. |
| `CrossDeviceResume` | *(Tarea UWP)* | 49.1 MB | Sincronización de "Vincular a Windows / Continuar en PC" con tu teléfono móvil. | Innecesario mientras juegas; consume casi 50 MB de RAM constantemente. |
| `Sleekshot` | *(App de Usuario)* | 127.4 MB | Herramienta de capturas de pantalla (`Sleekshot`). | Consume **127 MB de RAM**. Si al jugar usas las capturas de Steam (`F12`) o NVIDIA, puedes cerrarlo para ahorrar mucha memoria. |
| `SearchHost` / `SearchIndexer` | `WSearch` | 156.0 MB | **Búsqueda de Windows e Indexador:** Indexa archivos del disco constantemente y mantiene la interfaz de búsqueda en memoria. | El indexador (`WSearch`) es famoso por usar el SSD en momentos inoportunos. Windows recrea `SearchHost` solo si abres la lupa. |
| `logioptionsplus_updater` | `OptionsPlusUpdaterService` | 24.6 MB | Buscador de actualizaciones automáticas del software de tu mouse/teclado Logitech. | El actualizador es 100% prescindible mientras juegas (el agente del mouse es aparte). |
| `sqlwriter` | `SQLWriter` | 9.7 MB | **SQL Server VSS Writer:** Servicio de copias de seguridad para bases de datos SQL Server (instalado por Visual Studio/herramientas de desarrollo). | Solo sirve si tienes una base de datos SQL en producción haciendo backups en caliente. |
| `PresentationFontCache` | `FontCache3.0.0.0` | 24.6 MB | Caché de fuentes para aplicaciones antiguas hechas en WPF (.NET). | Prescindible durante el juego. |
| `UserOOBEBroker` | *(Sistema Usuario)* | 10.1 MB | Broker de notificaciones de "Configuración inicial de Windows" (esos avisos de *"Termina de configurar tu PC"*). | Totalmente prescindible. |

---

## Categoría 2: Opcionales / Uso Diario (Depende de tus periféricos o de qué estés haciendo)
*Estos programas son útiles en el día a día, pero puedes elegir si van a tu **Lista Blanca** o a la **Lista Negra** según cómo juegues.*

| Proceso (`.exe`) | Servicio Asociado | RAM Actual | ¿Qué es y qué hace? | ¿Puedo matarlo o debo dejarlo en Lista Blanca? |
| :--- | :--- | :---: | :--- | :--- |
| `Antigravity` / `language_server` | *(App de Usuario)* | 1,079.7 MB | Tu IDE actual (Antigravity) y su servidor de análisis de código. | **Consume > 1 GB de RAM.** Obviamente mientras programamos ahora debe estar abierto, pero **cuando vayas a jugar debe cerrarse sí o sí**. |
| `logioptionsplus_agent` / `logioptionsplus_appbroker` | *(App de Usuario)* | 109.2 MB | **Logi Options+:** Gestiona los botones personalizados, DPI y gestos de tu mouse/teclado Logitech. | **Si usas botones mapeados o DPI específico en tu mouse Logitech al jugar:** Déjalo en **Lista Blanca**. Si usas un mouse básico o con memoria interna, puedes matarlo y ahorrar 110 MB. |
| `OmenCap` | `HPOmenCap` | 9.5 MB | **HP Omen HSA Service:** Es el único servicio ligero de HP que se comunica directamente con los perfiles térmicos de OMEN. | **Recomendado en Lista Blanca** (consume solo 9.5 MB) si quieres que el perfil de ventiladores de OMEN siga activo sin tener toda la interfaz pesada abierta. |
| `XtuService` | `XTU3SERVICE` | 40.0 MB | **Intel Extreme Tuning Utility (XTU):** Servicio que usa HP OMEN por debajo para aplicar *undervolt* o límites de potencia (PL1/PL2) a tu procesador Intel. | **Déjalo en Lista Blanca** si usas el modo Rendimiento o Undervolt de OMEN; si lo matas, el CPU podría volver a sus límites de energía por defecto. |
| `OneApp.IGCC.WinService` | `igccservice` | 35.8 MB | **Intel Graphics Command Center Service:** Gestión de color y perfiles de la gráfica integrada Intel. | Como en los juegos pesados usas la GPU dedicada **NVIDIA**, este servicio de la gráfica integrada se puede detener sin peligro. |
| `RtkNGUI64` | *(App Bandeja)* | 18.6 MB | Interfaz gráfica (icono de bandeja) del panel de sonido Realtek. | Puedes matar la interfaz (`RtkNGUI64.exe`); el sonido seguirá funcionando perfectamente gracias al driver y `RtkAudioService64`. |
| `SecurityHealthSystray` | *(App Bandeja)* | 12.2 MB | Solo es el icono del escudo de Windows Defender al lado del reloj. | Puedes matarlo para liberar recursos de bandeja sin desactivar la protección. |
| `SystemSettings` | *(App UWP)* | 1.9 MB | La ventana de "Configuración de Windows" suspendida en segundo plano. | Se puede cerrar sin problema. |

---

## Categoría 3: Necesarios para Jugar (Tu Lista Blanca Base de Juego)
*Estos son los procesos que tu guardián debe permitir porque ejecutan el juego, los gráficos, el sonido y tus periféricos.*

| Proceso (`.exe`) | Servicio Asociado | RAM Actual | ¿Qué es y qué hace? | ¿Por qué NO debes matarlo al jugar? |
| :--- | :--- | :---: | :--- | :--- |
| `steam` / `steamservice` | `Steam Client Service` | 119.3 MB | Cliente principal de Steam y su servicio de permisos/anticheat. | Necesario para lanzar tus juegos de Steam, validar DRM y conectar a servidores. |
| `steamwebhelper` (8 procesos) | *(Hijo de Steam)* | 633.3 MB | Navegador interno de Steam (tienda, amigos, overlay `Shift+Tab`). | **Tip de optimización:** Si matas todos los `steamwebhelper`, Steam los vuelve a abrir. Pero nuestro programa puede **iniciar Steam en modo `-nochatui -nofriendsui -silent`** o reducir su prioridad/memoria para que pase de 633 MB a ~80 MB. |
| `nvcontainer` / `NVDisplay.Container` | `NVDisplay.ContainerLocalSystem` / `NvContainerLocalSystem` | 208.5 MB | Contenedores del driver de tu tarjeta gráfica **NVIDIA** (panel de control, perfiles de juego, G-Sync, Optimus). | Si matas `NVDisplay.Container`, perderás el control del driver de pantalla, el cambio automático de GPU (Optimus) o el overlay de NVIDIA. |
| `RtkAudioService64` | `RtkAudioService` | 10.1 MB | Servicio de procesamiento de audio Realtek (ecualización, detección de auriculares en el jack). | Si lo matas, puede dejar de detectar cuando conectas o desconectas auriculares. |
| `RtkBtManServ` | `RtkBtManServ` | 9.2 MB | Servicio de Bluetooth Realtek. | **Vital si usas auriculares o mando (joystick) por Bluetooth.** Si juegas todo por cable/dongle USB, podrías pausarlo. |
| `SynTPEnh` / `SynTPEnhService` | `SynTPEnhService` | 34.2 MB | Driver avanzado del Touchpad Synaptics de tu laptop (incluye el rechazo de palma al apoyar la mano en el teclado `WASD`). | Si lo matas, el touchpad pierde funciones como desactivarse mientras escribes/juegas con el teclado. |
| `igfxEM` / `igfxCUIService` / `IntelCpHDCPSvc` / `IntelCpHeciSvc` | `cplspcon` / `cphs` / `igfxCUIService2.0.0.0` | 46.8 MB | Servicios del driver de la pantalla integrada Intel (en laptops con NVIDIA Optimus, la pantalla física de la laptop pasa por la iGPU Intel). | No conviene matarlos porque gestionan el brillo de pantalla, teclas rápidas de pantalla y salida de vídeo híbrida. |
| `esif_uf` | `esifsvc` | 6.3 MB | **Intel Dynamic Platform and Thermal Framework:** Controla sensores térmicos de la placa base y evita que la laptop se sobrecaliente. | **Crítico para laptops:** Nunca lo mates jugando, regula la temperatura entre CPU y chasis. |

---

## Categoría 4: Interfaz y Subsistema de Usuario de Windows (Mejor mantener, o manipular con cuidado)
*Procesos propios de la sesión gráfica de Windows.*

| Proceso (`.exe`) | RAM Actual | ¿Qué es y qué hace? | ¿Por qué NO conviene matarlo (o qué pasa si lo haces)? |
| :--- | :---: | :--- | :--- |
| `explorer` | 351.3 MB | **Explorador de Windows:** Barra de tareas, escritorio, `Alt+Tab` y gestor de carpetas. | *Técnicamente se puede matar* para ahorrar 350 MB de RAM mientras juegas (y relanzarlo al salir del juego), pero te quedarías sin barra de tareas ni `Win+D`. Podemos ponerlo como una opción **"Modo Extremo (Matar Explorer)"** opcional. |
| `dwm` | 81.4 MB | **Desktop Window Manager:** Compositor gráfico de Windows. Dibuja todas las ventanas y gestiona DirectX/Flip Model. | **Intocable.** Si lo matas, la pantalla parpadea en negro y Windows lo reinicia de inmediato, pudiendo crashear el juego abierto. |
| `TextInputHost` / `ctfmon` | 111.8 MB | Subsistema de entrada de teclado de Windows 11. | Si los matas, en varios juegos o launchers deja de funcionar el teclado para escribir en el chat o iniciar sesión. |
| `sihost` / `ShellHost` / `StartMenuExperienceHost` / `ApplicationFrameHost` / `RuntimeBroker` / `taskhostw` / `dllhost` / `conhost` | ~385 MB (total) | Infraestructura base de la sesión de usuario de Windows, permisos de apps y contenedores COM. | Matarlos causa inestabilidad en la sesión de Windows o se reinician solos gastando más CPU al recrearse. |
| `MsMpEng` / `MpDefenderCoreService` / `NisSrv` / `SecurityHealthService` / `DefenderAiPlatformHost` / `DefenderSessionHelper` | ~310 MB (total) | **Windows Defender Antivirus:** Motor de escaneo en tiempo real. | Windows protege estos procesos con **PPL (Protected Process Light)**: ni siquiera como Administrador puedes hacerles `taskkill`. **Truco:** Lo que sí puede hacer nuestro programa es bajar la prioridad o pausar tareas programadas de escaneo de Defender mientras juegas. |

---

## Categoría 5: Críticos del Kernel y Sistema Operativo (PROHIBIDO TOCAR — Causan Pantallazo Azul)
*Nuestro programa tendrá estos procesos en una **"Lista de Inmunidad Absoluta (Kernel Safe-List)"** codificada internamente para que jamás, bajo ningún concepto, intente cerrarlos.*

* `System` / `Idle` / `Registry` / `Memory Compression`: Hilos del propio núcleo (Kernel) de Windows y gestor de memoria comprimida.
* `smss.exe` (Session Manager Subsystem) / `csrss.exe` (Client Server Runtime) / `wininit.exe` / `winlogon.exe`: Si matas cualquiera de estos cuatro, Windows muestra un **Pantallazo Azul (BSOD) instantáneo**.
* `services.exe` / `lsass.exe` (Local Security Authority) / `svchost.exe`: Gestor de todos los servicios de Windows y autenticación de seguridad.
* `fontdrvhost.exe` / `WUDFHost.exe` / `dasHost.exe` / `wlanext.exe` / `WmiPrvSE.exe` / `unsecapp.exe`: Drivers en modo usuario, Wi-Fi (`wlanext.exe`, si lo matas pierdes el Wi-Fi) y proveedor WMI (que nuestro propio programa usará para vigilar procesos).

---

## Servicios de Windows (`svchost.exe`) que SÍ podemos Pausar/Detener en Modo Juego
Aunque no debemos matar el ejecutable `svchost.exe` a ciegas, cada `svchost.exe` aloja servicios específicos. Nuestro programa puede **detener limpiamente** estos servicios al activar el Modo Juego y **volver a encenderlos** al salir:

1. `WSearch` (**Windows Search**): Evita indexación de disco en segundo plano.
2. `SysMain` (**Superfetch**): Precarga apps de oficina en la RAM; en juegos modernos con SSD suele causar *stuttering* (tirones) de disco.
3. `UsoSvc` / `wuauserv` / `DoSvc` (**Orquestador de Windows Update y Optimización de Distribución**): Impide al 100% que Windows descargue o instale actualizaciones mientras estás jugando.
4. `HpTouchpointAnalyticsService` / `HPSysInfoCap` / `HPDiagsCap` / `HPAppHelperCap` / `HPNetworkCap`: Toda la suite de telemetría y bloatware de HP.
5. `ClickToRunSvc` (**Microsoft Office**): Detiene actualizaciones y procesos de Office.
6. `OptionsPlusUpdaterService` (**Logitech Updater**): Detiene actualizaciones de Logitech.
7. `SQLWriter`: Detiene el servicio VSS de SQL Server.
8. `PcaSvc` (**Asistente para compatibilidad de programas**) y `DPS` (**Directivas de diagnóstico**): Monitores de telemetría de fallos de Windows que consumen recursos.

---

## Paso 2: Especificación Aprobada del Programa ("GameGuard")

Según tus elecciones, así queda definida la configuración y arquitectura que vamos a desarrollar en **C# / .NET (WinForms + System Tray)**:

### 1. Configuración de Tu Lista Blanca Personalizada
* **Hardware y Potencia (Permitidos):** `OmenCap.exe` (`HPOmenCap`) y `XtuService.exe` (`XTU3SERVICE`) para mantener perfiles térmicos, ventiladores y límites de potencia del procesador.
* **Periféricos Logitech (Permitidos):** `logioptionsplus_agent.exe` y `logioptionsplus_appbroker.exe` para mantener DPI y botones del mouse/teclado (pero se bloquea `logioptionsplus_updater.exe`).
* **Bluetooth y Audio (Permitidos):** `RtkBtManServ.exe`, `RtkAudioService64.exe` y servicios Bluetooth de Windows para auriculares/mando inalámbrico.
* **Gráficos y Touchpad (Permitidos):** Contenedores NVIDIA (`nvcontainer`, `NVDisplay.Container`), servicios Intel DPTF/Display (`esif_uf`, `igfxEM`) y Touchpad Synaptics (`SynTPEnh`).
* **Steam y Juegos (Permitidos + Optimizados):** `steam.exe`, `steamservice.exe` y **cualquier juego hijo lanzado por Steam** (autorización automática).
* **Bloqueados / Cerrados al Jugar:** Todo el bloatware/telemetría OEM (`TouchpointAnalyticsClientService`, `SysInfoCap`, `DiagsCap`, `NetworkCap`, `AppHelperCap`, `BridgeCommunication`), `Sleekshot` (~127 MB), `OfficeClickToRun`, `SearchHost`/`SearchIndexer`, `WidgetBoard`/`WidgetService`, `CrossDeviceResume`, `MicrosoftStartFeedProvider`, `sqlwriter`, `OneApp.IGCC.WinService`, `RtkNGUI64`, `Antigravity` y cualquier proceso de usuario que no esté en la Lista Blanca.

### 2. Características Incluidas en `GameGuard`
1. **Interfaz de Escritorio Oscura + Bandeja del Sistema (System Tray):**
   * Panel de control visual con métricas en tiempo real (RAM libre, RAM liberada, procesos bloqueados, servicios pausados, juego activo).
   * Minimizado a la bandeja del sistema (junto al reloj) con menú rápido de clic derecho.
   * Gestor integrado para agregar o quitar programas de la Lista Blanca o Negra (incluyendo selector de archivos `Examinar...`).
2. **Guardián en Tiempo Real por Eventos WMI (`Win32_ProcessStartTrace`):**
   * Cero consumo de CPU en reposo: despierta únicamente cuando Windows crea un proceso nuevo.
   * Si el proceso está prohibido o no está en la Lista Blanca de usuario, lo termina al instante.
3. **Detección Inteligente de Hijos de Steam + Boost de CPU:**
   * Detecta automáticamente cuando `steam.exe` lanza un juego (o su sub-launcher), lo autoriza dinámicamente sin que tengas que editar listas, y eleva su prioridad de CPU a **Alta (`High`)**.
4. **Modo "Steam Ligero" (Ahorro de ~500 MB de RAM):**
   * Reduce la prioridad de todos los procesos `steamwebhelper.exe` a `BelowNormal` y vacía periódicamente su *Working Set* de memoria mientras juegas, reduciendo su huella de ~630 MB a ~80-120 MB sin cerrar Steam.
5. **Purgado Profundo de RAM (Estilo MemReduct):**
   * Usa las APIs nativas de Windows (`K32EmptyWorkingSet` y `NtSetSystemInformation`) para vaciar los *Working Sets* y la *Standby List* de memoria caché al activar el Modo Juego.
6. **Cambio Automático de Plan de Energía:**
   * Activa el plan **Ultimate Performance** (`d5071f6b-3d62-442f-9b94-acae1129cdc4`) al entrar en Modo Juego y permite volver al plan **Equilibrado** (`381b4222-f694-41f0-9685-ff5bb260df2e`) al salir.


---

## Paso 3: Motor de Auto-Detección Dinámica Multimarca (`HardwareProfileDetector`)

Para que puedas instalar y usar este programa en **cualquier otra PC (Laptop o Escritorio, cualquier marca y procesador/gráfica)**, se implementó el motor [`HardwareProfileDetector`](GameGuard/Services/HardwareProfileDetector.cs) que se ejecuta automáticamente al abrir el programa en una máquina nueva (o al pulsar el botón **`🤖 Auto-Detectar`**).

### 1. ¿Qué consulta el motor al arrancar?
1. **`Win32_ComputerSystem` y `Win32_Battery`:** Identifica el fabricante, modelo y si el chasis es **Laptop (Mobile)** o **PC de Escritorio (Desktop)**.
   * *Si es Laptop:* Protege los drivers de Touchpad (`SynTPEnh`, `ETDCtrl`, `AsusTPHelper`) para mantener el rechazo de palma al jugar con teclado.
   * *Si es PC de Escritorio:* Pasa los procesos de Touchpad y sincronización móvil a la Lista Negra.
2. **`Win32_Processor`:** Identifica si el procesador es **Intel** o **AMD Ryzen**.
   * *Si es Intel:* Protege los demonios térmicos y de potencia (`esif_uf`, `XtuService`) y bloquea la telemetría/actualizadores de Intel (`DSAService`, `DSAUpdateService`, `IntelAnalyticsService`).
   * *Si es AMD:* Protege `AMDRyzenMasterDriver` y bloquea la telemetría `AUEPMaster` / `AUEPLauncher` (AMD User Experience Program).
3. **`Win32_VideoController`:** Identifica todas las GPUs instaladas (**NVIDIA**, **AMD Radeon**, **Intel Arc/Iris/UHD**).
   * *Si es NVIDIA:* Protege `nvcontainer`, `NVDisplay.Container`, `NVIDIA Overlay` y bloquea `NvTelemetryContainer`.
   * *Si es AMD Radeon:* Protege `atiesrxx`, `atieclxx`, `RadeonSoftware` y bloquea `AMDBugReportTool`.
   * *Gráfica Híbrida (iGPU Intel + GPU dedicada NVIDIA/AMD):* Si detecta gráfica dedicada, pausa el servicio `OneApp.IGCC.WinService` (`igccservice`) de Intel mientras juegas.

### 2. Matriz Multimarca (Separación de Control Térmico vs Bloatware por Fabricante)

| Marca / OEM | ✅ Se agrega a Lista Blanca (Ventiladores / Energía) | 🚫 Se agrega a Lista Negra y Servicios a Pausar (Bloatware / Telemetría) |
| :--- | :--- | :--- |
| **HP (OMEN / Victus / Pavilion)** | `OmenCap`, `XtuService` | `TouchpointAnalyticsClientService`, `SysInfoCap`, `DiagsCap`, `NetworkCap`, `AppHelperCap`, `BridgeCommunication`, `HP.Omen.OmenCommandCenter`, `HPJumpStart*` |
| **Lenovo (Legion / LOQ / IdeaPad / ThinkPad)** | `LITSSvc` *(Lenovo Intelligent Thermal Solution `Fn+Q`)*, `LenovoHotkeys` | `LenovoVantage`, `LenovoVantageService`, `Lenovo.Modern.ImController`, `UDClientService`, `LenovoArena`, `LenovoNow` |
| **ASUS (ROG / TUF / Placas ROG)** | `AsusOptimization` *(Perfiles `Fn+F5`)*, `ATKOSD2` | `ArmouryCrate`, `ArmouryCrate.UserSessionHelper`, `ArmourySocketServer`, `AsusFramework`, `AsusSoftwareManager`, `AsusLinkNear`, `MyASUS`, `GameSDK` |
| **MSI (Raider / Katana / Placas MSI)** | `MSICentralService` | `MSI.Companion`, `MSI.TerminalServer`, `Dragon Center`, `MSI.VoiceControlService`, `NahimicCompanion`, `LightKeeperService` |
| **Acer (Predator / Nitro / Aspire)** | `PSAdminService`, `PSSvc`, `NitroSenseService` *(Demonios de ventiladores)* | `PredatorSense` (UI), `NitroSense` (UI), `AcerCareCenter`, `ACCStd`, `AcerAgentSvc`, `AcerJumpStart`, `UEIPSvc` *(Telemetría)* |
| **Dell / Alienware** | `AWCCService`, `AWCC.Background.Server` | `SupportAssistAgent`, `DellDataVault`, `DDVDataCollector`, `DellTechHub`, `AWCC` (UI) |
| **Sony (VAIO)** | `VESMgr`, `VESMgrSub`, `SPMgr` *(VAIO Power Management)* | `VAIOCare`, `VCSystemTray`, `VAIOMessenger`, `VAIOUpdate`, `VAIOSampleCollector`, `SampleCollector` |
| **Samsung / Gigabyte (AORUS)** | `SamsungSystemSupportEngine` | `SamsungMagician`, `SamsungUpdate`, `GCC` (Gigabyte Control Center), `RGBFusion`, `AppCenter` |
| **Periféricos (Logitech, Razer, Corsair, SteelSeries)** | `logioptionsplus_agent`, `lghub_agent`, `Razer Synapse Service`, `iCUE`, `SteelSeriesSonar` | `logioptionsplus_updater`, `lghub_updater`, `RazerIngameEngine`, `GameManagerService3`, `RazerAxon`, `Corsair.Service.CpuIdRemote`, `SteelSeriesUpdateService` |

### 3. Escaneo Heurístico en Vivo (Cualquier marca o software desconocido)
Además de la base de datos por fabricante, [`HardwareProfileDetector`](GameGuard/Services/HardwareProfileDetector.cs) analiza todos los procesos y servicios activos en la PC buscando patrones heurísticos de telemetría y actualizadores (`*Telemetry*`, `*Analytics*`, `*Updater*`, `*UpdateService*`, `*Diagnostics HSA*`, `*CrashReport*`, `*SupportAssist*`) que no pertenezcan al Kernel de Windows y los incorpora automáticamente a la Lista Negra de esa máquina.


---

## Paso 4: Procesos Detectados de PUBG (PlayerUnknown's Battlegrounds) y Anti-Cheat

Al escanear tu laptop con **PUBG** abierto, se identificaron los siguientes ejecutables y servicios en ejecución (más los binarios auxiliares en la carpeta del juego) y se agregaron a la **Lista Blanca** por defecto:

| Proceso (`.exe`) | PID / Servicio | RAM Actual | ¿Qué es y por qué está en la Lista Blanca? |
| :--- | :--- | :---: | :--- |
| `TslGame` (2 instancias) | Juego Principal | ~3,323.5 MB | **Cliente principal de PUBG (`TslGame.exe`).** Se detecta como juego activo, se eleva su prioridad de CPU y **se protege su memoria RAM** durante el purgado para no vaciar sus texturas al archivo de paginación. |
| `TslGame_BE` | Wrapper BattlEye | 11.9 MB | Lanzador puente entre PUBG y el sistema anti-trampas BattlEye (`C:\Program Files (x86)\Steam\steamapps\common\PUBG\TslGame\Binaries\Win64\TslGame_BE.exe`). |
| `BEService` / `BEService_pubg` / `BEService_x64` | Servicio `BEService` | 35.1 MB | **BattlEye Service (`C:\Program Files (x86)\Common Files\BattlEye\BEService.exe`).** Si se cierra o bloquea, el servidor de PUBG te expulsa de la partida por error de Anti-Cheat. |
| `zksvc` | Servicio `zksvc` | 12.4 MB | **Zakynthos Service (`C:\Program Files\Common Files\PUBG\zksvc.exe`).** Sistema anti-trampas propietario de KRAFTON requerido por PUBG. |
| `ExecPubg` / `TslGame_ZK` | Bootstrapper | *(Al iniciar)* | Ejecutables iniciales que lanza Steam para arrancar PUBG y sus servicios antes de abrir `TslGame.exe`. |
| `UnrealCEFSubProcess_3071` (`UnrealCEFSubProcess*`) | Motor Web Interno | *(En menús)* | Navegador embebido de Unreal Engine que usa PUBG para renderizar el lobby, eventos y tienda dentro del juego. |
| `iigw_server` / `BroCrashReporter` | Auxiliares PUBG | *(Bajo demanda)* | Servidor de pasarela interna de PUBG y gestor de reportes de cierre. |
| `gameoverlayui64` | Hijo de Steam | 33.1 MB | Versión de 64 bits del Overlay de Steam (`Shift+Tab`) que se engancha a juegos DirectX 11/12 de 64 bits como PUBG. |
