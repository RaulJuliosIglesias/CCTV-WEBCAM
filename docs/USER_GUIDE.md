# 📖 Guía de Usuario / User Guide

<div align="center">

**Transforma cualquier cámara RTSP en una webcam virtual**

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![Windows 11](https://img.shields.io/badge/Windows-11-0078D4?style=for-the-badge&logo=windows11)](https://www.microsoft.com/windows)

</div>

---

## 🇪🇸 Español

### 🚀 Inicio Rápido

#### Paso 1: Ejecutar la Aplicación
1. Descarga y extrae `RTSPVirtualCam-portable.zip`
2. Ejecuta `RTSPVirtualCam.exe`
3. No requiere instalación ni permisos de administrador

#### Paso 2: Conectar a Cámara RTSP
1. Ingresa la URL RTSP en el campo correspondiente
2. Formato: `rtsp://usuario:contraseña@IP:puerto/ruta`
3. Selecciona la marca de cámara desde el dropdown (Hikvision, Dahua, ONVIF)
4. Ejemplos comunes:
   - **Hikvision**: `rtsp://admin:password@192.168.1.100:554/Streaming/Channels/101`
   - **Dahua**: `rtsp://admin:password@192.168.1.100:554/cam/realmonitor?channel=1&subtype=0`
   - **ONVIF**: `rtsp://admin:password@192.168.1.100:554/onvif1`

#### Paso 3: Verificar Conexión
1. Haz clic en **▶ Preview** para probar el stream
2. Verifica que la imagen se muestre correctamente
3. Revisa la información de stream en la barra de estado:
   - Resolución (ej: 1920x1080)
   - FPS (ej: 30 fps)
   - Codec (ej: H.264)
   - Bitrate y transporte

#### Paso 4: Virtualizar Cámara
1. Haz clic en **📹 Virtualize** para crear la cámara virtual
2. Espera el mensaje "Virtual Camera Active"
3. La cámara ahora está disponible para otras aplicaciones
4. **Windows 11**: Aparece como "RTSP VirtualCam"
5. **Windows 10**: Aparece como "OBS Virtual Camera"

#### Paso 5: Usar en Aplicaciones
1. Abre Zoom, Teams, Meet, OBS, Discord, etc.
2. Busca "RTSP VirtualCam" en las opciones de cámara
3. Selecciona y ajusta configuración si es necesario

### ⚙️ Funciones Avanzadas

#### Panel de Control
- **Preview**: Muestra vista previa del stream RTSP
- **Virtualize**: Crea cámara virtual Windows
- **Stop**: Detiene conexión y virtualización
- **Settings**: Configuración avanzada (próximamente)
- **PTZ Controls**: Pan-Tilt-Zoom (solo cámaras compatibles)
- **Profiles**: Guarda y carga configuraciones de cámara

#### Barra de Estado
- **Estado**: Conectado/Virtualizado/Desconectado
- **Resolución**: Dimensiones del video (ej: 1920x1080)
- **Frame Rate**: FPS del stream (ej: 30 fps)
- **Codec**: Tipo de compresión (ej: H.264)
- **Transport**: Protocolo (TCP/UDP)
- **Bitrate**: Tasa de datos actual
- **Latency**: Latencia de conexión en ms

#### Historial de URLs
- Guarda automáticamente las últimas 10 conexiones
- Acceso rápido desde el dropdown junto al campo URL
- Organizado por fecha de uso más reciente

### 🔧 Solución de Problemas

#### La cámara no aparece en aplicaciones
1. **Reinicia la aplicación** de videoconferencia
2. **Verifica permisos de cámara** en Windows > Configuración > Privacidad > Cámara
3. **Confirma Windows 11 Build 22000+** (requerido para cámara virtual)
4. **Desconecta y vuelve a conectar** desde RTSPVirtualCam

#### "No Signal" o pantalla negra
1. **Verifica la URL RTSP** - prueba primero en VLC
2. **Confirma credenciales** de la cámara
3. **Revisa conectividad de red** hacia la cámara
4. **Intenta con transporte TCP** (predeterminado)

#### Timeout de conexión
1. **Verifica IP y puerto** de la cámara
2. **Comprueba que RTSP esté habilitado** en la cámara
3. **Reduce timeout** en configuración si es necesario
4. **Prueba con cable Ethernet** para mayor estabilidad

#### Alta latencia o lag
1. **Usa conexión cableada** en lugar de WiFi
2. **Reduce resolución** si el ancho de banda es limitado
3. **Verifica configuración de buffer** (300ms predeterminado)
4. **Cierra otras aplicaciones** que consuman red

### 📋 Requisitos del Sistema

| Componente | Mínimo | Recomendado |
|------------|--------|-------------|
| **Sistema Operativo** | Windows 11 Build 22000 | Windows 11 22H2+ |
| **RAM** | 2 GB | 4 GB |
| **CPU** | Cualquier x64 | Multi-core |
| **Red** | 10 Mbps | 100 Mbps |
| **Espacio** | 50 MB | 100 MB |

---

## 🇬🇧 English

### 🚀 Quick Start

#### Step 1: Launch Application
1. Download and extract `RTSPVirtualCam-portable.zip`
2. Run `RTSPVirtualCam.exe`
3. No installation or admin rights required

#### Step 2: Connect to RTSP Camera
1. Enter RTSP URL in the corresponding field
2. Format: `rtsp://username:password@IP:port/path`
3. Select camera brand from dropdown (Hikvision, Dahua, ONVIF)
4. Common examples:
   - **Hikvision**: `rtsp://admin:password@192.168.1.100:554/Streaming/Channels/101`
   - **Dahua**: `rtsp://admin:password@192.168.1.100:554/cam/realmonitor?channel=1&subtype=0`
   - **ONVIF**: `rtsp://admin:password@192.168.1.100:554/onvif1`

#### Step 3: Verify Connection
1. Click **▶ Preview** to test the stream
2. Verify the image displays correctly
3. Check stream information in status bar:
   - Resolution (e.g., 1920x1080)
   - FPS (e.g., 30 fps)
   - Codec (e.g., H.264)
   - Bitrate and transport

#### Step 4: Virtualize Camera
1. Click **📹 Virtualize** to create virtual camera
2. Wait for "Virtual Camera Active" message
3. Camera is now available for other applications
4. **Windows 11**: Appears as "RTSP VirtualCam"
5. **Windows 10**: Appears as "OBS Virtual Camera"

#### Step 5: Use in Applications
1. Open Zoom, Teams, Meet, OBS, Discord, etc.
2. Look for "RTSP VirtualCam" in camera options
3. Select and adjust settings if needed

### ⚙️ Advanced Features

#### Control Panel
- **Preview**: Shows RTSP stream preview
- **Virtualize**: Creates Windows virtual camera
- **Stop**: Stops connection and virtualization
- **Settings**: Advanced configuration (coming soon)
- **PTZ Controls**: Pan-Tilt-Zoom (compatible cameras only)
- **Profiles**: Save and load camera configurations

#### Status Bar
- **Status**: Connected/Virtualized/Disconnected
- **Resolution**: Video dimensions (e.g., 1920x1080)
- **Frame Rate**: Stream FPS (e.g., 30 fps)
- **Codec**: Compression type (e.g., H.264)
- **Transport**: Protocol (TCP/UDP)
- **Bitrate**: Current data rate
- **Latency**: Connection latency in ms

#### URL History
- Automatically saves last 10 connections
- Quick access from dropdown next to URL field
- Organized by most recent usage

### 🔧 Troubleshooting

#### Camera not available in apps
1. **Restart the video conferencing application**
2. **Check camera permissions** in Windows > Settings > Privacy > Camera
3. **Confirm Windows 11 Build 22000+** (required for virtual camera)
4. **Disconnect and reconnect** from RTSPVirtualCam

#### "No Signal" or black screen
1. **Verify RTSP URL** - test first in VLC
2. **Confirm camera credentials**
3. **Check network connectivity** to camera
4. **Try TCP transport** (default)

#### Connection timeout
1. **Verify camera IP and port**
2. **Ensure RTSP is enabled** on camera
3. **Reduce timeout** in settings if needed
4. **Try Ethernet connection** for better stability

#### High latency or lag
1. **Use wired connection** instead of WiFi
2. **Lower resolution** if bandwidth is limited
3. **Check buffer configuration** (300ms default)
4. **Close other network-consuming** applications

### 📋 System Requirements

| Component | Minimum | Recommended |
|------------|---------|-------------|
| **Operating System** | Windows 11 Build 22000 | Windows 11 22H2+ |
| **RAM** | 2 GB | 4 GB |
| **CPU** | Any x64 | Multi-core |
| **Network** | 10 Mbps | 100 Mbps |
| **Storage** | 50 MB | 100 MB |

---

<div align="center">

**© 2026 Raúl Julios Iglesias - Todos los Derechos Reservados**

</div>
