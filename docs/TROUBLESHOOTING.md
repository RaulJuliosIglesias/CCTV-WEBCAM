# 🐛 Troubleshooting / Solución de Problemas

<div align="center">

**Guía completa de diagnóstico y solución de problemas**

[![Windows 11](https://img.shields.io/badge/Windows-11-0078D4?style=for-the-badge&logo=windows11)](https://www.microsoft.com/windows)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)

</div>

<p align="center">
  <strong>🇬🇧 English</strong> | <a href="#solución-de-problemas-español">🇪🇸 Español</a>
</p>

---

## Troubleshooting (English)

### 🔍 Quick Diagnosis Flow

```mermaid
flowchart TD
    A[RTSPVirtualCam Issue] --> B{Can see preview?}
    B -->|No| C[Connection Problem]
    B -->|Yes| D{Camera virtualized?}
    D -->|No| E[Virtual Camera Problem]
    D -->|Yes| F[App Integration Problem]
    
    C --> C1[Check RTSP URL]
    C --> C2[Check Network]
    C --> C3[Check Camera]
    
    E --> E1[Check Windows Version]
    E --> E2[Check Permissions]
    E --> E3[Restart App]
    
    F --> F1[Restart Target App]
    F --> F2[Check Camera List]
    F --> F3[Check Privacy Settings]
```

---

### 🌐 Connection Issues

#### ❌ "Connection timeout" / "No Signal"

**Symptoms:**
- Preview shows "No Signal" placeholder
- Status shows "Connecting..." indefinitely
- Error message appears after 30 seconds

**Diagnostic Steps:**
```powershell
# 1. Test network connectivity
ping 192.168.1.100

# 2. Test RTSP port
Test-NetConnection 192.168.1.100 -Port 554

# 3. Test URL in VLC
vlc "rtsp://admin:password@192.168.1.100:554/Streaming/Channels/101"
```

**Common Causes & Solutions:**

| Cause | Solution |
|-------|----------|
| **Wrong IP/Port** | Verify camera IP in router DHCP list |
| **Camera offline** | Check camera power and network cable |
| **Firewall blocking** | Add rule for RTSP traffic (port 554) |
| **Wrong URL format** | Use correct format for your camera brand |
| **Network isolation** | Camera and PC on same subnet |

**Camera-Specific URLs:**
```bash
# Hikvision
rtsp://admin:password@IP:554/Streaming/Channels/101  # Main stream
rtsp://admin:password@IP:554/Streaming/Channels/102  # Sub-stream

# Dahua
rtsp://admin:password@IP:554/cam/realmonitor?channel=1&subtype=0
rtsp://admin:password@IP:554/cam/realmonitor?channel=1&subtype=1

# ONVIF Generic
rtsp://admin:password@IP:554/onvif1
```

#### ❌ PTZ controls not working

**Symptoms:**
- PTZ buttons disabled or not responding
- Camera movement commands fail

**Solutions:**
1. **Check camera compatibility** - PTZ only works with Hikvision cameras currently
2. **Verify PTZ credentials** - Separate RTSP and PTZ credentials may be required
3. **Check camera firmware** - Update to latest firmware
4. **Test with manufacturer software** - Verify PTZ works with official tools
5. **Check network ports** - PTZ may use different ports than RTSP

**Symptoms:**
- Immediate error message
- VLC asks for credentials

#### ❌ "Authentication failed"

**Symptoms:**
- Immediate error message
- VLC asks for credentials

**Solutions:**
1. **Verify credentials** in camera web interface
2. **Check special characters** - URL encode if needed:
   ```bash
   # Replace special characters
   @ → %40
   : → %3A
   # → %23
   / → %2F
   ```
3. **Default credentials** to try:
   - `admin:admin`
   - `admin:12345`
   - `admin:password`
   - `admin:` (no password)

#### ❌ "High CPU usage"

**Symptoms:**
- System becomes slow
- RTSPVirtualCam.exe using high CPU

**Solutions:**
1. **Lower camera resolution** - Use sub-stream instead of main stream
2. **Reduce FPS** - Set to 15-20 FPS instead of 30
3. **Enable hardware acceleration** - Settings → Performance → Hardware decode
4. **Close other applications** - Free up system resources
5. **Update graphics drivers** - Ensure GPU acceleration is working

**Symptoms:**
- Connection succeeds but no video
- Error about unsupported format

#### ❌ "Unsupported codec" / "Stream not found"

**Symptoms:**
- Connection succeeds but no video
- Error about unsupported format

**Solutions:**
1. **Check camera codec settings** - ensure H.264
2. **Try sub-stream** (usually lower resolution, H.264)
3. **Update camera firmware**
4. **Reduce camera resolution** to 1080p or lower

---

### 📹 Video Issues

#### ❌ Black screen in preview

**Symptoms:**
- Connected status but black video
- Audio may work but no video

**Solutions:**
1. **Wait 5-10 seconds** for stream initialization
2. **Try TCP transport** (already default in app)
3. **Reduce camera resolution** in camera settings
4. **Check camera encoding** - force H.264 baseline profile
5. **Restart camera** - power cycle if needed

#### ❌ Choppy/laggy/high latency video

**Symptoms:**
- Video freezes or stutters
- Audio-video sync issues
- Delay of several seconds

**Performance Optimization:**

| Issue | Solution |
|-------|----------|
| **High latency** | Use sub-stream instead of main stream |
| **Network congestion** | Use wired Ethernet instead of WiFi |
| **High resolution** | Reduce to 720p or 1080p |
| **High bitrate** | Lower bitrate in camera settings |
| **CPU overload** | Close other applications |

**Network Requirements:**
- **720p@30fps**: ~2-4 Mbps
- **1080p@30fps**: ~4-8 Mbps
- **4K@30fps**: ~15-25 Mbps

---

### 🎥 Virtual Camera Issues

#### ❌ "Virtual camera creation failed"

**Symptoms:**
- Error message when clicking Virtualize
- Camera appears but shows black screen

**Solutions:**
1. **Check Windows version** - Windows 11 Build 22000+ required
2. **Run as administrator** - Right-click RTSPVirtualCam.exe → Run as admin
3. **Check camera privacy settings**:
   - Settings → Privacy → Camera
   - Enable "Camera access" and "Allow desktop apps to access camera"
4. **Restart Windows Camera service**:
   ```powershell
   net stop FrameServer
   net start FrameServer
   ```
5. **Update graphics drivers** - Latest drivers from manufacturer

**Symptoms:**
- "RTSP VirtualCam" not in camera list
- Other apps don't see the virtual camera

**System Requirements Check:**
```powershell
# Check Windows version
winver
# Should show: Windows 11 Build 22000 or higher

# Check camera permissions
# Settings → Privacy → Camera → Let apps access camera
```

**Solutions:**
1. **Restart target application** (Zoom, Teams, etc.)
2. **Check Windows privacy settings**:
   - Settings → Privacy → Camera
   - Enable "Camera access"
   - Enable "Allow desktop apps to access camera"
3. **Restart RTSPVirtualCam** with administrator privileges
4. **Check Windows 11 build** - must be 22000+
5. **Update Windows** to latest version

#### ❌ "Windows 11 required" error

**Cause:** MFCreateVirtualCamera API only available on Windows 11

**Solutions:**
- **Upgrade to Windows 11** (recommended)
- **Use Windows 10 with OBS Virtual Camera**:
  1. Install OBS Studio
  2. Add RTSP source in OBS
  3. Start OBS Virtual Camera
  4. Select "OBS Virtual Camera" in apps
- **Use Unity Capture plugin** (included in scripts folder)

---

### 🖥️ Application Issues

#### ❌ App crashes on startup

**Solutions:**
1. **Check .NET 8 runtime** installed
2. **Run as administrator**
3. **Check Windows Event Viewer** for crash details
4. **Disable antivirus** temporarily
5. **Download fresh copy** - possible corruption

#### ❌ UI freezes or becomes unresponsive

**Solutions:**
1. **Wait 30 seconds** - may be initializing
2. **Check network connection** - may timeout
3. **Restart application**
4. **Check system resources** - CPU/memory usage

---

### 📋 Log Files & Debugging

#### Log Locations
```
# Application logs
./logs/rtspvirtualcam.log

# Windows Event Viewer
Windows Logs → Application → Source: RTSPVirtualCam

# VLC logs (if enabled)
./logs/vlc.log
```

#### Debug Information Collection
```powershell
# System information
systeminfo | findstr /B /C:"OS Name" /C:"OS Version"

# Network configuration
ipconfig /all

# Camera connectivity test
Test-NetConnection -ComputerName 192.168.1.100 -Port 554
```

#### Common Log Messages
```
INFO: RTSP connection established
WARN: Frame buffer overflow - dropping frames
ERROR: MFCreateVirtualCamera failed - Windows 11 required
ERROR: RTSP authentication failed
```

---

## Solución de Problemas (Español)

### 🔍 Flujo de Diagnóstico Rápido

```mermaid
flowchart TD
    A[Problema RTSPVirtualCam] --> B{Ves vista previa?}
    B -->|No| C[Problema de Conexión]
    B -->|Sí| D{Cámara virtualizada?}
    D -->|No| E[Problema Cámara Virtual]
    D -->|Sí| F[Problema Integración App]
    
    C --> C1[Verificar URL RTSP]
    C --> C2[Verificar Red]
    C --> C3[Verificar Cámara]
    
    E --> E1[Verificar Versión Windows]
    E --> E2[Verificar Permisos]
    E --> E3[Reiniciar App]
    
    F --> F1[Reiniciar App Objetivo]
    F --> F2[Verificar Lista Cámaras]
    F --> F3[Verificar Configuración Privacidad]
```

---

### 🌐 Problemas de Conexión

#### ❌ "Timeout de conexión" / "No Signal"

**Síntomas:**
- Vista previa muestra placeholder "No Signal"
- Estado muestra "Conectando..." indefinidamente
- Mensaje de error después de 30 segundos

**Pasos de Diagnóstico:**
```powershell
# 1. Probar conectividad de red
ping 192.168.1.100

# 2. Probar puerto RTSP
Test-NetConnection 192.168.1.100 -Port 554

# 3. Probar URL en VLC
vlc "rtsp://admin:password@192.168.1.100:554/Streaming/Channels/101"
```

**Causas Comunes y Soluciones:**

| Causa | Solución |
|-------|----------|
| **IP/Puerto incorrecto** | Verificar IP de cámara en lista DHCP del router |
| **Cámara offline** | Revisar energía y cable de red de la cámara |
| **Firewall bloqueando** | Agregar regla para tráfico RTSP (puerto 554) |
| **Formato URL incorrecto** | Usar formato correcto para marca de cámara |
| **Aislamiento de red** | Cámara y PC en misma subred |

**URLs Específicas por Marca:**
```bash
# Hikvision
rtsp://admin:password@IP:554/Streaming/Channels/101  # Stream principal
rtsp://admin:password@IP:554/Streaming/Channels/102  # Sub-stream

# Dahua
rtsp://admin:password@IP:554/cam/realmonitor?channel=1&subtype=0
rtsp://admin:password@IP:554/cam/realmonitor?channel=1&subtype=1

# ONVIF Genérico
rtsp://admin:password@IP:554/onvif1
```

#### ❌ Controles PTZ no funcionan

**Síntomas:**
- Botones PTZ deshabilitados o no responden
- Comandos de movimiento de cámara fallan

**Soluciones:**
1. **Verificar compatibilidad de cámara** - PTZ solo funciona con cámaras Hikvision actualmente
2. **Verificar credenciales PTZ** - Pueden requerirse credenciales separadas para RTSP y PTZ
3. **Verificar firmware de cámara** - Actualizar a firmware más reciente
4. **Probar con software del fabricante** - Verificar que PTZ funciona con herramientas oficiales
5. **Verificar puertos de red** - PTZ puede usar puertos diferentes a RTSP

**Síntomas:**
- Mensaje de error inmediato
- VLC solicita credenciales

#### ❌ "Fallo de autenticación"

**Síntomas:**
- Mensaje de error inmediato
- VLC solicita credenciales

**Soluciones:**
1. **Verificar credenciales** en interfaz web de cámara
2. **Revisar caracteres especiales** - codificar en URL si es necesario:
   ```bash
   # Reemplazar caracteres especiales
   @ → %40
   : → %3A
   # → %23
   / → %2F
   ```
3. **Credenciales predeterminadas** a probar:
   - `admin:admin`
   - `admin:12345`
   - `admin:password`
   - `admin:` (sin contraseña)

#### ❌ "Alto uso de CPU"

**Síntomas:**
- El sistema se vuelve lento
- RTSPVirtualCam.exe usando alta CPU

**Soluciones:**
1. **Reducir resolución de cámara** - Usar sub-stream en lugar de stream principal
2. **Reducir FPS** - Configurar a 15-20 FPS en lugar de 30
3. **Activar aceleración por hardware** - Configuración → Rendimiento → Decodificación por hardware
4. **Cerrar otras aplicaciones** - Liberar recursos del sistema
5. **Actualizar controladores gráficos** - Asegurar que la aceleración GPU funcione

**Síntomas:**
- Conexión exitosa pero sin video
- Error sobre formato no soportado

#### ❌ "Codec no soportado" / "Stream no encontrado"

**Síntomas:**
- Conexión exitosa pero sin video
- Error sobre formato no soportado

**Soluciones:**
1. **Verificar configuración codec** de cámara - asegurar H.264
2. **Probar sub-stream** (usualmente menor resolución, H.264)
3. **Actualizar firmware** de cámara
4. **Reducir resolución** de cámara a 1080p o menor

---

### 📹 Problemas de Video

#### ❌ Pantalla negra en vista previa

**Síntomas:**
- Estado conectado pero video negro
- Audio puede funcionar pero no video

**Soluciones:**
1. **Esperar 5-10 segundos** para inicialización de stream
2. **Intentar transporte TCP** (ya es predeterminado en app)
3. **Reducir resolución** de cámara en configuración
4. **Verificar encoding** de cámara - forzar H.264 baseline profile
5. **Reiniciar cámara** - ciclo de energía si es necesario

#### ❌ Video entrecortado/lento/alta latencia

**Síntomas:**
- Video se congela o titila
- Problemas de sincronización audio-video
- Retardo de varios segundos

**Optimización de Rendimiento:**

| Problema | Solución |
|----------|----------|
| **Alta latencia** | Usar sub-stream en lugar de stream principal |
| **Congestión de red** | Usar Ethernet cableada en lugar de WiFi |
| **Resolución alta** | Reducir a 720p o 1080p |
| **Bitrate alto** | Reducir bitrate en configuración de cámara |
| **Sobrecarga CPU** | Cerrar otras aplicaciones |

**Requisitos de Red:**
- **720p@30fps**: ~2-4 Mbps
- **1080p@30fps**: ~4-8 Mbps
- **4K@30fps**: ~15-25 Mbps

---

### 🎥 Problemas de Cámara Virtual

#### ❌ "Falló creación de cámara virtual"

**Síntomas:**
- Mensaje de error al hacer clic en Virtualizar
- La cámara aparece pero muestra pantalla negra

**Soluciones:**
1. **Verificar versión de Windows** - Se requiere Windows 11 Build 22000+
2. **Ejecutar como administrador** - Clic derecho en RTSPVirtualCam.exe → Ejecutar como administrador
3. **Verificar configuración de privacidad de cámara**:
   - Configuración → Privacidad → Cámara
   - Activar "Acceso a cámara" y "Permitir que apps de escritorio accedan a la cámara"
4. **Reiniciar servicio Windows Camera**:
   ```powershell
   net stop FrameServer
   net start FrameServer
   ```
5. **Actualizar controladores gráficos** - Últimos controladores del fabricante

**Síntomas:**
- "RTSP VirtualCam" no está en lista de cámaras
- Otras apps no ven la cámara virtual

**Verificación de Requisitos del Sistema:**
```powershell
# Verificar versión de Windows
winver
# Debe mostrar: Windows 11 Build 22000 o superior

# Verificar permisos de cámara
# Configuración → Privacidad → Cámara → Permitir acceso de apps a cámara
```

**Soluciones:**
1. **Reiniciar aplicación objetivo** (Zoom, Teams, etc.)
2. **Verificar configuración de privacidad de Windows**:
   - Configuración → Privacidad → Cámara
   - Activar "Acceso a cámara"
   - Activar "Permitir que apps de escritorio accedan a la cámara"
3. **Reiniciar RTSPVirtualCam** con privilegios de administrador
4. **Verificar build de Windows 11** - debe ser 22000+
5. **Actualizar Windows** a última versión

#### ❌ Error "Se requiere Windows 11"

**Causa:** La API MFCreateVirtualCamera solo está disponible en Windows 11

**Soluciones:**
- **Actualizar a Windows 11** (recomendado)
- **Usar Windows 10 con OBS Virtual Camera**:
  1. Instalar OBS Studio
  2. Agregar fuente RTSP en OBS
  3. Iniciar OBS Virtual Camera
  4. Seleccionar "OBS Virtual Camera" en las apps
- **Usar plugin Unity Capture** (incluido en carpeta scripts)

---

### 🖥️ Problemas de Aplicación

#### ❌ La app se cierra al iniciar

**Soluciones:**
1. **Verificar runtime .NET 8** instalado
2. **Ejecutar como administrador**
3. **Revisar Visor de Eventos de Windows** para detalles del cierre
4. **Desactivar antivirus** temporalmente
5. **Descargar copia fresca** - posible corrupción

#### ❌ La UI se congela o no responde

**Soluciones:**
1. **Esperar 30 segundos** - puede estar inicializando
2. **Verificar conexión de red** - puede estar en timeout
3. **Reiniciar aplicación**
4. **Verificar recursos del sistema** - uso de CPU/memoria

---

### 📋 Archivos de Log y Depuración

#### Ubicación de Logs
```
# Logs de aplicación
./logs/rtspvirtualcam.log

# Visor de Eventos de Windows
Registros de Windows → Aplicación → Origen: RTSPVirtualCam

# Logs de VLC (si está activado)
./logs/vlc.log
```

#### Recolección de Información de Depuración
```powershell
# Información del sistema
systeminfo | findstr /B /C:"OS Name" /C:"OS Version"

# Configuración de red
ipconfig /all

# Prueba de conectividad de cámara
Test-NetConnection -ComputerName 192.168.1.100 -Port 554
```

#### Mensajes Comunes en Logs
```
INFO: RTSP connection established
WARN: Frame buffer overflow - dropping frames
ERROR: MFCreateVirtualCamera failed - Windows 11 required
ERROR: RTSP authentication failed
```

---

### 🆘 Support & Contact

#### Before Requesting Support
1. **Check this guide first** - most issues are covered
2. **Collect log files** from the locations above
3. **Gather system information** using the diagnostic commands
4. **Test with VLC** to isolate RTSP issues

#### Common Debug Information to Include
- Windows version and build number
- Camera brand and model
- Exact RTSP URL (remove password)
- Error messages from logs
- Network topology (WiFi vs Ethernet)

---

<div align="center">

**© 2026 Raúl Julios Iglesias - Todos los Derechos Reservados**

</div>
