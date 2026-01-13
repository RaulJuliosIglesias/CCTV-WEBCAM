<div align="center">

# 🎥 RTSP VirtualCam

### Transforma cualquier cámara RTSP en una webcam virtual

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![Windows 11](https://img.shields.io/badge/Windows-11-0078D4?style=for-the-badge&logo=windows11)](https://www.microsoft.com/windows)
[![License: MIT](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)](../LICENSE)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen?style=for-the-badge)](../CONTRIBUTING.md)

<p align="center">
  <a href="../README.md">🇬🇧 English</a> | <strong>🇪🇸 Español</strong>
</p>

---

**Una aplicación de escritorio Windows ligera que conecta cámaras PTZ Hikvision (o cualquier stream RTSP) y las virtualiza como webcams para usar en Zoom, Teams, Google Meet y otras aplicaciones de videoconferencia.**

</div>

---

## ✨ Características

| Característica | Descripción |
|----------------|-------------|
| 🔌 **Conexión Fácil** | Solo pega tu URL RTSP y haz clic en "Virtualizar" |
| ⚡ **Baja Latencia** | Optimizado para streaming en tiempo real con buffer de 300ms |
| 🚫 **Sin Drivers** | Usa la API nativa MFCreateVirtualCamera de Windows 11 |
| 📺 **Universal** | Funciona con Zoom, Teams, Meet, OBS, Discord y más |
| 🎨 **UI Moderna** | Interfaz WPF limpia con indicadores de estado |
| 💾 **Historial de URLs** | Recuerda tus últimas 10 conexiones |

---

## 📋 Requisitos

| Requisito | Detalles |
|-----------|----------|
| **Sistema Operativo** | Windows 11 (Build 22000+) |
| **Runtime** | .NET 8 (incluido en versión portable) |
| **Red** | Acceso al stream RTSP de la cámara |

> ⚠️ **Nota**: Windows 10 no está soportado debido a la falta de API de cámara virtual.

---

## 🚀 Inicio Rápido

### Opción 1: Descargar Versión Portable (Recomendado)

1. Descarga la última versión desde [Releases](../../releases)
2. Extrae `RTSPVirtualCam-portable.zip`
3. Ejecuta `RTSPVirtualCam.exe`
4. ¡No requiere instalación!

### Opción 2: Compilar desde Código Fuente

```powershell
# Clonar el repositorio
git clone https://github.com/YOUR_USERNAME/CCTV-WEBCAM.git
cd CCTV-WEBCAM/RTSPVirtualCam

# Restaurar y compilar
dotnet restore
dotnet build

# Ejecutar
dotnet run --project src/RTSPVirtualCam
```

---

## 📖 Guía de Uso

### Paso 1: Ingresa la URL RTSP

Ingresa la URL RTSP de tu cámara en el formato:
```
rtsp://usuario:contraseña@IP:puerto/ruta
```

### Paso 2: Vista Previa

Haz clic en **▶ Preview** para verificar que el stream funciona correctamente.

### Paso 3: Virtualizar

Haz clic en **📹 Virtualize** para crear la cámara virtual.

### Paso 4: Usar en Aplicaciones

Selecciona **"RTSP VirtualCam"** como tu cámara en cualquier aplicación de videoconferencia.

---

## 🔧 Ejemplos de URLs RTSP

<details>
<summary><b>Cámaras Hikvision</b></summary>

```bash
# Stream principal (1080p/4K)
rtsp://admin:password@192.168.1.100:554/Streaming/Channels/101

# Stream secundario (720p/menor)
rtsp://admin:password@192.168.1.100:554/Streaming/Channels/102

# Tercer stream  
rtsp://admin:password@192.168.1.100:554/Streaming/Channels/103
```
</details>

<details>
<summary><b>Cámaras Dahua</b></summary>

```bash
# Stream principal
rtsp://admin:password@192.168.1.100:554/cam/realmonitor?channel=1&subtype=0

# Stream secundario
rtsp://admin:password@192.168.1.100:554/cam/realmonitor?channel=1&subtype=1
```
</details>

<details>
<summary><b>ONVIF Genérico</b></summary>

```bash
rtsp://admin:password@192.168.1.100:554/onvif1
rtsp://admin:password@192.168.1.100:554/stream1
```
</details>

<details>
<summary><b>Streams de Prueba (para desarrollo)</b></summary>

```bash
rtsp://wowzaec2demo.streamlock.net/vod/mp4:BigBuckBunny_115k.mp4
```
</details>

---

## 📁 Estructura del Proyecto

