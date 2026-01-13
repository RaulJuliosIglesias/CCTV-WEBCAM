# 🐛 Troubleshooting / Solución de Problemas

<p align="center">
  <strong>🇬🇧 English</strong> | <a href="#solución-de-problemas-español">🇪🇸 Español</a>
</p>

---

## Troubleshooting (English)

### Connection Issues

#### ❌ "Connection timeout"

**Possible causes:**
- Camera is not reachable on the network
- Wrong IP address or port
- Firewall blocking RTSP traffic

**Solutions:**
1. Ping the camera IP: `ping 192.168.1.100`
2. Verify port 554 is open: `Test-NetConnection 192.168.1.100 -Port 554`
3. Test URL in VLC: `vlc rtsp://...`
4. Check firewall rules

#### ❌ "Authentication failed"

**Possible causes:**
- Wrong username or password
- Special characters not encoded

**Solutions:**
1. Verify credentials in camera web interface
2. URL-encode special characters:
   - `@` → `%40`
   - `:` → `%3A`
   - `#` → `%23`

#### ❌ "Unsupported codec"

**Possible causes:**
- Camera using non-standard codec

**Solutions:**
1. Check camera settings for H.264/H.265
2. Try sub-stream (usually lower codec)
3. Update camera firmware

---

### Video Issues

#### ❌ Black screen in preview

**Solutions:**
1. Wait 5-10 seconds for stream to initialize
2. Try TCP transport (already default)
3. Reduce camera resolution
4. Check camera is not being used by another app

#### ❌ Choppy/laggy video

**Solutions:**
1. Use sub-stream instead of main stream
2. Reduce camera resolution/bitrate
3. Check network bandwidth
4. Close other bandwidth-heavy applications

---

### Virtual Camera Issues

#### ❌ Camera not appearing in apps

**Solutions:**
1. Restart the video conferencing app
2. Check Windows Settings → Privacy → Camera
3. Verify Windows 11 Build 22000+:
   ```powershell
   winver
   ```

#### ❌ "Windows 11 required" error

**Cause:** MFCreateVirtualCamera API only available on Windows 11

**Solution:** Upgrade to Windows 11 or use alternative solutions (OBS Virtual Camera)

---

### Log Files

Logs are stored in:
```
%APPDATA%\RTSPVirtualCam\logs\rtspvirtualcam.log
```

Or in the application directory:
```
./logs/rtspvirtualcam.log
```

---

## Solución de Problemas (Español)

### Problemas de Conexión

#### ❌ "Timeout de conexión"

**Posibles causas:**
- La cámara no es accesible en la red
- Dirección IP o puerto incorrectos
- Firewall bloqueando tráfico RTSP

**Soluciones:**
1. Hacer ping a la IP de la cámara: `ping 192.168.1.100`
2. Verificar que el puerto 554 esté abierto: `Test-NetConnection 192.168.1.100 -Port 554`
3. Probar URL en VLC: `vlc rtsp://...`
4. Revisar reglas del firewall

#### ❌ "Fallo de autenticación"

**Posibles causas:**
- Usuario o contraseña incorrectos
- Caracteres especiales no codificados

**Soluciones:**
1. Verificar credenciales en interfaz web de la cámara
2. Codificar caracteres especiales en URL:
   - `@` → `%40`
   - `:` → `%3A`
   - `#` → `%23`

#### ❌ "Codec no soportado"

**Posibles causas:**
- La cámara usa codec no estándar

**Soluciones:**
1. Verificar configuración de cámara para H.264/H.265
2. Probar sub-stream (usualmente codec menor)
3. Actualizar firmware de la cámara

---

### Problemas de Video

#### ❌ Pantalla negra en vista previa

**Soluciones:**
1. Esperar 5-10 segundos para que el stream se inicialice
2. Intentar transporte TCP (ya es el predeterminado)
3. Reducir resolución de la cámara
4. Verificar que la cámara no esté siendo usada por otra app

#### ❌ Video entrecortado/lento

**Soluciones:**
1. Usar sub-stream en lugar de stream principal
2. Reducir resolución/bitrate de la cámara
3. Verificar ancho de banda de red
4. Cerrar otras aplicaciones que consuman ancho de banda

---

### Problemas de Cámara Virtual

#### ❌ La cámara no aparece en las apps

**Soluciones:**
1. Reiniciar la aplicación de videoconferencia
2. Revisar Configuración de Windows → Privacidad → Cámara
3. Verificar Windows 11 Build 22000+:
   ```powershell
   winver
   ```

#### ❌ Error "Se requiere Windows 11"

**Causa:** La API MFCreateVirtualCamera solo está disponible en Windows 11

**Solución:** Actualizar a Windows 11 o usar soluciones alternativas (OBS Virtual Camera)

---

### Archivos de Log

Los logs se almacenan en:
```
%APPDATA%\RTSPVirtualCam\logs\rtspvirtualcam.log
```

O en el directorio de la aplicación:
```
./logs/rtspvirtualcam.log
```