```
RTSPVirtualCam/
├── 📂 .github/                    # Configuración de GitHub
│   ├── workflows/                 # Pipelines CI/CD
│   └── ISSUE_TEMPLATE/            # Plantillas de issues
│
├── 📂 docs/                       # Documentación
│   ├── README_ES.md               # Documentación en español
│   ├── INSTALLATION.md            # Guía de instalación
│   ├── USER_GUIDE.md              # Manual de usuario
│   ├── DEVELOPMENT.md             # Guía de desarrollo
│   └── TROUBLESHOOTING.md         # Problemas comunes
│
├── 📂 scripts/                    # Scripts de utilidad
│   ├── build-release.ps1          # Compilar versión release
│   └── publish-portable.ps1       # Crear versión portable
│
├── 📂 src/RTSPVirtualCam/         # Aplicación principal
│   ├── 📂 Models/                 # Modelos de datos
│   │   ├── ConnectionInfo.cs
│   │   ├── CameraSettings.cs
│   │   └── AppSettings.cs
│   ├── 📂 Services/               # Lógica de negocio
│   │   ├── IRtspService.cs
│   │   ├── RtspService.cs
│   │   ├── IVirtualCameraService.cs
│   │   └── VirtualCameraService.cs
│   ├── 📂 ViewModels/             # ViewModels MVVM
│   │   └── MainViewModel.cs
│   ├── 📂 Views/                  # Vistas WPF
│   │   ├── MainWindow.xaml
│   │   └── MainWindow.xaml.cs
│   ├── 📂 Helpers/                # Utilidades
│   │   └── Converters.cs
│   ├── App.xaml
│   ├── App.xaml.cs
│   └── appsettings.json
│
├── 📄 RTSPVirtualCam.sln          # Archivo de solución
├── 📄 README.md                   # Documentación principal
├── 📄 LICENSE                     # Licencia MIT
└── 📄 .gitignore                  # Reglas de git ignore
```

---

## 🛠️ Stack Tecnológico

| Tecnología | Versión | Propósito |
|------------|---------|-----------|
| **.NET** | 8.0 | Runtime y Framework |
| **WPF** | - | Interfaz de Usuario |
| **LibVLCSharp** | 3.8.5 | Streaming y decodificación RTSP |
| **CommunityToolkit.MVVM** | 8.2.2 | Patrón MVVM |
| **Serilog** | 4.0.0 | Logging |
| **DirectN** | 1.18.0 | Interoperabilidad con API de Windows |

---

## 📦 Crear Versión Portable

Para crear un ejecutable portable auto-contenido:

```powershell
# Navegar al proyecto
cd RTSPVirtualCam

# Compilar release portable
dotnet publish src/RTSPVirtualCam -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish

# El ejecutable estará en:
# ./publish/RTSPVirtualCam.exe
```

### 📍 Ubicación del Ejecutable

Después de compilar, encuentra tu `.exe` portable en:

```
RTSPVirtualCam/
└── publish/
    └── RTSPVirtualCam.exe  ← Ejecutable portable (auto-contenido)
```

O en modo debug:
```
RTSPVirtualCam/
└── src/RTSPVirtualCam/bin/Debug/net8.0-windows/win-x64/
    └── RTSPVirtualCam.exe
```

---

## 🗺️ Hoja de Ruta

### ✅ v1.0 - MVP (Actual)
- [x] Conexión stream RTSP via LibVLC
- [x] Vista previa en la aplicación
- [x] Servicio de cámara virtual (placeholder)
- [x] UI WPF moderna
- [x] Historial de URLs
- [x] Logging

### 🔄 v1.1 - Mejorada
- [ ] Implementación completa de MFCreateVirtualCamera
- [ ] Persistencia de configuraciones
- [ ] Auto-reconexión en desconexión
- [ ] Soporte para bandeja del sistema
- [ ] Tema modo oscuro

### 🔮 v1.2 - Avanzada
- [ ] Múltiples cámaras simultáneas
- [ ] Integración de control PTZ
- [ ] Aceleración por hardware (DXVA2)
- [ ] Soporte para Windows 10 (DirectShow)
- [ ] Paquete de instalación

---

## 🤝 Contribuir

¡Las contribuciones son bienvenidas! Por favor lee nuestra [Guía de Contribución](../CONTRIBUTING.md) primero.

```bash
# Haz fork del repositorio
# Crea tu rama de feature
git checkout -b feature/caracteristica-increible

# Haz commit de tus cambios
git commit -m "Agrega característica increíble"

# Push a la rama
git push origin feature/caracteristica-increible

# Abre un Pull Request
```

---

## 🐛 Solución de Problemas

<details>
<summary><b>La cámara no aparece en las aplicaciones de video</b></summary>

1. Reinicia la aplicación de videoconferencia
2. Verifica que la configuración de privacidad de Windows permita acceso a la cámara
3. Verifica Windows 11 Build 22000 o superior
</details>

<details>
<summary><b>Timeout de conexión</b></summary>

1. Verifica que la IP y puerto de la cámara sean correctos
2. Comprueba la conectividad de red hacia la cámara
3. Asegúrate de que RTSP esté habilitado en la cámara
4. Intenta usar transporte TCP (`--rtsp-tcp`)
</details>

<details>
<summary><b>Pantalla negra en vista previa</b></summary>

1. Verifica las credenciales de la cámara
2. Verifica el formato de la URL del stream
3. Prueba primero con VLC player
</details>

---

## 📄 Licencia

Este proyecto está licenciado bajo la Licencia MIT - ver el archivo [LICENSE](../LICENSE) para más detalles.

---

## 🙏 Agradecimientos

- [VCamNetSample](https://github.com/smourier/VCamNetSample) - Implementación de referencia de cámara virtual
- [LibVLCSharp](https://github.com/videolan/libvlcsharp) - Bindings de VLC para .NET
- [CommunityToolkit.MVVM](https://github.com/CommunityToolkit/dotnet) - Toolkit MVVM

---

<div align="center">

**Hecho con ❤️ para la comunidad open source**

⭐ ¡Dale estrella a este repositorio si te resulta útil! ⭐

</div>
